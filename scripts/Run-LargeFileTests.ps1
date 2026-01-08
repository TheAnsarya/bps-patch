<#
.SYNOPSIS
    Run large file tests for BPS Patch encoding/decoding.

.DESCRIPTION
    This script performs comprehensive large file testing to validate the BPS patch
    implementation with files of various sizes. It's designed to run overnight or
    during periods when the computer is not in active use.

    Tests include:
    - Multiple file sizes (1MB, 10MB, 50MB, 100MB, 500MB, 1GB)
    - All matching algorithms (Linear, RabinKarp, SuffixArray)
    - Various compression options (lazy matching, cost-based, RLE)
    - Performance benchmarks

.PARAMETER FileSizes
    Array of file sizes to test in MB. Default: 1, 10, 50, 100
    For full testing use: 1, 10, 50, 100, 500, 1024

.PARAMETER Algorithms
    Matching algorithms to test. Default: All
    Options: Linear, RabinKarp, SuffixArray, All

.PARAMETER OutputPath
    Directory to store test results. Default: ./test-results

.PARAMETER SkipCleanup
    If specified, temporary test files are preserved after testing.

.EXAMPLE
    .\Run-LargeFileTests.ps1

.EXAMPLE
    .\Run-LargeFileTests.ps1 -FileSizes 1, 10, 50 -Algorithms SuffixArray

.EXAMPLE
    .\Run-LargeFileTests.ps1 -FileSizes 1, 10, 50, 100, 500, 1024 -OutputPath "C:\TestResults"
#>

param(
    [int[]]$FileSizes = @(1, 10, 50, 100),
    [ValidateSet('Linear', 'RabinKarp', 'SuffixArray', 'All')]
    [string]$Algorithms = 'All',
    [string]$OutputPath = './test-results',
    [switch]$SkipCleanup
)

$ErrorActionPreference = 'Stop'
$ScriptRoot = $PSScriptRoot
$ProjectRoot = Split-Path $ScriptRoot -Parent

# Create output directory
$OutputPath = Resolve-Path $OutputPath -ErrorAction SilentlyContinue
if (-not $OutputPath) {
    $OutputPath = New-Item -ItemType Directory -Path $OutputPath -Force | Select-Object -ExpandProperty FullName
}
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
}

$TempPath = Join-Path $OutputPath "temp"
if (-not (Test-Path $TempPath)) {
    New-Item -ItemType Directory -Path $TempPath -Force | Out-Null
}

$LogFile = Join-Path $OutputPath "large-file-tests-$(Get-Date -Format 'yyyy-MM-dd-HHmmss').log"
$ResultsFile = Join-Path $OutputPath "large-file-results-$(Get-Date -Format 'yyyy-MM-dd-HHmmss').csv"

function Write-Log {
    param([string]$Message, [string]$Level = 'INFO')
    $Timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $LogMessage = "[$Timestamp] [$Level] $Message"
    Write-Host $LogMessage -ForegroundColor $(switch ($Level) {
        'ERROR' { 'Red' }
        'WARN' { 'Yellow' }
        'SUCCESS' { 'Green' }
        default { 'White' }
    })
    Add-Content -Path $LogFile -Value $LogMessage
}

function Get-AlgorithmsToTest {
    if ($Algorithms -eq 'All') {
        return @('Linear', 'RabinKarp', 'SuffixArray')
    }
    return @($Algorithms)
}

function New-TestFile {
    param([int]$SizeMB, [string]$Name)

    $FilePath = Join-Path $TempPath $Name
    $SizeBytes = $SizeMB * 1024 * 1024

    Write-Log "Generating $SizeMB MB test file: $Name"

    # Generate file with pattern (sequential + random sections)
    $Buffer = New-Object byte[] (1024 * 1024) # 1MB buffer
    $Random = [System.Random]::new(42)

    $Stream = [System.IO.File]::Create($FilePath)
    try {
        for ($i = 0; $i -lt $SizeMB; $i++) {
            # Alternating pattern: sequential, random, repeating
            switch ($i % 3) {
                0 {
                    # Sequential data
                    for ($j = 0; $j -lt $Buffer.Length; $j++) {
                        $Buffer[$j] = [byte](($i * 1024 * 1024 + $j) % 256)
                    }
                }
                1 {
                    # Random data
                    $Random.NextBytes($Buffer)
                }
                2 {
                    # Repeating pattern
                    $Pattern = [byte[]](0xAB, 0xCD, 0xEF, 0x12)
                    for ($j = 0; $j -lt $Buffer.Length; $j++) {
                        $Buffer[$j] = $Pattern[$j % 4]
                    }
                }
            }
            $Stream.Write($Buffer, 0, $Buffer.Length)
        }
    }
    finally {
        $Stream.Dispose()
    }

    return $FilePath
}

function New-ModifiedFile {
    param([string]$SourcePath, [string]$Name, [double]$ChangePercent = 5.0)

    $TargetPath = Join-Path $TempPath $Name
    $SourceData = [System.IO.File]::ReadAllBytes($SourcePath)

    Write-Log "Creating modified file with $ChangePercent% changes: $Name"

    $Random = [System.Random]::new(99)
    $ChangeCount = [int]($SourceData.Length * ($ChangePercent / 100.0))

    for ($i = 0; $i -lt $ChangeCount; $i++) {
        $Pos = $Random.Next($SourceData.Length)
        $SourceData[$Pos] = [byte]($SourceData[$Pos] -bxor 0xFF)
    }

    [System.IO.File]::WriteAllBytes($TargetPath, $SourceData)
    return $TargetPath
}

function Test-Encoding {
    param(
        [string]$SourcePath,
        [string]$TargetPath,
        [string]$Algorithm,
        [hashtable]$Options = @{}
    )

    $PatchPath = Join-Path $TempPath "patch-$Algorithm-$(Get-Random).bps"
    $OutputPath = Join-Path $TempPath "output-$Algorithm-$(Get-Random).bin"

    $Result = @{
        Algorithm = $Algorithm
        SourceSize = (Get-Item $SourcePath).Length
        TargetSize = (Get-Item $TargetPath).Length
        PatchSize = 0
        EncodeTime = 0
        DecodeTime = 0
        Success = $false
        Error = $null
        Options = ($Options | ConvertTo-Json -Compress)
    }

    try {
        # Build CLI arguments
        $EncodeArgs = @(
            "encode",
            $SourcePath,
            $PatchPath,
            $TargetPath,
            "--algorithm", $Algorithm
        )

        if ($Options.UseLazyMatching) { $EncodeArgs += "--lazy-matching" }
        if ($Options.UseCostBasedMatching) { $EncodeArgs += "--cost-based" }
        if (-not $Options.UseRleOptimization) { $EncodeArgs += "--no-rle" }

        # Measure encoding time
        $CliPath = Join-Path $ProjectRoot "src\BpsPatch.Cli\bin\Debug\net10.0\bps-patch.exe"
        if (-not (Test-Path $CliPath)) {
            $CliPath = Join-Path $ProjectRoot "bin\Debug\net10.0\bps-patch.exe"
        }

        $EncodeStart = Get-Date
        $EncodeOutput = & $CliPath $EncodeArgs 2>&1
        $EncodeEnd = Get-Date
        $Result.EncodeTime = ($EncodeEnd - $EncodeStart).TotalMilliseconds

        if ($LASTEXITCODE -ne 0) {
            throw "Encoding failed: $EncodeOutput"
        }

        $Result.PatchSize = (Get-Item $PatchPath).Length

        # Measure decoding time
        $DecodeArgs = @("decode", $SourcePath, $PatchPath, $OutputPath)

        $DecodeStart = Get-Date
        $DecodeOutput = & $CliPath $DecodeArgs 2>&1
        $DecodeEnd = Get-Date
        $Result.DecodeTime = ($DecodeEnd - $DecodeStart).TotalMilliseconds

        if ($LASTEXITCODE -ne 0) {
            throw "Decoding failed: $DecodeOutput"
        }

        # Verify output matches target
        $TargetHash = (Get-FileHash $TargetPath -Algorithm SHA256).Hash
        $OutputHash = (Get-FileHash $OutputPath -Algorithm SHA256).Hash

        if ($TargetHash -ne $OutputHash) {
            throw "Output file does not match target file!"
        }

        $Result.Success = $true
    }
    catch {
        $Result.Error = $_.Exception.Message
        Write-Log "Test failed: $($_.Exception.Message)" -Level ERROR
    }
    finally {
        # Cleanup patch and output files
        if (-not $SkipCleanup) {
            Remove-Item $PatchPath -ErrorAction SilentlyContinue
            Remove-Item $OutputPath -ErrorAction SilentlyContinue
        }
    }

    return $Result
}

# Main execution
Write-Log "=" * 80
Write-Log "BPS Large File Test Suite"
Write-Log "=" * 80
Write-Log "File sizes: $($FileSizes -join ', ') MB"
Write-Log "Algorithms: $(Get-AlgorithmsToTest)"
Write-Log "Output path: $OutputPath"
Write-Log "Log file: $LogFile"
Write-Log "=" * 80

# Build the project first
Write-Log "Building project..."
Push-Location $ProjectRoot
try {
    $BuildOutput = dotnet build -c Debug 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Log "Build failed: $BuildOutput" -Level ERROR
        exit 1
    }
    Write-Log "Build successful" -Level SUCCESS
}
finally {
    Pop-Location
}

# Initialize results CSV
$CsvHeader = "SizeMB,Algorithm,Options,PatchSize,EncodeTimeMs,DecodeTimeMs,CompressionRatio,Success,Error"
Set-Content -Path $ResultsFile -Value $CsvHeader

$AlgorithmsToTest = Get-AlgorithmsToTest
$TotalTests = $FileSizes.Count * $AlgorithmsToTest.Count * 4 # 4 option combinations
$CompletedTests = 0

$OptionCombinations = @(
    @{ UseLazyMatching = $false; UseCostBasedMatching = $false; UseRleOptimization = $true }
    @{ UseLazyMatching = $true; UseCostBasedMatching = $false; UseRleOptimization = $true }
    @{ UseLazyMatching = $false; UseCostBasedMatching = $true; UseRleOptimization = $true }
    @{ UseLazyMatching = $true; UseCostBasedMatching = $true; UseRleOptimization = $true }
)

foreach ($SizeMB in $FileSizes) {
    Write-Log ""
    Write-Log "-" * 60
    Write-Log "Testing $SizeMB MB files"
    Write-Log "-" * 60

    # Generate source file
    $SourcePath = New-TestFile -SizeMB $SizeMB -Name "source-$SizeMB.bin"

    # Generate target file (5% changes)
    $TargetPath = New-ModifiedFile -SourcePath $SourcePath -Name "target-$SizeMB.bin" -ChangePercent 5.0

    foreach ($Algorithm in $AlgorithmsToTest) {
        foreach ($Options in $OptionCombinations) {
            $CompletedTests++
            $OptionsDesc = if ($Options.UseLazyMatching -and $Options.UseCostBasedMatching) {
                "Lazy+Cost"
            } elseif ($Options.UseLazyMatching) {
                "Lazy"
            } elseif ($Options.UseCostBasedMatching) {
                "Cost"
            } else {
                "Default"
            }

            Write-Log "[$CompletedTests/$TotalTests] $SizeMB MB, $Algorithm, $OptionsDesc"

            $Result = Test-Encoding -SourcePath $SourcePath -TargetPath $TargetPath -Algorithm $Algorithm -Options $Options

            # Calculate compression ratio
            $CompressionRatio = if ($Result.PatchSize -gt 0) {
                [math]::Round($Result.TargetSize / $Result.PatchSize, 2)
            } else {
                0
            }

            # Write to CSV
            $CsvLine = "$SizeMB,$Algorithm,`"$OptionsDesc`",$($Result.PatchSize),$([int]$Result.EncodeTime),$([int]$Result.DecodeTime),$CompressionRatio,$($Result.Success),`"$($Result.Error -replace '"', '""')`""
            Add-Content -Path $ResultsFile -Value $CsvLine

            if ($Result.Success) {
                $RatioDisplay = "{0:N2}x" -f $CompressionRatio
                $EncodeSpeed = "{0:N2}" -f ($SizeMB * 1024 / ($Result.EncodeTime / 1000))
                Write-Log "  PASS - Patch: $($Result.PatchSize) bytes, Ratio: $RatioDisplay, Encode: $($Result.EncodeTime)ms ($EncodeSpeed KB/s)" -Level SUCCESS
            } else {
                Write-Log "  FAIL - $($Result.Error)" -Level ERROR
            }
        }
    }

    # Cleanup source and target files
    if (-not $SkipCleanup) {
        Remove-Item $SourcePath -ErrorAction SilentlyContinue
        Remove-Item $TargetPath -ErrorAction SilentlyContinue
    }
}

# Summary
Write-Log ""
Write-Log "=" * 80
Write-Log "Test Summary"
Write-Log "=" * 80

$Results = Import-Csv $ResultsFile
$SuccessCount = ($Results | Where-Object { $_.Success -eq 'True' }).Count
$FailCount = ($Results | Where-Object { $_.Success -eq 'False' }).Count

Write-Log "Total tests: $($Results.Count)"
Write-Log "Passed: $SuccessCount" -Level $(if ($SuccessCount -gt 0) { 'SUCCESS' } else { 'INFO' })
Write-Log "Failed: $FailCount" -Level $(if ($FailCount -gt 0) { 'ERROR' } else { 'INFO' })
Write-Log "Results file: $ResultsFile"
Write-Log "Log file: $LogFile"

# Cleanup temp directory
if (-not $SkipCleanup) {
    Remove-Item $TempPath -Recurse -ErrorAction SilentlyContinue
}

Write-Log "=" * 80
Write-Log "Large file tests complete!"
Write-Log "=" * 80

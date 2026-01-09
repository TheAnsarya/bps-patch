# 🧪 Manual Testing Guide

> 📚 **Navigation**: [← Back to README](../README.md) | [Usage Guide](USAGE.md) | [Compression Testing](COMPRESSION_TESTING.md) | [Benchmarks](BENCHMARKS_SETUP.md)

This guide provides step-by-step instructions for manually testing the BPS Patch library. These tests are designed to complement the automated test suite and verify real-world behavior.

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Quick Start](#quick-start)
3. [Large File Testing](#large-file-testing)
4. [Performance Benchmarking](#performance-benchmarking)
5. [Edge Case Testing](#edge-case-testing)
6. [Compression Optimization Testing](#compression-optimization-testing)
7. [CLI Testing](#cli-testing)
8. [Troubleshooting](#troubleshooting)

---

## Prerequisites

### Required Software
- .NET 10 SDK
- PowerShell 7+ (for scripts)
- Git (optional, for version control)

### Build the Project
```powershell
cd c:\Users\me\source\repos\bps-patch
dotnet build -c Debug
```

### Verify Tests Pass
```powershell
dotnet test --no-build
```

Expected: 229+ tests should pass (107 modern + 122 legacy).

---

## Quick Start

### 1. Basic Encoding/Decoding Test

Create a simple test with small files:

```powershell
# Create test files
$source = [byte[]](0..255)
$target = [byte[]](0..255)
$target[100] = 0xFF  # Modify one byte

[System.IO.File]::WriteAllBytes("source.bin", $source)
[System.IO.File]::WriteAllBytes("target.bin", $target)

# Create patch
.\bin\Debug\net10.0\bps-patch.exe encode source.bin patch.bps target.bin

# Apply patch
.\bin\Debug\net10.0\bps-patch.exe decode source.bin patch.bps output.bin

# Verify
if ((Get-FileHash target.bin).Hash -eq (Get-FileHash output.bin).Hash) {
	Write-Host "SUCCESS: Output matches target" -ForegroundColor Green
} else {
	Write-Host "FAILURE: Output does not match target" -ForegroundColor Red
}

# Cleanup
Remove-Item source.bin, target.bin, patch.bps, output.bin
```

### 2. Run Automated Tests
```powershell
dotnet test src\BpsPatch.Core.Tests\BpsPatch.Core.Tests.csproj -v minimal
```

---

## Large File Testing

Large file tests should be run when the computer is not in active use (overnight, lunch break, etc.) as they can take significant time and resources.

### Using the Test Script

```powershell
# Navigate to project root
cd c:\Users\me\source\repos\bps-patch

# Run with default settings (1, 10, 50, 100 MB files)
.\scripts\Run-LargeFileTests.ps1

# Run with specific file sizes
.\scripts\Run-LargeFileTests.ps1 -FileSizes 1, 10, 50

# Run overnight with full test (up to 1GB)
.\scripts\Run-LargeFileTests.ps1 -FileSizes 1, 10, 50, 100, 500, 1024 -OutputPath "C:\TestResults"

# Test specific algorithm
.\scripts\Run-LargeFileTests.ps1 -FileSizes 10, 50 -Algorithms SuffixArray

# Keep temporary files for inspection
.\scripts\Run-LargeFileTests.ps1 -SkipCleanup
```

### Script Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `-FileSizes` | 1, 10, 50, 100 | Array of file sizes in MB |
| `-Algorithms` | All | Linear, RabinKarp, SuffixArray, or All |
| `-OutputPath` | ./test-results | Directory for results |
| `-SkipCleanup` | False | Keep temp files after tests |

### Expected Results

For a 100MB file with 5% changes:
- **SuffixArray**: Best compression ratio (~15-20x), slowest encoding (~5-10 seconds)
- **RabinKarp**: Good compression ratio (~10-15x), moderate speed (~2-5 seconds)
- **Linear**: Decent compression ratio (~8-12x), fastest encoding (~1-2 seconds)

### Interpreting Results

The script generates two output files:
1. **Log file**: Detailed test progress and results
2. **CSV file**: Machine-readable results for analysis

```powershell
# View results
Import-Csv ".\test-results\large-file-results-*.csv" | Format-Table

# Analyze compression ratios by algorithm
Import-Csv ".\test-results\large-file-results-*.csv" | 
	Where-Object { $_.Success -eq 'True' } |
	Group-Object Algorithm | 
	ForEach-Object {
		[PSCustomObject]@{
			Algorithm = $_.Name
			AvgRatio = ($_.Group | Measure-Object -Property CompressionRatio -Average).Average
			AvgEncodeMs = ($_.Group | Measure-Object -Property EncodeTimeMs -Average).Average
		}
	}
```

---

## Performance Benchmarking

### Run BenchmarkDotNet

```powershell
cd src\BpsPatch.Core.Benchmarks
dotnet run -c Release --filter "*"
```

### Specific Benchmarks

```powershell
# Encoder benchmarks only
dotnet run -c Release --filter "*Encoder*"

# Decoder benchmarks only
dotnet run -c Release --filter "*Decoder*"

# Matching strategy benchmarks
dotnet run -c Release --filter "*Matching*"
```

### Memory Profiling

```powershell
# Run with memory diagnoser
dotnet run -c Release --filter "*" -- --memory
```

---

## Edge Case Testing

### 1. Empty Files

```powershell
# Create empty source
[System.IO.File]::WriteAllBytes("empty.bin", @())
[System.IO.File]::WriteAllBytes("small.bin", [byte[]](1, 2, 3))

# Should create valid patch
.\bin\Debug\net10.0\bps-patch.exe encode empty.bin patch.bps small.bin
.\bin\Debug\net10.0\bps-patch.exe decode empty.bin patch.bps output.bin

# Cleanup
Remove-Item empty.bin, small.bin, patch.bps, output.bin
```

### 2. Identical Files

```powershell
$data = [byte[]](0..999)
[System.IO.File]::WriteAllBytes("source.bin", $data)
[System.IO.File]::WriteAllBytes("target.bin", $data)

.\bin\Debug\net10.0\bps-patch.exe encode source.bin patch.bps target.bin

# Patch should be very small (just header + CRCs)
$patchSize = (Get-Item patch.bps).Length
Write-Host "Patch size for identical files: $patchSize bytes"
# Expected: < 100 bytes

Remove-Item source.bin, target.bin, patch.bps
```

### 3. Completely Different Files

```powershell
$random = [System.Random]::new(42)
$source = [byte[]]::new(10000)
$target = [byte[]]::new(10000)
$random.NextBytes($source)
$random.NextBytes($target)

[System.IO.File]::WriteAllBytes("source.bin", $source)
[System.IO.File]::WriteAllBytes("target.bin", $target)

.\bin\Debug\net10.0\bps-patch.exe encode source.bin patch.bps target.bin

# Patch should be approximately target size + overhead
$patchSize = (Get-Item patch.bps).Length
Write-Host "Patch size for completely different files: $patchSize bytes"
# Expected: ~10000-10100 bytes

.\bin\Debug\net10.0\bps-patch.exe decode source.bin patch.bps output.bin

# Verify
if ((Get-FileHash target.bin).Hash -eq (Get-FileHash output.bin).Hash) {
	Write-Host "SUCCESS" -ForegroundColor Green
}

Remove-Item source.bin, target.bin, patch.bps, output.bin
```

### 4. Large Offset Values

```powershell
# Create file with data at very end
$size = 10 * 1024 * 1024  # 10MB
$source = [byte[]]::new($size)
$target = [byte[]]::new($size)

# Fill with zeros
for ($i = 0; $i -lt $size; $i++) {
	$source[$i] = 0
	$target[$i] = 0
}

# Put some data at the end
for ($i = $size - 1000; $i -lt $size; $i++) {
	$source[$i] = [byte]($i % 256)
	$target[$i] = [byte](($i + 1) % 256)
}

[System.IO.File]::WriteAllBytes("source.bin", $source)
[System.IO.File]::WriteAllBytes("target.bin", $target)

.\bin\Debug\net10.0\bps-patch.exe encode source.bin patch.bps target.bin
.\bin\Debug\net10.0\bps-patch.exe decode source.bin patch.bps output.bin

Remove-Item source.bin, target.bin, patch.bps, output.bin
```

---

## Compression Optimization Testing

### Test Lazy Matching

```powershell
# Create test data with patterns
$source = [byte[]]::new(10000)
$target = [byte[]]::new(10000)
$pattern = [byte[]](0xAB, 0xCD, 0xEF, 0x12)

for ($i = 0; $i -lt 10000; $i++) {
	$source[$i] = $pattern[$i % 4]
	$target[$i] = $pattern[($i + 1) % 4]  # Shifted pattern
}

[System.IO.File]::WriteAllBytes("source.bin", $source)
[System.IO.File]::WriteAllBytes("target.bin", $target)

# Without lazy matching
.\bin\Debug\net10.0\bps-patch.exe encode source.bin patch-no-lazy.bps target.bin

# With lazy matching
.\bin\Debug\net10.0\bps-patch.exe encode source.bin patch-lazy.bps target.bin --lazy-matching

Write-Host "Without lazy: $((Get-Item patch-no-lazy.bps).Length) bytes"
Write-Host "With lazy: $((Get-Item patch-lazy.bps).Length) bytes"

Remove-Item source.bin, target.bin, patch-no-lazy.bps, patch-lazy.bps
```

### Test Cost-Based Matching

```powershell
# Create test data
$source = [byte[]]::new(5000)
$target = [byte[]]::new(5000)

for ($i = 0; $i -lt 5000; $i++) {
	$source[$i] = [byte]($i % 256)
	$target[$i] = [byte](($i + 50) % 256)  # Offset copy opportunity
}

[System.IO.File]::WriteAllBytes("source.bin", $source)
[System.IO.File]::WriteAllBytes("target.bin", $target)

# Without cost-based
.\bin\Debug\net10.0\bps-patch.exe encode source.bin patch-no-cost.bps target.bin

# With cost-based
.\bin\Debug\net10.0\bps-patch.exe encode source.bin patch-cost.bps target.bin --cost-based

Write-Host "Without cost-based: $((Get-Item patch-no-cost.bps).Length) bytes"
Write-Host "With cost-based: $((Get-Item patch-cost.bps).Length) bytes"

Remove-Item source.bin, target.bin, patch-no-cost.bps, patch-cost.bps
```

### Test RLE Optimization

```powershell
# Create data with RLE patterns
$target = [byte[]]::new(1000)
for ($i = 0; $i -lt 1000; $i++) {
	$target[$i] = [byte]($i / 100)  # Runs of 100 same bytes
}

$source = [byte[]]::new(1000)
for ($i = 0; $i -lt 1000; $i++) {
	$source[$i] = [byte]($i % 256)
}

[System.IO.File]::WriteAllBytes("source.bin", $source)
[System.IO.File]::WriteAllBytes("target.bin", $target)

# With RLE (default)
.\bin\Debug\net10.0\bps-patch.exe encode source.bin patch-rle.bps target.bin

# Without RLE
.\bin\Debug\net10.0\bps-patch.exe encode source.bin patch-no-rle.bps target.bin --no-rle

Write-Host "With RLE: $((Get-Item patch-rle.bps).Length) bytes"
Write-Host "Without RLE: $((Get-Item patch-no-rle.bps).Length) bytes"

Remove-Item source.bin, target.bin, patch-rle.bps, patch-no-rle.bps
```

---

## CLI Testing

### Command Help

```powershell
.\bin\Debug\net10.0\bps-patch.exe --help
.\bin\Debug\net10.0\bps-patch.exe encode --help
.\bin\Debug\net10.0\bps-patch.exe decode --help
```

### Encode Options

```powershell
# Basic encode
.\bin\Debug\net10.0\bps-patch.exe encode source.bin patch.bps target.bin

# With metadata
.\bin\Debug\net10.0\bps-patch.exe encode source.bin patch.bps target.bin -m "My patch v1.0"

# With specific algorithm
.\bin\Debug\net10.0\bps-patch.exe encode source.bin patch.bps target.bin --algorithm SuffixArray

# With all optimizations
.\bin\Debug\net10.0\bps-patch.exe encode source.bin patch.bps target.bin --lazy-matching --cost-based
```

### Decode Options

```powershell
# Basic decode
.\bin\Debug\net10.0\bps-patch.exe decode source.bin patch.bps output.bin

# Ignore CRC errors (dangerous!)
.\bin\Debug\net10.0\bps-patch.exe decode source.bin patch.bps output.bin --ignore-crc
```

---

## Troubleshooting

### Common Issues

#### Build Errors
```powershell
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

#### Test Failures
```powershell
# Run with verbose output
dotnet test -v detailed

# Run specific failing test
dotnet test --filter "FullyQualifiedName~TestName"
```

#### Out of Memory
- For very large files (>500MB), ensure you have sufficient RAM
- Consider using the SuffixArray algorithm which is more memory-efficient
- Close other applications during large file tests

#### Slow Performance
- Run Release builds for benchmarking: `dotnet build -c Release`
- Disable real-time antivirus scanning for test directories
- Use SSD storage for temp files

### Debug Mode

```powershell
# Set environment variable for verbose logging
$env:BPS_DEBUG = "1"
.\bin\Debug\net10.0\bps-patch.exe encode source.bin patch.bps target.bin
```

### Getting Help

1. Check [ALGORITHMS.md](ALGORITHMS.md) for algorithm details
2. Check [BPS_FORMAT_SPECIFICATION.md](BPS_FORMAT_SPECIFICATION.md) for format details
3. Run automated tests to verify environment
4. Check GitHub issues for known problems

---

## Checklist

Before releasing or making major changes, verify:

- [ ] All automated tests pass (`dotnet test`)
- [ ] Large file tests pass (1MB, 10MB, 50MB)
- [ ] All algorithms produce identical output
- [ ] CLI help is accurate
- [ ] Documentation is up to date
- [ ] No memory leaks in large file handling
- [ ] Performance is acceptable for target use cases

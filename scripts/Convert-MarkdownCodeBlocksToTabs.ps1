#!/usr/bin/env pwsh
<#
.SYNOPSIS
	Converts space indentation to tabs in Markdown code blocks.

.DESCRIPTION
	This script processes all Markdown files and converts 4-space indentation
	to tab characters inside code blocks (``` and ~~~~).

.PARAMETER Path
	The root path to search for Markdown files. Defaults to current directory.

.PARAMETER WhatIf
	Preview changes without modifying files.

.EXAMPLE
	.\Convert-MarkdownCodeBlocksToTabs.ps1
	.\Convert-MarkdownCodeBlocksToTabs.ps1 -Path "C:\MyProject" -WhatIf
#>

[CmdletBinding(SupportsShouldProcess)]
param(
	[Parameter()]
	[string]$Path = "."
)

$ErrorActionPreference = "Stop"

# Get all markdown files
$mdFiles = Get-ChildItem -Path $Path -Filter "*.md" -Recurse -File

$totalFiles = 0
$modifiedFiles = 0
$totalConversions = 0

foreach ($file in $mdFiles) {
	$totalFiles++
	$content = Get-Content -Path $file.FullName -Raw
	$originalContent = $content

	# Track if we're inside a code block
	$lines = $content -split "`r?`n"
	$newLines = @()
	$inCodeBlock = $false
	$codeBlockMarker = ""
	$fileConversions = 0

	foreach ($line in $lines) {
		# Check for code block start/end
		if ($line -match '^(`{3,}|~{3,})') {
			$marker = $Matches[1]
			if (-not $inCodeBlock) {
				$inCodeBlock = $true
				$codeBlockMarker = $marker.Substring(0, 1)  # ` or ~
			} elseif ($line -match "^[$codeBlockMarker]{3,}$") {
				$inCodeBlock = $false
				$codeBlockMarker = ""
			}
			$newLines += $line
			continue
		}

		# Inside code block - convert leading spaces to tabs
		if ($inCodeBlock) {
			$originalLine = $line

			# Match leading spaces in groups of 4 and convert to tabs
			$leadingSpaces = ""
			if ($line -match '^( +)') {
				$leadingSpaces = $Matches[1]
				$spaceCount = $leadingSpaces.Length
				$tabCount = [math]::Floor($spaceCount / 4)
				$remainingSpaces = $spaceCount % 4

				if ($tabCount -gt 0) {
					$tabs = "`t" * $tabCount
					$remaining = " " * $remainingSpaces
					$restOfLine = $line.Substring($spaceCount)
					$line = $tabs + $remaining + $restOfLine

					if ($line -ne $originalLine) {
						$fileConversions++
					}
				}
			}
		}

		$newLines += $line
	}

	$newContent = $newLines -join "`r`n"

	# Check if content changed
	if ($newContent -ne $originalContent) {
		$modifiedFiles++
		$totalConversions += $fileConversions

		if ($WhatIf -or $PSCmdlet.ShouldProcess($file.FullName, "Convert spaces to tabs")) {
			if (-not $WhatIf) {
				Set-Content -Path $file.FullName -Value $newContent -NoNewline
			}
			Write-Host "✅ $($file.Name): $fileConversions conversions" -ForegroundColor Green
		}
	} else {
		Write-Host "⏭️  $($file.Name): No changes needed" -ForegroundColor DarkGray
	}
}

Write-Host ""
Write-Host "📊 Summary:" -ForegroundColor Cyan
Write-Host "   Files scanned: $totalFiles"
Write-Host "   Files modified: $modifiedFiles"
Write-Host "   Total conversions: $totalConversions"

if ($WhatIf) {
	Write-Host ""
	Write-Host "⚠️  WhatIf mode - no files were actually modified" -ForegroundColor Yellow
}

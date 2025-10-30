# BPS Patch - Usage Guide

Complete guide for using the BPS patch tool from command line and as a library in your own applications.

**Last Updated**: October 30, 2025

---

## Table of Contents

1. [Quick Start](#quick-start)
2. [Command-Line Usage](#command-line-usage)
3. [Library Usage](#library-usage)
4. [Advanced Topics](#advanced-topics)
5. [Performance Tuning](#performance-tuning)
6. [Troubleshooting](#troubleshooting)

---

## Quick Start

### Installation

**From Release**:
1. Download latest release from GitHub Releases
2. Extract to desired location
3. Add to PATH (optional)

**From Source**:
```powershell
git clone https://github.com/TheAnsarya/bps-patch.git
cd bps-patch/bps-patch
dotnet build -c Release
```

### Basic Examples

**Create a patch**:
```powershell
bps-patch encode original.bin modified.bin patch.bps "v1.0 - Bug fixes"
```

**Apply a patch**:
```powershell
bps-patch decode original.bin patch.bps patched.bin
```

---

## Command-Line Usage

### Encoding (Create Patch)

**Syntax**:
```
bps-patch encode <source> <target> <patch> [metadata]
```

**Parameters**:
- `source`: Original file path
- `target`: Modified file path
- `patch`: Output patch file path
- `metadata`: Optional metadata string (typically XML)

**Examples**:
```powershell
# Simple patch creation
bps-patch encode rom_v1.0.gba rom_v1.1.gba update.bps

# With metadata
bps-patch encode original.smc patched.smc translation.bps "Japanese translation by TeamX"

# With XML metadata
bps-patch encode base.bin mod.bin patch.bps "<patch><author>John Doe</author><version>2.0</version></patch>"
```

**Output**:
```
Creating patch...
Source size: 4,194,304 bytes
Target size: 4,194,304 bytes
Patch size: 125,432 bytes
Compression ratio: 3.0%
Completed in 245ms
```

### Decoding (Apply Patch)

**Syntax**:
```
bps-patch decode <source> <patch> <output>
```

**Parameters**:
- `source`: Original file path
- `patch`: BPS patch file path
- `output`: Output file path

**Examples**:
```powershell
# Apply patch to create new file
bps-patch decode original.bin patch.bps patched.bin

# Apply translation patch
bps-patch decode game_en.gba translation.bps game_jp.gba
```

**Output**:
```
Applying patch...
Source size: 4,194,304 bytes
Target size: 4,194,304 bytes
Patch size: 125,432 bytes
Warnings:
  - Source CRC32 mismatch (expected vs actual)
Completed in 98ms
```

### Exit Codes

- `0`: Success
- `1`: Error occurred (check error message)
- `2`: Invalid arguments

---

## Library Usage

### Add Reference

**NuGet** (when published):
```powershell
dotnet add package BpsPatch
```

**Project Reference**:
```xml
<ItemGroup>
  <ProjectReference Include="..\bps-patch\bps-patch.csproj" />
</ItemGroup>
```

### Basic API

#### Create Patch

```csharp
using bps_patch;

// Simple patch creation
var sourceFile = new FileInfo("original.bin");
var targetFile = new FileInfo("modified.bin");
var patchFile = new FileInfo("patch.bps");

Encoder.CreatePatch(
    sourceFile,
    patchFile,
    targetFile,
    metadata: "Version 1.0 - Bug fixes"
);

Console.WriteLine($"Patch created: {patchFile.Length} bytes");
```

#### Apply Patch

```csharp
using bps_patch;

// Simple patch application
var sourceFile = new FileInfo("original.bin");
var patchFile = new FileInfo("patch.bps");
var outputFile = new FileInfo("patched.bin");

List<string> warnings = Decoder.ApplyPatch(
    sourceFile,
    patchFile,
    outputFile
);

// Check for warnings
if (warnings.Count > 0) {
    Console.WriteLine("Warnings:");
    foreach (var warning in warnings) {
        Console.WriteLine($"  - {warning}");
    }
}
```

### Advanced API Examples

#### Custom Error Handling

```csharp
try {
    var warnings = Decoder.ApplyPatch(sourceFile, patchFile, outputFile);
    
    if (warnings.Any(w => w.Contains("CRC32"))) {
        Console.WriteLine("Warning: File hash mismatch - file may be corrupted");
    }
} catch (PatchFormatException ex) {
    Console.WriteLine($"Invalid patch file: {ex.Message}");
} catch (IOException ex) {
    Console.WriteLine($"File error: {ex.Message}");
}
```

#### Batch Processing

```csharp
var patchDirectory = new DirectoryInfo("patches");
var sourceFile = new FileInfo("base.bin");

foreach (var patchFile in patchDirectory.GetFiles("*.bps")) {
    var outputFile = new FileInfo($"output/{patchFile.Name}.bin");
    
    Console.WriteLine($"Applying {patchFile.Name}...");
    
    var warnings = Decoder.ApplyPatch(sourceFile, patchFile, outputFile);
    
    if (warnings.Count == 0) {
        Console.WriteLine($"  ✓ Success");
    } else {
        Console.WriteLine($"  ⚠ Completed with {warnings.Count} warning(s)");
    }
}
```

#### Verify Patch Before Applying

```csharp
// Read patch header to check compatibility
using var patchStream = patchFile.OpenRead();
byte[] header = new byte[4];
patchStream.Read(header, 0, 4);

if (Encoding.UTF8.GetString(header) != "BPS1") {
    throw new PatchFormatException("Not a valid BPS patch file");
}

// Decode sizes
ulong sourceSize = ReadVarInt(patchStream);
ulong targetSize = ReadVarInt(patchStream);

Console.WriteLine($"Patch expects source size: {sourceSize} bytes");
Console.WriteLine($"Your file size: {sourceFile.Length} bytes");

if ((ulong)sourceFile.Length != sourceSize) {
    Console.WriteLine("Warning: Source file size mismatch!");
}
```

### Pattern Matching Algorithms

#### Use Rabin-Karp for Large Files

```csharp
// For large, repetitive files
var source = File.ReadAllBytes("large_source.bin");
var target = File.ReadAllBytes("large_target.bin");

// Use Rabin-Karp algorithm (O(n) average case)
var (length, start, reachedEnd) = Encoder.FindBestRunRabinKarp(
    source,
    target.AsSpan(position, remainingLength),
    minimumLongestRun: 4
);

Console.WriteLine($"Found {length} byte match at position {start}");
```

#### Use Suffix Array for Multiple Patches

```csharp
// Build suffix array once, reuse for multiple pattern searches
var source = File.ReadAllBytes("rom.bin");
var suffixArray = new SuffixArray(source);

// Create multiple patches efficiently
foreach (var targetFile in targetFiles) {
    var target = File.ReadAllBytes(targetFile);
    
    for (int pos = 0; pos < target.Length; pos++) {
        var (length, start, reachedEnd) = Encoder.FindBestRunSuffixArray(
            suffixArray,
            target.AsSpan(pos),
            minimumLongestRun: 4
        );
        
        // Use match result...
    }
}
```

### Memory-Mapped Files for Large Files

```csharp
using System.IO.MemoryMappedFiles;

// For files > 256MB
if (MemoryMappedFileHelper.ShouldUseMemoryMapped(fileInfo.Length)) {
    var (mmf, accessor) = MemoryMappedFileHelper.CreateReadAccessor(fileInfo);
    
    using (mmf)
    using (accessor) {
        // Read chunks without loading entire file
        byte[] chunk = MemoryMappedFileHelper.ReadBytes(accessor, offset: 1000000, length: 4096);
        
        // Compute CRC32 in chunks
        byte[] crc = MemoryMappedFileHelper.ComputeCRC32Chunked(accessor, fileInfo.Length);
    }
}
```

---

## Advanced Topics

### Metadata Format

**Recommended**: Use XML for structured metadata

```xml
<patch>
  <title>Final Fantasy VI - Brave New World</title>
  <author>BTB and Synchysi</author>
  <version>2.0.0</version>
  <date>2025-10-30</date>
  <description>Complete gameplay overhaul</description>
  <website>https://example.com</website>
</patch>
```

**Access in Code**:
```csharp
// Metadata is embedded in patch file
// Extract by reading BPS header manually
using var patchStream = patchFile.OpenRead();
// Skip header, sizes...
ulong metadataSize = ReadVarInt(patchStream);
byte[] metadataBytes = new byte[metadataSize];
patchStream.Read(metadataBytes, 0, (int)metadataSize);
string metadata = Encoding.UTF8.GetString(metadataBytes);
```

### Compression Ratios

Typical compression ratios by change type:

| Change Type | Compression Ratio | Example |
|-------------|------------------|---------|
| Text replacement | 1-5% | Translation patches |
| Graphics update | 5-15% | Sprite modifications |
| Code changes | 2-8% | Bug fixes |
| Total conversion | 50-90% | Complete ROM hacks |
| Identical files | < 0.1% | Test/verification |

### Performance Characteristics

**Encoding Time** (approximate):
- Small files (< 1MB): < 100ms
- Medium files (1-10MB): 100ms - 1s
- Large files (10-100MB): 1s - 10s
- Huge files (> 100MB): 10s - 60s

**Decoding Time** (approximate):
- Small patches (< 100KB): < 50ms
- Medium patches (100KB-1MB): 50ms - 200ms
- Large patches (> 1MB): 200ms - 1s

**Memory Usage**:
- Files < 256MB: 2x file size (source + target in memory)
- Files > 256MB: ~10MB constant (memory-mapped)

---

## Performance Tuning

### Choose Optimal Algorithm

```csharp
// Small files: Linear search
if (sourceSize < 1024 * 1024) { // < 1MB
    Encoder.FindBestRunLinear(source, target);
}

// Large files: Rabin-Karp
else if (sourceSize < 100 * 1024 * 1024) { // < 100MB
    Encoder.FindBestRunRabinKarp(source, target);
}

// Huge files or multiple patches: Suffix array
else {
    var sa = new SuffixArray(source);
    Encoder.FindBestRunSuffixArray(sa, target);
}
```

### Optimize for Specific Scenarios

**Scenario 1: Many small changes** (e.g., bug fixes)
- Linear search is fastest
- Patch will be small (<< 1% of file size)

**Scenario 2: Large repetitive changes** (e.g., graphics replacement)
- Rabin-Karp or suffix array recommended
- Patch size depends on similarity

**Scenario 3: Complete overhaul** (e.g., total conversion)
- Any algorithm works (few matches to find)
- Patch size will be large (> 50% of file size)

### Parallel Processing

For creating multiple patches in parallel:

```csharp
using System.Threading.Tasks;

var patchTasks = targetFiles.Select(targetFile => Task.Run(() => {
    Encoder.CreatePatch(sourceFile, targetFile, patchFile, metadata);
})).ToArray();

Task.WaitAll(patchTasks);
```

---

## Troubleshooting

### Common Errors

#### "Source CRC32 mismatch"

**Cause**: Wrong source file being patched.

**Solution**: Verify you have the correct original file:
```powershell
# Compute CRC32 of your file
certutil -hashfile original.bin CRC32
```

#### "Patch CRC32 mismatch"

**Cause**: Corrupted patch file.

**Solution**: Re-download patch file or verify integrity.

#### "File size exceeds maximum"

**Cause**: File larger than `int.MaxValue` (2GB).

**Solution**: Use memory-mapped file approach or split file.

#### "Access denied" / "File is being used"

**Cause**: File locked by another process.

**Solution**: Close applications using the file, or use file sharing:
```csharp
var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
```

### Validation Checklist

Before creating a patch:
- ✅ Source and target files are correct
- ✅ Files are not corrupted
- ✅ Sufficient disk space for patch file
- ✅ Write permissions to output directory

Before applying a patch:
- ✅ Patch file is not corrupted
- ✅ Source file matches expected hash (if available)
- ✅ Sufficient disk space for output file
- ✅ Write permissions to output directory

### Debugging Tips

**Enable verbose logging**:
```csharp
// Add Console.WriteLine to trace execution
Console.WriteLine($"Processing position {currentPos}/{totalLength}");
```

**Validate intermediate results**:
```csharp
// Check patch size is reasonable
if (patchFile.Length > sourceFile.Length * 1.1) {
    Console.WriteLine("Warning: Patch is unusually large - may indicate an issue");
}
```

**Compare with reference implementation**:
```powershell
# Use beat or flips to create reference patch
beat --create source.bin target.bin reference.bps

# Compare sizes
ls *.bps
```

---

## Additional Resources

- **BPS Format Specification**: See `BPS_FORMAT_SPECIFICATION.md`
- **Implementation Details**: See `IMPLEMENTATION.md`
- **API Documentation**: See XML comments in source code
- **ROM Hacking Community**: https://www.romhacking.net/
- **Original BPS Tool (beat)**: https://github.com/blakesmith/beat

---

## License

This implementation is provided under the MIT License. See LICENSE file for details.

---

## Support

- **Issues**: https://github.com/TheAnsarya/bps-patch/issues
- **Discussions**: https://github.com/TheAnsarya/bps-patch/discussions
- **Email**: [Your contact information]

---

**Happy Patching!** 🎮

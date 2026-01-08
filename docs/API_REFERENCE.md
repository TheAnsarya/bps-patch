# BPS Patch API Reference

Complete API documentation for the BPS Patch library.

## Table of Contents

- [Encoder Class](#encoder-class)
- [Decoder Class](#decoder-class)
- [RabinKarp Class](#rabinkarp-class)
- [SuffixArray Class](#suffixarray-class)
- [Utilities Class](#utilities-class)
- [PatchAction Enum](#patchaction-enum)
- [PatchFormatException Class](#patchformatexception-class)

---

## Encoder Class

**Namespace**: `bps_patch`  
**Assembly**: `bps-patch.dll`

Creates BPS patch files by analyzing differences between source and target files.

### Methods

#### CreatePatch

```csharp
public static void CreatePatch(
    FileInfo sourceFile,
    FileInfo patchFile,
    FileInfo targetFile,
    string manifest)
```

Creates a BPS patch file by comparing source and target files.

**Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `sourceFile` | `FileInfo` | Original file to patch from |
| `patchFile` | `FileInfo` | Output patch file to create |
| `targetFile` | `FileInfo` | Desired result file after patching |
| `manifest` | `string` | Metadata string (typically XML) |

**Exceptions**:
- `ArgumentException`: File exceeds `int.MaxValue` bytes

**Example**:
```csharp
Encoder.CreatePatch(
    new FileInfo("original.bin"),
    new FileInfo("patch.bps"),
    new FileInfo("modified.bin"),
    "My Patch v1.0");
```

---

#### EncodeNumber

```csharp
public static byte[] EncodeNumber(ulong number)
```

Encodes a number using BPS variable-length encoding (7 bits per byte).

**Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `number` | `ulong` | Number to encode |

**Returns**: `byte[]` - Encoded bytes (1-10 bytes for ulong)

**Example**:
```csharp
byte[] encoded = Encoder.EncodeNumber(300);
// Result: [0x2C, 0x81] (2 bytes)
```

---

#### FindNextRun

```csharp
public static (PatchAction Mode, int Length, int Start) FindNextRun(
    ReadOnlySpan<byte> source,
    ReadOnlySpan<byte> target,
    int targetPosition)
```

Finds the optimal patch action for the current target position.

**Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `source` | `ReadOnlySpan<byte>` | Source file data |
| `target` | `ReadOnlySpan<byte>` | Target file data |
| `targetPosition` | `int` | Current position in target |

**Returns**: Tuple of (action type, match length, start position)

**Example**:
```csharp
var (mode, length, start) = Encoder.FindNextRun(sourceData, targetData, 0);
// mode: PatchAction.SourceRead, SourceCopy, TargetCopy, or TargetRead
```

---

#### FindBestRun

```csharp
public static (int Length, int Start, bool ReachedEnd) FindBestRun(
    ReadOnlySpan<byte> source,
    ReadOnlySpan<byte> target,
    int minimumLongestRun = 4,
    int checkUntilMax = -1)
```

Finds the best matching run using the default algorithm (linear search).

**Parameters**:
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `source` | `ReadOnlySpan<byte>` | - | Data to search in |
| `target` | `ReadOnlySpan<byte>` | - | Pattern to search for |
| `minimumLongestRun` | `int` | 4 | Minimum match length |
| `checkUntilMax` | `int` | -1 | Maximum position (-1 for all) |

**Returns**: Tuple of (match length, start position, reached end flag)

---

#### FindBestRunLinear

```csharp
public static (int Length, int Start, bool ReachedEnd) FindBestRunLinear(...)
```

Linear search implementation. O(n²) worst case, best for small files.

---

#### FindBestRunRabinKarp

```csharp
public static (int Length, int Start, bool ReachedEnd) FindBestRunRabinKarp(...)
```

Rabin-Karp rolling hash implementation. O(n) average case.

---

#### FindBestRunSuffixArray

```csharp
public static (int Length, int Start, bool ReachedEnd) FindBestRunSuffixArray(
    ReadOnlySpan<byte> source,
    ReadOnlySpan<byte> target,
    int minimumLongestRun = 4)

public static (int Length, int Start, bool ReachedEnd) FindBestRunSuffixArray(
    SuffixArray suffixArray,
    ReadOnlySpan<byte> target,
    int minimumLongestRun = 4)
```

Suffix array implementation. O(log n) query time. Second overload reuses pre-built suffix array.

---

#### CheckRun

```csharp
public static (int Length, bool ReachedEnd) CheckRun(
    ReadOnlySpan<byte> source,
    ReadOnlySpan<byte> target)
```

Counts consecutive matching bytes using SIMD optimization.

**Returns**: Tuple of (match length, reached end of target flag)

---

#### CheckRunScalar

```csharp
public static (int Length, bool ReachedEnd) CheckRunScalar(
    ReadOnlySpan<byte> source,
    ReadOnlySpan<byte> target)
```

Non-SIMD version for benchmarking comparison.

---

## Decoder Class

**Namespace**: `bps_patch`  
**Assembly**: `bps-patch.dll`

Applies BPS patches to reconstruct target files from source files.

### Constants

| Constant | Value | Description |
|----------|-------|-------------|
| `MIN_PATCH_SIZE` | 19 | Minimum valid BPS patch size |

### Methods

#### ApplyPatch

```csharp
public static List<string> ApplyPatch(
    FileInfo sourceFile,
    FileInfo patchFile,
    FileInfo targetFile)
```

Applies a BPS patch to a source file to create the target file.

**Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `sourceFile` | `FileInfo` | Original file to patch |
| `patchFile` | `FileInfo` | BPS patch file |
| `targetFile` | `FileInfo` | Output file to create |

**Returns**: `List<string>` - Warning messages (empty if all checks pass)

**Exceptions**:
- `PatchFormatException`: Invalid patch format or header
- `ArgumentException`: Source size mismatch or target too large

**Example**:
```csharp
var warnings = Decoder.ApplyPatch(
    new FileInfo("original.bin"),
    new FileInfo("patch.bps"),
    new FileInfo("patched.bin"));

if (warnings.Count > 0) {
    Console.WriteLine("Warnings:");
    foreach (var w in warnings)
        Console.WriteLine($"  - {w}");
}
```

---

## RabinKarp Class

**Namespace**: `bps_patch`  
**Assembly**: `bps-patch.dll`

Rabin-Karp rolling hash implementation for fast substring matching.

### Methods

#### FindBestRun

```csharp
public static (int Length, int Start, bool ReachedEnd) FindBestRun(
    ReadOnlySpan<byte> source,
    ReadOnlySpan<byte> target,
    int minimumLongestRun = 4,
    int checkUntilMax = -1)
```

Finds the best matching substring using rolling hash.

**Time Complexity**: O(n) average, O(nm) worst case

**Example**:
```csharp
var (length, start, reachedEnd) = RabinKarp.FindBestRun(
    sourceData, 
    targetPattern,
    minimumLongestRun: 4);
```

---

## SuffixArray Class

**Namespace**: `bps_patch`  
**Assembly**: `bps-patch.dll`

Suffix array data structure for fast pattern matching.

### Constructors

#### SuffixArray(ReadOnlySpan<byte>)

```csharp
public SuffixArray(ReadOnlySpan<byte> data)
```

Creates a suffix array from the given data.

**Time Complexity**: O(n² log n) - naive implementation

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Data` | `ReadOnlySpan<byte>` | Underlying data |
| `Suffixes` | `ReadOnlySpan<int>` | Sorted suffix indices |
| `LCP` | `ReadOnlySpan<int>` | Longest common prefix array |

### Methods

#### FindLongestMatch

```csharp
public (int Length, int Start, bool ReachedEnd) FindLongestMatch(
    ReadOnlySpan<byte> pattern,
    int minimumLength = 4)
```

Finds the longest matching substring in the suffix array.

**Time Complexity**: O(log n) search + O(m) match extension

**Example**:
```csharp
var suffixArray = new SuffixArray(sourceData);
var (length, start, reachedEnd) = suffixArray.FindLongestMatch(pattern);
```

---

## Utilities Class

**Namespace**: `bps_patch`  
**Assembly**: `bps-patch.dll`

Utility methods for CRC32 computation and validation.

### Constants

| Constant | Value | Description |
|----------|-------|-------------|
| `CRC32_RESULT_CONSTANT` | `0x2144df1c` | CRC32 residue magic value |

### Methods

#### ComputeCRC32(FileInfo)

```csharp
public static uint ComputeCRC32(FileInfo sourceFile)
```

Computes CRC32 checksum for a file.

**Returns**: `uint` - CRC32 checksum

---

#### ComputeCRC32Bytes(FileInfo)

```csharp
public static byte[] ComputeCRC32Bytes(FileInfo sourceFile)
```

Computes CRC32 checksum as byte array (with retry logic).

**Returns**: `byte[]` - 4-byte CRC32 (little-endian)

---

#### ComputeCRC32Bytes(ReadOnlySpan<byte>)

```csharp
public static byte[] ComputeCRC32Bytes(ReadOnlySpan<byte> data)
```

Computes CRC32 checksum for in-memory data.

**Returns**: `byte[]` - 4-byte CRC32 (little-endian)

---

## PatchAction Enum

**Namespace**: `bps_patch`  
**Assembly**: `bps-patch.dll`

Defines the four BPS patch operations.

| Value | Name | Description |
|-------|------|-------------|
| 0 | `SourceRead` | Copy from source at same position |
| 1 | `TargetRead` | Copy new bytes from patch |
| 2 | `SourceCopy` | Copy from elsewhere in source |
| 3 | `TargetCopy` | Copy from earlier in target |

---

## PatchFormatException Class

**Namespace**: `bps_patch`  
**Assembly**: `bps-patch.dll`  
**Inherits**: `Exception`

Thrown when a BPS patch file is malformed.

### Constructors

```csharp
public PatchFormatException()
public PatchFormatException(string message)
public PatchFormatException(string message, Exception inner)
```

---

## Usage Examples

### Creating a Patch

```csharp
using bps_patch;

var source = new FileInfo("original.rom");
var target = new FileInfo("modified.rom");
var patch = new FileInfo("my-hack.bps");

Encoder.CreatePatch(source, patch, target, "ROM Hack v1.0");
Console.WriteLine($"Created patch: {patch.Length} bytes");
```

### Applying a Patch

```csharp
using bps_patch;

var source = new FileInfo("original.rom");
var patch = new FileInfo("my-hack.bps");
var output = new FileInfo("patched.rom");

var warnings = Decoder.ApplyPatch(source, patch, output);

if (warnings.Count == 0) {
    Console.WriteLine("Patch applied successfully!");
} else {
    Console.WriteLine("Patch applied with warnings:");
    foreach (var warning in warnings)
        Console.WriteLine($"  ⚠️ {warning}");
}
```

### Using Suffix Array for Multiple Queries

```csharp
using bps_patch;

byte[] sourceData = File.ReadAllBytes("large-file.bin");
var suffixArray = new SuffixArray(sourceData);

// Multiple queries reuse the same suffix array
foreach (var pattern in patterns) {
    var (length, start, _) = suffixArray.FindLongestMatch(pattern);
    if (length >= 4) {
        Console.WriteLine($"Found {length}-byte match at position {start}");
    }
}
```

---

## See Also

- [ARCHITECTURE.md](ARCHITECTURE.md) - System architecture
- [ALGORITHMS.md](ALGORITHMS.md) - Algorithm details
- [PERFORMANCE.md](PERFORMANCE.md) - Performance tuning

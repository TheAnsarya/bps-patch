# 🔌 BPS Patch API Reference

> 📚 **Navigation**: [← Back to README](../README.md) | [Usage Guide](USAGE.md) | [Architecture](ARCHITECTURE.md) | [Algorithms](ALGORITHMS.md)

Complete API documentation for the BPS Patch library.

## Table of Contents

- [BpsEncoder Class](#bpsencoder-class)
- [BpsEncoderOptions Class](#bpsencoderoptions-class)
- [BpsDecoder Class](#bpsdecoder-class)
- [DecodingResult Class](#decodingresult-class)
- [IMatchingStrategy Interface](#imatchingstrategy-interface)
- [Matching Strategies](#matching-strategies)
- [VariableLengthInt Class](#variablelengthint-class)
- [Crc32Calculator Class](#crc32calculator-class)
- [BpsAction Enum](#bpsaction-enum)
- [BpsFormatException Class](#bpsformatexception-class)
- [Legacy API (Reference Only)](#legacy-api-reference-only)

---

## BpsEncoder Class

**Namespace**: `BpsPatch.Core`  
**Assembly**: `BpsPatch.Core.dll`

Creates BPS patch files by analyzing differences between source and target files.

### Methods

#### CreatePatch

```csharp
public static void CreatePatch(
	FileInfo sourceFile,
	FileInfo patchFile,
	FileInfo targetFile,
	string metadata = "",
	BpsEncoderOptions? options = null)
```

Creates a BPS patch file by comparing source and target files.

**Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `sourceFile` | `FileInfo` | Original file to patch from |
| `patchFile` | `FileInfo` | Output patch file to create |
| `targetFile` | `FileInfo` | Desired result file after patching |
| `metadata` | `string` | Optional metadata string (default: empty) |
| `options` | `BpsEncoderOptions?` | Encoding options (null for defaults) |

**Exceptions**:
- `ArgumentException`: File exceeds `int.MaxValue` bytes
- `IOException`: Error reading/writing files

**Example**:
```csharp
using BpsPatch.Core;

// Simple usage
BpsEncoder.CreatePatch(
	new FileInfo("original.bin"),
	new FileInfo("patch.bps"),
	new FileInfo("modified.bin"),
	"My Patch v1.0");

// With options
BpsEncoder.CreatePatch(
	new FileInfo("original.bin"),
	new FileInfo("patch.bps"),
	new FileInfo("modified.bin"),
	"My Patch v1.0",
	new BpsEncoderOptions {
		Algorithm = MatchingAlgorithm.SuffixArray,
		UseLazyMatching = true,
		UseCostBasedMatching = true
	});
```

---

## BpsEncoderOptions Class

**Namespace**: `BpsPatch.Core`

Configuration options for BPS patch encoding.

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Algorithm` | `MatchingAlgorithm` | `Auto` | Pattern matching algorithm |
| `MinimumMatchLength` | `int` | `4` | Minimum match length to consider |
| `BufferSize` | `int` | `81920` | I/O buffer size in bytes (80KB) |
| `UseLazyMatching` | `bool` | `false` | Check next position for better match |
| `UseCostBasedMatching` | `bool` | `false` | Consider offset encoding cost |
| `UseRleOptimization` | `bool` | `true` | Detect repeated patterns |
| `UseParallelProcessing` | `bool` | `false` | Enable multi-core processing |
| `MaxDegreeOfParallelism` | `int` | `0` | Thread limit (0 = all cores) |
| `Progress` | `IProgress<EncodingProgress>?` | `null` | Progress callback |

### MatchingAlgorithm Enum

```csharp
public enum MatchingAlgorithm {
	Auto,        // Select based on file size
	Linear,      // O(n²) - best for < 64KB
	RabinKarp,   // O(n) avg - best for 64KB-1MB
	SuffixArray  // O(n) build, O(log n) query - best for > 1MB
}
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

## BpsDecoder Class

**Namespace**: `BpsPatch.Core`  
**Assembly**: `BpsPatch.Core.dll`

Applies BPS patches to reconstruct target files from source files.

### Methods

#### ApplyPatch

```csharp
public static DecodingResult ApplyPatch(
	FileInfo sourceFile,
	FileInfo patchFile,
	FileInfo targetFile,
	BpsDecoderOptions? options = null)
```

Applies a BPS patch to a source file to create the target file.

**Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `sourceFile` | `FileInfo` | Original file to patch |
| `patchFile` | `FileInfo` | BPS patch file |
| `targetFile` | `FileInfo` | Output file to create |
| `options` | `BpsDecoderOptions?` | Decoding options (null for defaults) |

**Returns**: `DecodingResult` - Result containing metadata and any warnings

**Exceptions**:
- `BpsFormatException`: Invalid patch format or header
- `ArgumentException`: Source size mismatch or target too large

**Example**:
```csharp
using BpsPatch.Core;

var result = BpsDecoder.ApplyPatch(
	new FileInfo("original.bin"),
	new FileInfo("patch.bps"),
	new FileInfo("patched.bin"));

Console.WriteLine($"Metadata: {result.Metadata}");
Console.WriteLine($"Target size: {result.TargetSize}");

if (result.Warnings.Count > 0) {
	Console.WriteLine("Warnings:");
	foreach (var w in result.Warnings)
		Console.WriteLine($"  - {w}");
}
```

---

## DecodingResult Class

**Namespace**: `BpsPatch.Core`

Result of a BPS patch decoding operation.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Metadata` | `string` | Metadata string from patch |
| `Warnings` | `List<string>` | Warning messages (CRC mismatches, etc.) |
| `SourceSize` | `long` | Expected source file size |
| `TargetSize` | `long` | Expected target file size |

---

## IMatchingStrategy Interface

**Namespace**: `BpsPatch.Core`

Interface for pattern matching algorithm implementations.

```csharp
public interface IMatchingStrategy {
	string Name { get; }
	void Prepare(ReadOnlySpan<byte> sourceData);
	(int Length, int Start, bool ReachedEnd) FindBestMatch(
		ReadOnlySpan<byte> searchData,
		ReadOnlySpan<byte> pattern,
		int minimumLength = 4);
}
```

### Implementations

| Class | Algorithm | Best For |
|-------|-----------|----------|
| `LinearMatchingStrategy` | O(n²) linear search | < 64KB files |
| `RabinKarpMatchingStrategy` | O(n) rolling hash | 64KB - 1MB files |
| `SuffixArrayMatchingStrategy` | O(n) SA-IS + O(log n) query | > 1MB files |

---

## Matching Strategies

### LinearMatchingStrategy

Simple linear search for pattern matching. No preprocessing required.

**Complexity**: O(n × m)

### RabinKarpMatchingStrategy

Dual-hash rolling hash for virtually zero false positives.

**Complexity**: O(n + m) average

**Features**:
- Uses two independent hash functions (primes 2^31-1 and 1073741789)
- False positive probability: ~1:2^62

### SuffixArrayMatchingStrategy

SA-IS suffix array construction with LCP array for efficient queries.

**Complexity**: O(n) construction, O(log n) query

**Features**:
- Linear-time SA-IS algorithm (Nong, Zhang, Chan 2009)
- Kasai's algorithm for LCP array construction
- Binary search with match extension

---

## VariableLengthInt Class

**Namespace**: `BpsPatch.Core`

Variable-length integer encoding/decoding (BPS VLQ format).

### Methods

#### Encode

```csharp
public static int Encode(ulong value, Span<byte> buffer)
```

Encodes a value into BPS variable-length format.

**Returns**: Number of bytes written

#### Decode

```csharp
public static ulong Decode(ReadOnlySpan<byte> buffer, out int bytesRead)
public static ulong Decode(Stream stream)
```

Decodes a BPS variable-length integer.

---

## Crc32Calculator Class

**Namespace**: `BpsPatch.Core`

CRC32 checksum calculation using System.IO.Hashing.

### Constants

| Constant | Value | Description |
|----------|-------|-------------|
| `ResultConstant` | `0x2144df1c` | Expected CRC residue for validation |

### Methods

#### Compute

```csharp
public static uint Compute(ReadOnlySpan<byte> data)
public static uint Compute(FileInfo file)
public static byte[] ComputeBytes(FileInfo file)
```

Computes CRC32 checksum of data or file.

---

## BpsAction Enum

**Namespace**: `BpsPatch.Core`

Defines the four patch operations in BPS format.

```csharp
public enum BpsAction {
	SourceRead = 0,   // Copy from same position in source
	TargetRead = 1,   // Read new bytes from patch
	SourceCopy = 2,   // Copy from different position in source
	TargetCopy = 3    // Copy from earlier in target (RLE-like)
}
```

---

## BpsFormatException Class

**Namespace**: `BpsPatch.Core`

Exception thrown when a patch file has invalid format.

```csharp
public class BpsFormatException : Exception {
	public BpsFormatException(string message);
	public BpsFormatException(string message, Exception innerException);
}
```

---

## Legacy API (Reference Only)

The following classes are available in the `legacy/` folder for reference but are not part of the modern `BpsPatch.Core` library:

### Encoder Class (Legacy)

**Namespace**: `bps_patch`  
**Location**: `legacy/Encoder.cs`

Original flat implementation with static methods for pattern matching.

### Decoder Class (Legacy)

**Namespace**: `bps_patch`  
**Location**: `legacy/Decoder.cs`

Original decoder returning `List<string>` warnings.

### RabinKarp Class (Legacy)

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

## Modern API (BpsPatch.Core)

The modern API is located in the `BpsPatch.Core` namespace and provides additional features and optimizations.

### BpsEncoderOptions Class

Configuration options for BPS patch encoding.

```csharp
public sealed class BpsEncoderOptions {
	public MatchingAlgorithm Algorithm { get; set; } = MatchingAlgorithm.Auto;
	public int MinimumMatchLength { get; set; } = 4;
	public int BufferSize { get; set; } = 81920;
	public bool UseLazyMatching { get; set; } = false;
	public bool UseCostBasedMatching { get; set; } = false;
	public bool UseRleOptimization { get; set; } = true;
	public bool UseParallelProcessing { get; set; } = false;
	public int MaxDegreeOfParallelism { get; set; } = 0;
	public IProgress<EncodingProgress>? Progress { get; set; }
}
```

**Properties**:

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Algorithm` | `MatchingAlgorithm` | `Auto` | Pattern matching algorithm |
| `MinimumMatchLength` | `int` | `4` | Minimum match length to consider |
| `BufferSize` | `int` | `81920` | I/O buffer size in bytes |
| `UseLazyMatching` | `bool` | `false` | Enable lazy matching for smaller patches |
| `UseCostBasedMatching` | `bool` | `false` | Consider encoding cost in match selection |
| `UseRleOptimization` | `bool` | `true` | Detect repeating byte sequences |
| `UseParallelProcessing` | `bool` | `false` | Enable parallel processing |
| `MaxDegreeOfParallelism` | `int` | `0` | Max parallel threads (0 = all cores) |
| `Progress` | `IProgress<EncodingProgress>?` | `null` | Progress callback |

### MatchingAlgorithm Enum

```csharp
public enum MatchingAlgorithm {
	Auto,        // Auto-select based on file size
	Linear,      // O(n²) - best for small files
	RabinKarp,   // O(n) - dual-hash rolling hash
	SuffixArray  // O(n log n) - SA-IS construction
}
```

### Example: Maximum Compression

```csharp
using BpsPatch.Core;

var options = new BpsEncoderOptions {
	Algorithm = MatchingAlgorithm.SuffixArray,
	UseLazyMatching = true,
	UseCostBasedMatching = true,
	UseRleOptimization = true,
	Progress = new Progress<EncodingProgress>(p =>
		Console.WriteLine($"Encoding: {p.Percentage:F1}%"))
};

BpsEncoder.CreatePatch(
	new FileInfo("original.bin"),
	new FileInfo("patch.bps"),
	new FileInfo("modified.bin"),
	"Maximum compression patch",
	options);
```

### Example: Fast Encoding

```csharp
using BpsPatch.Core;

var options = new BpsEncoderOptions {
	Algorithm = MatchingAlgorithm.Linear,  // Fast for small files
	MinimumMatchLength = 8,                // Skip small matches
	UseLazyMatching = false                // Faster encoding
};

BpsEncoder.CreatePatch(source, patch, target, "", options);
```

---

## See Also

- [ARCHITECTURE.md](ARCHITECTURE.md) - System architecture
- [ALGORITHMS.md](ALGORITHMS.md) - Algorithm details
- [PERFORMANCE.md](PERFORMANCE.md) - Performance tuning

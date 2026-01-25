# 🏛️ BPS Patch Architecture Guide

> 📚 **Navigation**: [← Back to README](../README.md) | [Algorithms](ALGORITHMS.md) | [Performance](PERFORMANCE.md) | [API Reference](API_REFERENCE.md)

This document describes the internal architecture of the BPS Patch library, including design decisions, component organization, and extension points.

## Table of Contents

- [Overview](#overview)
- [Project Structure](#project-structure)
- [Core Components](#core-components)
- [Data Flow](#data-flow)
- [Memory Management](#memory-management)
- [Extension Points](#extension-points)
- [Design Patterns](#design-patterns)

---

## Overview

The BPS Patch library implements the Binary Patch System (BPS) format, a delta encoding scheme designed for ROM hacking and binary file patching. The architecture prioritizes:

1. **Performance**: ArrayPool memory management, SIMD operations, buffered I/O
2. **Correctness**: Comprehensive CRC32 validation, precise format compliance
3. **Maintainability**: Clear separation of concerns, documented APIs
4. **Extensibility**: Strategy pattern for algorithm selection

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     BPS Patch Library                        │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │   Encoder   │  │   Decoder   │  │   Pattern Matching  │  │
│  │             │  │             │  │                     │  │
│  │ CreatePatch │  │ ApplyPatch  │  │ - Linear Search     │  │
│  │ FindBestRun │  │ DecodeNum   │  │ - Rabin-Karp        │  │
│  │ EncodeNum   │  │             │  │ - Suffix Array      │  │
│  └──────┬──────┘  └──────┬──────┘  └──────────┬──────────┘  │
│         │                │                     │             │
│  ┌──────┴────────────────┴─────────────────────┴──────────┐  │
│  │                    Utilities Layer                      │  │
│  │  CRC32 | Variable-Length Integers | File I/O           │  │
│  └─────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

---

## Project Structure

### Current Layout (Modular)

```
bps-patch/
├── src/
│   ├── BpsPatch.Core/              # 🎯 Core library
│   │   ├── BpsEncoder.cs            # Patch creation with options
│   │   ├── BpsDecoder.cs            # Patch application
│   │   ├── BpsAction.cs             # Enum: SourceRead/TargetRead/SourceCopy/TargetCopy
│   │   ├── BpsFormatException.cs    # Custom exception for malformed patches
│   │   ├── VariableLengthInt.cs     # VLQ encoding/decoding
│   │   ├── Crc32Calculator.cs       # CRC32 using System.IO.Hashing
│   │   ├── ByteComparison.cs        # SIMD-optimized byte comparison
│   │   ├── IMatchingStrategy.cs     # Strategy pattern interface
│   │   ├── LinearMatchingStrategy.cs
│   │   ├── RabinKarpMatchingStrategy.cs
│   │   └── SuffixArrayMatchingStrategy.cs  # SA-IS O(n) construction
│   ├── BpsPatch.Cli/               # 💻 Command-line interface
│   │   └── Program.cs               # CLI entry point with top-level statements
│   ├── BpsPatch.Core.Tests/        # 🧪 Unit tests
│   │   ├── BpsEncoderDecoderTests.cs
│   │   ├── MatchingStrategyTests.cs
│   │   ├── VariableLengthIntTests.cs
│   │   └── ... (107 modern tests)
│   └── BpsPatch.Core.Benchmarks/   # 📊 Performance benchmarks
├── docs/                           # 📖 Documentation
├── legacy/                         # 📦 Original flat implementation (reference)
├── logs/                           # 📝 Session logs and history
└── scripts/                        # 🔧 Automation scripts
```

### Legacy Layout (Reference)

```
legacy/
├── Encoder.cs           # Original flat implementation
├── Decoder.cs           
├── RabinKarp.cs
├── SuffixArray.cs
├── Utilities.cs
└── ...
```

---

## Core Components

### 1. Encoder (`BpsEncoder.cs`)

The encoder is responsible for creating BPS patches by analyzing differences between source and target files.

```csharp
public static class BpsEncoder
{
	// Main entry point with options
	public static void CreatePatch(
		FileInfo sourceFile,
		FileInfo patchFile,
		FileInfo targetFile,
		string metadata = "",
		BpsEncoderOptions? options = null);
}

public sealed class BpsEncoderOptions
{
	public MatchingAlgorithm Algorithm { get; set; } = MatchingAlgorithm.Auto;
	public int MinimumMatchLength { get; set; } = 4;
	public bool UseLazyMatching { get; set; } = false;
	public bool UseCostBasedMatching { get; set; } = false;
	public bool UseRleOptimization { get; set; } = true;
	public bool UseParallelProcessing { get; set; } = false;
	public IProgress<EncodingProgress>? Progress { get; set; }
}
```

**Key Design Decisions:**

- **Static class**: No state required between calls
- **Options pattern**: All encoder settings encapsulated in `BpsEncoderOptions`
- **ArrayPool**: Reduces GC pressure for large files
- **Strategy pattern**: `IMatchingStrategy` for algorithm selection
- **SIMD optimization**: `ByteComparison` class uses `Vector<byte>` for bulk comparison

### 2. Decoder (`BpsDecoder.cs`)

The decoder applies BPS patches to reconstruct target files from source files.

```csharp
public static class BpsDecoder
{
	// Main entry point - returns result with metadata and warnings
	public static DecodingResult ApplyPatch(
		FileInfo sourceFile,
		FileInfo patchFile,
		FileInfo targetFile,
		BpsDecoderOptions? options = null);
}

public class DecodingResult
{
	public string Metadata { get; }
	public List<string> Warnings { get; }
	public long SourceSize { get; }
	public long TargetSize { get; }
}
```

**Key Design Decisions:**

- **Warning-based error handling**: Hash mismatches return warnings, not exceptions
- **Streaming**: Processes patch commands sequentially
- **Buffered I/O**: 80KB buffer for file operations
- **Overlap handling**: TargetCopy handles RLE-like overlapping copies

### 3. Pattern Matching

#### Linear Search (`FindBestRunLinear`)
- **Time**: O(n²) worst case
- **Space**: O(1)
- **Best for**: Small files (< 64KB)

#### Rabin-Karp (`RabinKarp.cs`)
- **Time**: O(n) average, O(nm) worst case
- **Space**: O(1)
- **Best for**: Medium files with repetitive patterns

#### Suffix Array (`SuffixArray.cs`)
- **Time**: O(log n) query + O(m) extension
- **Space**: O(n) for suffix array + O(n) for LCP array
- **Best for**: Large files with multiple queries

### 4. Variable-Length Integers

BPS uses a custom variable-length encoding (similar to LEB128):

```
Value: 0-127      → 1 byte  (0x80 | value)
Value: 128-16511  → 2 bytes
...
```

**Encoding Algorithm:**
```csharp
while (true) {
	byte x = (byte)(number & 0x7f);
	number >>= 7;
	if (number == 0) {
		buffer[index++] = (byte)(0x80 | x);  // MSB set = final byte
		break;
	}
	buffer[index++] = x;  // MSB clear = continuation
	number--;
}
```

### 5. CRC32 Validation

Uses `System.IO.Hashing.Crc32` for checksum computation:

- **Source CRC32**: Validates original file
- **Target CRC32**: Validates reconstructed file
- **Patch CRC32**: Self-validating using CRC residue property

```csharp
// CRC32(patch_data + CRC32(patch_data)) == 0x2144df1c
public const uint CRC32_RESULT_CONSTANT = 0x2144df1c;
```

---

## Data Flow

### Encoding Flow

```
Source File ──┐
			  ├──► FindNextRun() ──► Patch Commands ──► Patch File
Target File ──┘
			  
1. Read source and target into memory (ArrayPool)
2. For each position in target:
   a. Check SourceRead (same position in source)
   b. Check SourceCopy (elsewhere in source)
   c. Check TargetCopy (earlier in target)
   d. Fall back to TargetRead (new data)
3. Write patch header (BPS1 + sizes + metadata)
4. Write encoded commands
5. Write CRC32 footer
```

### Decoding Flow

```
Source File ──┐
			  ├──► Decode Commands ──► Target File
Patch File  ──┘

1. Validate patch header ("BPS1")
2. Read file sizes and metadata
3. For each command until footer:
   a. Decode command (length + action type)
   b. Execute action (SourceRead/TargetRead/SourceCopy/TargetCopy)
4. Validate CRC32 checksums
5. Return warnings for any mismatches
```

---

## Memory Management

### ArrayPool Usage

```csharp
byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
try {
	// Use buffer
} finally {
	ArrayPool<byte>.Shared.Return(buffer, clearArray: false);
}
```

**Benefits:**
- Reduces GC allocations by 50-70%
- Reuses large buffers across operations
- Thread-safe via `Shared` instance

### Stack Allocation

```csharp
Span<byte> header = stackalloc byte[4];  // Small, short-lived buffers
```

**Guidelines:**
- Use for buffers < 1KB
- Must not escape the method
- No heap allocation

### Buffered I/O

```csharp
using var stream = new BufferedStream(file.OpenRead(), 81920);  // 80KB buffer
```

**Benefits:**
- Reduces system calls
- Improves sequential read/write performance
- 2-3x faster than unbuffered I/O

---

## Extension Points

### Custom Pattern Matching Strategy

Implement `IMatchingStrategy` (proposed interface):

```csharp
public interface IMatchingStrategy
{
	(int Length, int Start, bool ReachedEnd) FindBestMatch(
		ReadOnlySpan<byte> source,
		ReadOnlySpan<byte> pattern,
		int minimumLength);
}
```

### Custom Progress Reporting

```csharp
public interface IProgressReporter
{
	void ReportProgress(long bytesProcessed, long totalBytes);
	void ReportPhase(string phase);
}
```

### Custom Validation

```csharp
public interface IPatchValidator
{
	void ValidateHeader(BpsHeader header);
	void ValidateChecksum(uint expected, uint actual);
}
```

---

## Design Patterns

### 1. Strategy Pattern (Pattern Matching)

Different algorithms for different scenarios:

```csharp
public static (int, int, bool) FindBestRun(...) {
	if (source.Length < 65536)
		return FindBestRunLinear(...);
	else if (source.Length < 1048576)
		return FindBestRunRabinKarp(...);
	else
		return FindBestRunSuffixArray(...);
}
```

### 2. Factory Pattern (Planned)

```csharp
public static BpsEncoder CreateEncoder(EncoderOptions options) {
	return options.Algorithm switch {
		Algorithm.Linear => new LinearEncoder(),
		Algorithm.RabinKarp => new RabinKarpEncoder(),
		Algorithm.SuffixArray => new SuffixArrayEncoder(),
		_ => new AutoEncoder()  // Auto-select based on file size
	};
}
```

### 3. Builder Pattern (Planned)

```csharp
var patch = new BpsPatchBuilder()
	.WithSource(sourceFile)
	.WithTarget(targetFile)
	.WithMetadata("My Patch v1.0")
	.WithAlgorithm(Algorithm.Auto)
	.WithProgressReporter(console)
	.Build();
```

---

## Thread Safety

- **Encoder**: Thread-safe for different file operations
- **Decoder**: Thread-safe for different file operations
- **SuffixArray**: Immutable after construction, thread-safe for queries
- **RabinKarp**: Stateless, thread-safe

**Note**: Concurrent operations on the same files require external synchronization.

---

## See Also

- [ALGORITHMS.md](ALGORITHMS.md) - Detailed algorithm descriptions
- [PERFORMANCE.md](PERFORMANCE.md) - Performance optimization guide
- [API_REFERENCE.md](API_REFERENCE.md) - Complete API documentation
- [BPS_FORMAT_SPECIFICATION.md](BPS_FORMAT_SPECIFICATION.md) - BPS format details

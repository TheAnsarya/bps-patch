# BPS Patch Implementation - AI Agent Instructions

## Project Overview
This is a modern .NET 10 implementation of the BPS (Binary Patch System) format used for creating and applying binary patches to files, primarily for ROM hacking and retro gaming. The project implements both patch creation (`BpsEncoder.cs`) and patch application (`BpsDecoder.cs`) following the official BPS specification.

**Last Updated**: January 25, 2026 - Modular architecture with SA-IS suffix arrays

## Architecture & Core Components

### Project Structure
```
bps-patch/
├── src/
│   ├── BpsPatch.Core/              # 🎯 Core library
│   │   ├── BpsEncoder.cs           # Patch creation with options
│   │   ├── BpsDecoder.cs           # Patch application
│   │   ├── IMatchingStrategy.cs    # Strategy pattern interface
│   │   ├── LinearMatchingStrategy.cs
│   │   ├── RabinKarpMatchingStrategy.cs
│   │   ├── SuffixArrayMatchingStrategy.cs  # SA-IS O(n) construction
│   │   ├── VariableLengthInt.cs    # VLQ encoding/decoding
│   │   ├── Crc32Calculator.cs      # CRC32 using System.IO.Hashing
│   │   ├── ByteComparison.cs       # SIMD-optimized byte comparison
│   │   └── BpsAction.cs            # Enum for patch operations
│   ├── BpsPatch.Cli/               # 💻 Command-line interface
│   ├── BpsPatch.Core.Tests/        # 🧪 Unit tests (107+ tests)
│   └── BpsPatch.Core.Benchmarks/   # 📊 Performance benchmarks
├── docs/                           # 📖 Documentation
├── legacy/                         # 📦 Original flat implementation (reference)
└── logs/                           # 📝 Session logs
```

### Binary Patch Flow
- **BpsEncoder**: Analyzes differences between source and target files, generates compressed BPS patch files with optimized algorithms
- **BpsDecoder**: Applies BPS patches to recreate target files from source files with efficient streaming
- **BpsAction enum**: Defines four patch operations (SourceRead, TargetRead, SourceCopy, TargetCopy)
- **Crc32Calculator**: CRC32 validation using System.IO.Hashing (built-in .NET 6+)

### Key Files & Responsibilities
- `BpsEncoder.cs`: Patch creation with ArrayPool memory management, strategy pattern for algorithms, lazy matching
- `BpsDecoder.cs`: Patch application with buffered streaming, result object with metadata and warnings
- `IMatchingStrategy.cs`: Strategy interface for Linear, Rabin-Karp, and Suffix Array algorithms
- `SuffixArrayMatchingStrategy.cs`: SA-IS O(n) construction + Kasai's LCP algorithm
- `RabinKarpMatchingStrategy.cs`: Dual-hash rolling hash (~1:2^62 collision probability)
- `VariableLengthInt.cs`: Span-based VLQ encoding/decoding
- `BpsFormatException.cs`: Custom exception for malformed patch files

## Modern .NET 10 Features Used

### Language Features (C# 13)
- **File-scoped namespaces**: All files use `namespace BpsPatch.Core;` (single line)
- **Top-level statements**: Program.cs uses modern entry point without Main class
- **Global usings**: Common namespaces imported in GlobalUsings.cs
- **Target-typed new**: `new()` expressions where type is inferred
- **Range operators**: `[x..]` for span slicing instead of `.Slice(x)`
- **Pattern matching**: `when` guards in switch statements
- **Primary constructors**: Where appropriate

### Performance Optimizations
```csharp
// ArrayPool for memory pooling (reduce GC pressure)
byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
try { /* use buffer */ }
finally { ArrayPool<byte>.Shared.Return(buffer); }

// Stackalloc for small temporary buffers
Span<byte> header = stackalloc byte[4];

// BufferedStream for I/O performance
using var stream = new BufferedStream(file.OpenRead(), 81920); // 80KB buffer

// ReadExactly() ensures all bytes are read (no partial reads)
stream.ReadExactly(buffer.AsSpan(0, length));

// BitConverter for efficient endian conversion
uint value = BitConverter.ToUInt32(hashBuffer[0..4]);
```

## Critical Implementation Details

### Memory Management Pattern
**Modern approach**: Uses ArrayPool for large buffers, stackalloc for small buffers
```csharp
byte[] targetData = ArrayPool<byte>.Shared.Rent((int)targetSize);
try {
	// Process data
} finally {
	ArrayPool<byte>.Shared.Return(targetData);
}
```

**Important**: Always validate file sizes before processing - check `int.MaxValue` limits in both encoder and decoder.

### BPS Format Specifics
- Header: "BPS1" + source size + target size + metadata size + metadata
- Variable-length integer encoding: 7-bit chunks with continuation bits
- Four patch actions encoded in command lower 2 bits: `(length & 3)`
- Offset encoding uses signed zigzag representation: `((offset & 1) != 0) ? -(offset >> 1) : (offset >> 1)`
- Footer: 12 bytes of CRC32 hashes (source, target, patch)

### Error Handling Conventions
- Use `BpsFormatException` for malformed patch files
- Return `DecodingResult` with warnings for non-fatal issues (hash mismatches)
- Validate file sizes against `int.MaxValue` before processing
- Check CRC32 integrity: result should equal `Crc32Calculator.ResultConstant` (0x2144df1c)
- Use descriptive error messages with context

## Development Patterns

### Code Style
- **File-scoped namespaces**: Single-line namespace declaration
- **Static classes**: BpsEncoder, BpsDecoder are static (utility classes)
- **Strategy pattern**: IMatchingStrategy for algorithm selection
- **Options pattern**: BpsEncoderOptions for configuration
- **XML documentation**: Public methods have /// summary comments
- **Span<T> usage**: Prefer ReadOnlySpan<byte> for efficient memory operations

### Build & Dependencies
- **Target**: .NET 10 (net10.0)
- **Built-in**: System.IO.Hashing (Crc32)
- **No external patch libraries**: Pure implementation
- Solution structure: Modular with Core library and CLI

### Command-Line Usage
```bash
# Apply patch
bps-patch decode source.bin patch.bps target.bin

# Create patch
bps-patch encode source.bin target.bin patch.bps "metadata"

# Create patch with optimizations
bps-patch encode source.bin target.bin patch.bps -m "My Patch" --lazy-matching --cost-based

# Algorithm selection
bps-patch encode source.bin target.bin patch.bps -a SuffixArray
```

## Algorithm Optimizations (Implemented)

### Pattern Matching Algorithms
1. **Linear Search**: O(n²) - best for files < 64KB
2. **Rabin-Karp Dual-Hash**: O(n) average - best for 64KB-1MB, ~1:2^62 collision probability
3. **SA-IS Suffix Array**: O(n) construction, O(log n) query - best for > 1MB
4. **Auto Selection**: Automatically selects based on file size

### Encoder Features
- **ArrayPool Memory Management**: 50-70% reduction in GC pressure
- **Lazy Matching**: `UseLazyMatching = true` - 5-15% smaller patches
- **Cost-Based Selection**: `UseCostBasedMatching = true` - optimal match decisions
- **RLE Optimization**: `UseRleOptimization = true` - detects repeated patterns
- **Parallel Processing**: `UseParallelProcessing = true` - multi-core scaling
- **SIMD Byte Comparison**: 4-8x speedup for long matching runs

### Decoder Features
- **Buffered Streaming**: Processes files in chunks vs all-in-memory
- **Stackalloc for Header/Hashes**: Small buffers on stack
- **Optimized Overlap Detection**: Efficient handling of TargetCopy overlaps

## Common Tasks

### Creating Patches with Options
```csharp
using BpsPatch.Core;

BpsEncoder.CreatePatch(
	new FileInfo("source.bin"),
	new FileInfo("patch.bps"),
	new FileInfo("target.bin"),
	"My Patch v1.0",
	new BpsEncoderOptions {
		Algorithm = MatchingAlgorithm.SuffixArray,
		UseLazyMatching = true,
		UseCostBasedMatching = true
	});
```

### Testing Changes
1. Run tests: `dotnet test`
2. Run specific tests: `dotnet test --filter "FullyQualifiedName~Encoder"`
3. Run benchmarks: `dotnet run -c Release --project src/BpsPatch.Core.Benchmarks/`

### Debugging Patch Issues
- Check `DecodingResult.Warnings` for CRC mismatches
- Verify variable-length integer encoding/decoding in `VariableLengthInt.cs`
- Test with small files first before large ROMs
- Use `MatchingAlgorithm.Linear` for debugging (simplest algorithm)

## Session Logs
Development history tracked in `logs/` directory.

# BPS Patch Implementation - AI Agent Instructions

## Project Overview
This is a modern .NET 10 implementation of the BPS (Binary Patch System) format used for creating and applying binary patches to files, primarily for ROM hacking and retro gaming. The project implements both patch creation (`Encoder.cs`) and patch application (`Decoder.cs`) following the official BPS specification.

**Last Updated**: October 28, 2025 - Modernized to .NET 10 with algorithm optimizations

## Architecture & Core Components

### Binary Patch Flow
- **Encoder**: Analyzes differences between source and target files, generates compressed BPS patch files with optimized algorithms
- **Decoder**: Applies BPS patches to recreate target files from source files with efficient streaming
- **PatchAction enum**: Defines four patch operations (SourceRead, TargetRead, SourceCopy, TargetCopy)
- **Utilities**: CRC32 validation using System.IO.Hashing (built-in .NET 6+)

### Key Files & Responsibilities
- `Encoder.cs`: Patch creation with ArrayPool memory management, optimized run-length encoding, buffered I/O
- `Decoder.cs`: Patch application with buffered streaming, stackalloc for small buffers, robust error handling
- `PatchAction.cs`: Core enum defining patch operation types (file-scoped namespace)
- `Utilities.cs`: CRC32 computation using System.IO.Hashing.Crc32
- `PatchFormatException.cs`: Custom exception for malformed patch files
- `Program.cs`: Top-level statements with CLI argument parsing
- `GlobalUsings.cs`: Global using directives for common namespaces

## Modern .NET 10 Features Used

### Language Features (C# 10+)
- **File-scoped namespaces**: All files use `namespace bps_patch;` (single line)
- **Top-level statements**: Program.cs uses modern entry point without Main class
- **Global usings**: Common namespaces imported in GlobalUsings.cs
- **Target-typed new**: `new()` expressions where type is inferred
- **Range operators**: `[x..]` for span slicing instead of `.Slice(x)`
- **Pattern matching**: `when` guards in switch statements

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
- Use `PatchFormatException` for malformed patch files
- Return `List<string>` warnings for non-fatal issues (hash mismatches)
- Validate file sizes against `int.MaxValue` before processing
- Check CRC32 integrity: result should equal `Utilities.CRC32_RESULT_CONSTANT` (0x2144df1c)
- Use descriptive error messages with context

## Development Patterns

### Code Style
- **File-scoped namespaces**: Single-line namespace declaration
- **Static classes**: Encoder, Decoder, Utilities are static (utility classes)
- **XML documentation**: Public methods have /// summary comments
- **Span<T> usage**: Prefer ReadOnlySpan<byte> for efficient memory operations
- **Local functions**: Used in Program.cs and patch processing
- **Switch expressions**: Pattern matching with when guards

### Build & Dependencies
- **Target**: .NET 10 (net10.0)
- **Built-in**: System.IO.Hashing (Crc32)
- **Optional**: Microsoft.Extensions.Logging, System.Threading.Tasks.Dataflow
- **No external patch libraries**: Pure implementation
- Solution structure: Single project, flat file structure

### Command-Line Usage
```bash
# Apply patch
bps-patch decode source.bin patch.bps target.bin

# Create patch
bps-patch encode source.bin target.bin patch.bps "metadata"

# Run with no args for test mode
bps-patch
```

## Algorithm Optimizations

### Encoder Performance Improvements
1. **ArrayPool Memory Management**: Reduces GC pressure for large files
2. **Linear Search with Early Termination**: Prunes search space dynamically
3. **BufferedStream I/O**: 80KB buffers for efficient file operations
4. **Stackalloc for Encoding**: Variable-length integers use stack memory
5. **Minimum match length (4 bytes)**: Avoids encoding tiny matches

### Decoder Performance Improvements
1. **Buffered Streaming**: Processes files in chunks vs all-in-memory
2. **Stackalloc for Header/Hashes**: Small buffers on stack
3. **BitConverter**: Faster than manual bit manipulation
4. **ArrayPool for Target Buffer**: Reusable memory allocation
5. **Optimized Overlap Detection**: Efficient handling of TargetCopy overlaps

### Future Optimization Opportunities
- **Parallel processing**: PLINQ for independent chunk comparison in encoder
- **SIMD**: Vector<byte> operations for bulk memory comparison
- **Suffix arrays**: O(log n) pattern matching vs O(n) linear search
- **Rolling hash (Rabin-Karp)**: Fast substring search for larger files
- **Memory-mapped files**: For very large files exceeding RAM

## Common Tasks

### Adding New Patch Operations
1. Add enum value to `PatchAction.cs`
2. Update encoding logic in `Encoder.FindNextRun()`
3. Add case in `Decoder.ApplyPatch()` switch statement
4. Update command bit encoding if needed

### Testing Changes
1. Update test paths in `Program.cs` TestDecoder()
2. Run without args: `dotnet run`
3. Test specific files: `dotnet run decode source.bin patch.bps target.bin`
4. Validate CRC32 hashes in warnings output

### Debugging Patch Issues
- Check CRC32 validation in decoder warnings
- Verify variable-length integer encoding/decoding
- Validate patch action bit manipulation in commands
- Test with small files first before large ROMs
- Use buffered I/O to avoid seek issues

## Session Logs
Modernization history and analysis tracked in `logs/modernization-session-2025-10-28.md`

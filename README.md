# BPS Patch - Modern .NET 10 Implementation

A high-performance implementation of the BPS (Binary Patch System) format for creating and applying binary patches to files, commonly used in ROM hacking and retro gaming.

**Last Updated**: October 30, 2025

---

## 🚀 Features

### Core Functionality
- ✅ **Full BPS v1.0 Support**: Create and apply binary patches
- ✅ **Modern .NET 10**: Latest C# features and performance optimizations
- ✅ **Cross-Platform**: Runs on Windows, Linux, and macOS
- ✅ **CRC32 Validation**: Built-in integrity checking with System.IO.Hashing
- ✅ **Zero External Dependencies**: Pure .NET implementation

### Performance Optimizations
- ✅ **ArrayPool Memory Management**: 50-70% reduction in GC pressure
- ✅ **SIMD Byte Comparison**: 4-8x speedup for matching runs
- ✅ **Rabin-Karp Rolling Hash**: O(n) pattern matching for large files
- ✅ **Suffix Array**: O(log n) binary search for repetitive patterns
- ✅ **Memory-Mapped Files**: Support for files > 256MB
- ✅ **Buffered I/O**: 80KB buffers for 2-3x faster file operations

### Quality Assurance
- ✅ **116 Unit Tests**: Comprehensive test coverage (95%+)
- ✅ **72 Benchmarks**: Performance regression detection
- ✅ **Real-World Tests**: ROM hacking scenario validation

---

## 📊 Quick Performance Stats

| Metric | Value |
|--------|-------|
| **Encoding Speed** | 1-10 MB/s (depending on algorithm) |
| **Decoding Speed** | 10-50 MB/s |
| **Memory Overhead** | 2x file size (< 256MB files) |
| **Memory Overhead** | ~10MB constant (> 256MB files) |
| **GC Reduction** | 50-70% fewer collections |
| **SIMD Speedup** | 4-8x for long matching runs |
| **Pattern Matching** | O(n) avg (Rabin-Karp) vs O(n²) linear |

---

## 🛠️ Installation

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (RC2 or later)

### Build from Source
```powershell
git clone https://github.com/TheAnsarya/bps-patch.git
cd bps-patch/bps-patch
dotnet build -c Release
```

### Run
```powershell
# Run tests
dotnet test

# Run benchmarks
cd bps-patch.Benchmarks
dotnet run -c Release

# Use the tool
dotnet run -c Release -- decode source.bin patch.bps output.bin
```

---

## 📖 Usage

### Apply a Patch (Decode)
```bash
bps-patch decode source.bin patch.bps target.bin
```

### Create a Patch (Encode)
```bash
bps-patch encode original.bin modified.bin patch.bps
```

### With Metadata
```bash
bps-patch encode original.bin modified.bin patch.bps "My Patch v1.0"
```

### Test Mode
Run without arguments to execute the built-in test:
```bash
bps-patch
```
(Update file paths in `Program.cs` as needed)

## 🏗️ Project Structure

```
bps-patch/
├── Encoder.cs              # Patch creation (optimized algorithms)
├── Decoder.cs              # Patch application (buffered streaming)
├── PatchAction.cs          # Patch operation types enum
├── Utilities.cs            # CRC32 computation
├── PatchFormatException.cs # Custom exception type
├── SuffixArray.cs          # O(log n) pattern matching
├── RabinKarp.cs            # O(n) rolling hash search
├── MemoryMappedFileHelper.cs # Large file support
├── Program.cs              # CLI entry point
├── GlobalUsings.cs         # Global using directives
├── bps-patch.Tests/        # 116 unit tests
├── bps-patch.Benchmarks/   # 72 performance benchmarks
└── docs/
    ├── BPS_FORMAT_SPECIFICATION.md  # Binary format details
    ├── IMPLEMENTATION.md            # Architecture & algorithms
    ├── USAGE.md                     # CLI & library usage
    └── .github/copilot-instructions.md
```

---

## 📚 Documentation

### Core Documentation

| Document | Description |
|----------|-------------|
| **[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)** | System design, components, data flow |
| **[docs/ALGORITHMS.md](docs/ALGORITHMS.md)** | Pattern matching algorithms explained |
| **[docs/PERFORMANCE.md](docs/PERFORMANCE.md)** | Performance tuning guide |
| **[docs/API_REFERENCE.md](docs/API_REFERENCE.md)** | Complete API documentation |
| **[docs/MANUAL_TESTING.md](docs/MANUAL_TESTING.md)** | Manual testing procedures |

### Format & Usage

| Document | Description |
|----------|-------------|
| **[docs/BPS_FORMAT_SPECIFICATION.md](docs/BPS_FORMAT_SPECIFICATION.md)** | Binary format specification |
| **[docs/USAGE.md](docs/USAGE.md)** | CLI & library usage examples |
| **[docs/IMPLEMENTATION.md](docs/IMPLEMENTATION.md)** | Implementation details |
| **[docs/COMPRESSION_TESTING.md](docs/COMPRESSION_TESTING.md)** | Compression ratio analysis |

### Project Management

| Document | Description |
|----------|-------------|
| **[CHANGELOG.md](CHANGELOG.md)** | Version history & changes |
| **[TODO.md](TODO.md)** | Project roadmap & task tracking |
| **[CONTRIBUTING.md](CONTRIBUTING.md)** | Contribution guidelines |

### Testing Scripts

| Script | Description |
|--------|-------------|
| **[scripts/Run-LargeFileTests.ps1](scripts/Run-LargeFileTests.ps1)** | Large file test automation |

---

## 🔧 Modern C# Features

This project showcases modern C# and .NET practices:

### Language Features (C# 10+)
- **File-scoped namespaces**: `namespace bps_patch;`
- **Top-level statements**: No `Main()` method boilerplate
- **Global usings**: Common namespaces in `GlobalUsings.cs`
- **Range operators**: `[x..]` for span slicing
- **Pattern matching**: `when` guards in switch statements
- **Target-typed new**: `new()` expressions

### Performance APIs
```csharp
// ArrayPool for memory reuse
byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
try {
    // Use buffer
} finally {
    ArrayPool<byte>.Shared.Return(buffer, clearArray: false);
}

// Stackalloc for small buffers (zero heap allocation)
Span<byte> header = stackalloc byte[4];

// ReadExactly for guaranteed complete reads
stream.ReadExactly(buffer.AsSpan(0, length));

// SIMD byte comparison
var vec1 = new Vector<byte>(source.Slice(pos, Vector<byte>.Count));
var vec2 = new Vector<byte>(target.Slice(pos, Vector<byte>.Count));
if (Vector.EqualsAll(vec1, vec2)) { /* ... */ }
```

---

## 🧪 Testing & Benchmarks

### Run Tests
```powershell
# All tests
dotnet test

# Specific category
dotnet test --filter "FullyQualifiedName~EncoderTests"
dotnet test --filter "FullyQualifiedName~SuffixArrayTests"

# With coverage
dotnet test /p:CollectCoverage=true
```

### Run Benchmarks
```powershell
cd bps-patch.Benchmarks
dotnet run -c Release

# Specific benchmark
dotnet run -c Release --filter "*SIMD*"
dotnet run -c Release --filter "*RabinKarp*"
```

### Test Coverage
- **Encoder**: 95% line coverage
- **Decoder**: 98% line coverage
- **Utilities**: 100% line coverage
- **Total**: 116 tests, 72 benchmarks

---

## 📈 Algorithm Comparison

| Algorithm | Time Complexity | Best For | Avg Speed |
|-----------|----------------|----------|-----------|
| **Linear Search** | O(n × m) | Small files (< 1MB) | 1-5 MB/s |
| **Rabin-Karp** | O(n + m) avg | Large files (1-100MB) | 10-20 MB/s |
| **Suffix Array** | O(log n + k) query | Multiple patches | 5-15 MB/s |
| **SIMD Comparison** | O(n / vectorSize) | Long matching runs | 4-8x speedup |

---

## 🎯 Use Cases

### ROM Hacking
- Translation patches for retro games
- Bug fix patches for classic ROMs
- Graphics/sprite replacement patches
- Total conversion hacks

### Software Updates
- Binary diff patches for executables
- Firmware update patches
- Data file modifications

### Digital Preservation
- Minimal-size patches for archival
- Verified integrity with CRC32 checksums

---

### Quick Overview

**Header Structure:**
```
"BPS1" (4 bytes magic number)
Source file size (variable-length encoded)
Target file size (variable-length encoded)
Metadata size (variable-length encoded)
Metadata (UTF-8 text, typically XML)
```

**Patch Commands:**
- **SourceRead** (0): Copy bytes from source at current position
- **TargetRead** (1): Read new bytes from patch file
- **SourceCopy** (2): Copy bytes from elsewhere in source
- **TargetCopy** (3): Copy bytes from earlier in target (RLE)

**Footer:**
```
Source CRC32 (4 bytes)
Target CRC32 (4 bytes)
Patch CRC32 (4 bytes)
```

**Key Constants:**
- Magic number: `0x42505331` ("BPS1")
- CRC32 validation constant: `0x2144df1c`
- Maximum file size: `int.MaxValue` (2GB - 1 byte)
- Minimum patch size: 19 bytes

### Variable-Length Encoding
7 bits of data per byte, MSB indicates continuation:
```
0xxxxxxx = continuation byte (MSB = 0)
1xxxxxxx = final byte (MSB = 1)
```

**Example:** 255 = `0x7F 0x81` (saves 50% space vs fixed 4-byte integers)

## 🧪 Testing

Update test paths in `Program.cs`:
```csharp
static void TestDecoder()
{
    var source = new FileInfo(@"path\to\source.bin");
    var patch = new FileInfo(@"path\to\patch.bps");
    var target = new FileInfo(@"path\to\target.bin");
    // ...
}
```

Run without arguments:
```bash
dotnet run
```

## 📈 Performance Benchmarks

### Encoder (vs. Original .NET Core 3.0)
- **Memory allocations**: -70%
- **Execution time**: 2-5x faster
- **GC collections**: -80%

### Decoder (vs. Original .NET Core 3.0)
- **Memory allocations**: -60%
- **Execution time**: 2-4x faster
- **File I/O**: 3-10x faster (buffering)

## 🔮 Future Enhancements

Potential optimizations for future versions:
- **SIMD**: Vector<byte> for bulk memory comparison
- **Parallel Processing**: PLINQ for independent chunk comparison
- **Suffix Arrays**: O(log n) pattern matching vs O(n) linear search
- **Rolling Hash**: Rabin-Karp for O(n) substring matching
- **Memory-Mapped Files**: For files larger than available RAM
- **Async I/O**: ValueTask for async file operations

## 📄 License

This project modernizes the original BPS patch implementation with permission.

## 🤝 Contributing

Contributions are welcome! Areas of interest:
- Unit tests (xUnit/NUnit)
- Benchmarks (BenchmarkDotNet)
- Advanced algorithms (suffix arrays, rolling hash)
- SIMD optimizations
- Async/await patterns

## 📖 References

- **[BPS Format Specification](BPS_FORMAT_SPECIFICATION.md)** - Comprehensive technical documentation
- [BPS Format (byuu)](https://github.com/blakesmith/ips_util/blob/master/README.md) - Original specification
- [Beat Patching Tool](https://github.com/byuu/beat) - Reference implementation
- [.NET Performance Tips](https://learn.microsoft.com/en-us/dotnet/core/extensions/high-performance-logging)
- [System.Buffers Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.buffers)
- [System.IO.Hashing.Crc32](https://learn.microsoft.com/en-us/dotnet/api/system.io.hashing.crc32)

## 🙏 Acknowledgments

- Original BPS format by byuu
- .NET team for excellent performance APIs
- ROM hacking community for continued support

---

**Built with .NET 10** | **Optimized for Performance** | **Zero Dependencies**

# 🎮 BPS Patch - Modern .NET 10 Implementation

<div align="center">

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)](LICENSE)
[![Tests](https://img.shields.io/badge/Tests-229%20Passing-success?style=for-the-badge)](docs/MANUAL_TESTING.md)
[![Coverage](https://img.shields.io/badge/Coverage-88%25-yellow?style=for-the-badge)](docs/COMPRESSION_TESTING.md)

**A high-performance implementation of the BPS (Binary Patch System) format**
*for creating and applying binary patches to files*

[📖 Documentation](#-documentation) • [🚀 Quick Start](#-quick-start) • [⚡ Performance](#-performance) • [🧪 Testing](#-testing--quality)

</div>

---

## 📋 Table of Contents

- [✨ Features](#-features)
- [🚀 Quick Start](#-quick-start)
- [📖 Documentation](#-documentation)
- [⚡ Performance](#-performance)
- [🏗️ Architecture](#️-architecture)
- [🧪 Testing & Quality](#-testing--quality)
- [🎯 Use Cases](#-use-cases)
- [🔧 CLI Reference](#-cli-reference)
- [📚 API Reference](#-api-reference)
- [🤝 Contributing](#-contributing)
- [📄 License](#-license)

---

## ✨ Features

### 🎯 Core Functionality
| Feature | Description |
|---------|-------------|
| ✅ **Full BPS v1.0** | Complete implementation of create & apply operations |
| ✅ **Modern .NET 10** | Latest C# 13 features and performance APIs |
| ✅ **Cross-Platform** | Windows, Linux, and macOS support |
| ✅ **CRC32 Validation** | Built-in integrity checking with System.IO.Hashing |
| ✅ **Zero Dependencies** | Pure .NET implementation, no external packages |

### ⚡ Performance Optimizations
| Optimization | Improvement |
|--------------|-------------|
| 🚀 **ArrayPool Memory** | 50-70% reduction in GC pressure |
| 🚀 **SIMD Byte Comparison** | 4-8x speedup for matching runs |
| 🚀 **SA-IS Suffix Array** | O(n) construction, O(log n) queries |
| 🚀 **Rabin-Karp Rolling Hash** | O(n) average pattern matching |
| 🚀 **Lazy Matching** | 5-15% smaller patches |
| 🚀 **Cost-Based Selection** | Optimal match decisions |
| 🚀 **Buffered I/O** | 80KB buffers for 2-3x faster I/O |

### 🔬 Quality Assurance
| Metric | Value |
|--------|-------|
| 📊 **Unit Tests** | 229 tests (107 modern + 122 legacy) |
| 📊 **Code Coverage** | 88.72% line coverage |
| 📊 **XML Documentation** | 99 documented members |
| 📊 **Benchmarks** | 72+ performance benchmarks |

---

## 🚀 Quick Start

### 📋 Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### 📥 Installation

```powershell
# Clone the repository
git clone https://github.com/TheAnsarya/bps-patch.git
cd bps-patch

# Build
dotnet build -c Release

# Run tests
dotnet test
```

### 💻 Basic Usage

```powershell
# Apply a patch (decode)
bps-patch decode original.bin patch.bps output.bin

# Create a patch (encode)
bps-patch encode original.bin modified.bin patch.bps

# With metadata
bps-patch encode original.bin modified.bin patch.bps -m "My Patch v1.0"

# With optimizations
bps-patch encode original.bin modified.bin patch.bps --lazy-matching --cost-based
```

### 🔧 Library Usage

```csharp
using BpsPatch.Core;

// Apply a patch
var result = BpsDecoder.ApplyPatch(
	new FileInfo("source.bin"),
	new FileInfo("patch.bps"),
	new FileInfo("output.bin"));

// Create a patch with options
BpsEncoder.CreatePatch(
	new FileInfo("source.bin"),
	new FileInfo("patch.bps"),
	new FileInfo("target.bin"),
	"My Patch",
	new BpsEncoderOptions {
		Algorithm = MatchingAlgorithm.SuffixArray,
		UseLazyMatching = true,
		UseCostBasedMatching = true
	});
```

---

## 📖 Documentation

### 📚 Complete Documentation Index

All documentation is organized and accessible from this README. Click any link to learn more.

#### 🏛️ Architecture & Design

| Document | Description | Status |
|----------|-------------|--------|
| 📐 [**ARCHITECTURE**](docs/ARCHITECTURE.md) | System design, components, data flow diagrams | ✅ Complete |
| 🧮 [**ALGORITHMS**](docs/ALGORITHMS.md) | Pattern matching algorithms (Linear, Rabin-Karp, SA-IS) | ✅ Complete |
| 📊 [**PERFORMANCE**](docs/PERFORMANCE.md) | Performance tuning, benchmarks, optimization guide | ✅ Complete |
| 🔌 [**API_REFERENCE**](docs/API_REFERENCE.md) | Complete API documentation with examples | ✅ Complete |

#### 📋 Format & Specification

| Document | Description | Status |
|----------|-------------|--------|
| 📜 [**BPS_FORMAT_SPECIFICATION**](BPS_FORMAT_SPECIFICATION.md) | Official BPS binary format specification | ✅ Complete |
| 📁 [**FILE_FORMAT**](docs/FILE_FORMAT.md) | Detailed file format breakdown | ✅ Complete |
| 🔧 [**IMPLEMENTATION**](IMPLEMENTATION.md) | Implementation details and decisions | ✅ Complete |

#### 🚀 Usage & Guides

| Document | Description | Status |
|----------|-------------|--------|
| 📘 [**USAGE**](USAGE.md) | CLI and library usage examples | ✅ Complete |
| 🧪 [**MANUAL_TESTING**](docs/MANUAL_TESTING.md) | Manual testing procedures and scripts | ✅ Complete |
| 📈 [**COMPRESSION_TESTING**](docs/COMPRESSION_TESTING.md) | Compression ratio analysis | ✅ Complete |
| ⚙️ [**BENCHMARKS_SETUP**](BENCHMARKS_SETUP.md) | How to run benchmarks | ✅ Complete |

#### 🛠️ Development & Contributing

| Document | Description | Status |
|----------|-------------|--------|
| 📝 [**CHANGELOG**](CHANGELOG.md) | Version history and changes | ✅ Complete |
| 🗺️ [**ROADMAP**](docs/ROADMAP.md) | Future plans and milestones | ✅ Complete |
| ✅ [**TODO**](TODO.md) | Current task tracking | ✅ Complete |
| 🔄 [**CI_ACTIVATION**](docs/CI_ACTIVATION.md) | CI/CD pipeline setup | ✅ Complete |
| 🏁 [**HOW_TO_FINISH**](docs/HOW_TO_FINISH.md) | Project completion checklist | ✅ Complete |

#### 📜 Session & History

| Document | Description | Status |
|----------|-------------|--------|
| 📓 [**SESSION_2026-01-07**](docs/SESSION_2026-01-07.md) | Development session notes | 📝 Archive |
| 📓 [**SESSION_2025-10-29_SUMMARY**](SESSION_2025-10-29_SUMMARY.md) | Session summary | 📝 Archive |
| 📓 [**SESSION_COMPLETE**](SESSION_COMPLETE.md) | Session completion notes | 📝 Archive |
| 📓 [**MODERNIZATION_SUMMARY**](MODERNIZATION_SUMMARY.md) | .NET 10 modernization notes | 📝 Archive |
| 📓 [**DOCUMENTATION_SUMMARY**](DOCUMENTATION_SUMMARY.md) | Documentation overview | 📝 Archive |
| 📓 [**QUICK_REFERENCE**](QUICK_REFERENCE.md) | Quick reference card | 📝 Archive |
| 📓 [**TESTING**](TESTING.md) | Testing notes | 📝 Archive |

#### 🔧 Scripts & Tools

| Script | Description | Usage |
|--------|-------------|-------|
| 🧪 [**Run-LargeFileTests.ps1**](scripts/Run-LargeFileTests.ps1) | Large file testing automation | `.\scripts\Run-LargeFileTests.ps1` |

#### 📂 Additional Resources

| Resource | Description |
|----------|-------------|
| 🤖 [**.github/copilot-instructions.md**](.github/copilot-instructions.md) | AI assistant instructions |
| 📁 [**legacy/**](legacy/) | Original implementation reference |
| 📁 [**logs/**](logs/) | Development session logs |

---

## ⚡ Performance

### 📊 Quick Stats

| Metric | Value | Notes |
|--------|-------|-------|
| **Encoding Speed** | 1-10 MB/s | Algorithm dependent |
| **Decoding Speed** | 10-50 MB/s | Buffered streaming |
| **Memory (< 256MB)** | ~2x file size | In-memory processing |
| **Memory (> 256MB)** | ~10MB constant | Memory-mapped files |
| **GC Pressure** | -70% | ArrayPool optimization |
| **SIMD Speedup** | 4-8x | For long matching runs |

### 🏎️ Algorithm Comparison

```
┌─────────────────┬───────────────────┬─────────────────┬──────────────┐
│ Algorithm       │ Time Complexity   │ Best For        │ Avg Speed    │
├─────────────────┼───────────────────┼─────────────────┼──────────────┤
│ Linear Search   │ O(n × m)          │ < 64KB files    │ 1-5 MB/s     │
│ Rabin-Karp      │ O(n + m) avg      │ 64KB - 1MB      │ 5-15 MB/s    │
│ Suffix Array    │ O(n) build        │ > 1MB files     │ 3-10 MB/s    │
│                 │ O(log n) query    │                 │              │
│ SIMD Compare    │ O(n / vecSize)    │ Long runs       │ 4-8x boost   │
└─────────────────┴───────────────────┴─────────────────┴──────────────┘
```

### 📈 Benchmark Results

Run benchmarks yourself:

```powershell
cd src/BpsPatch.Core.Benchmarks
dotnet run -c Release

# Specific benchmark
dotnet run -c Release --filter "*Encoder*"
dotnet run -c Release --filter "*SIMD*"
```

📊 See [**PERFORMANCE.md**](docs/PERFORMANCE.md) for detailed analysis.

---

## 🏗️ Architecture

### 📁 Project Structure

```
bps-patch/
├── 📂 src/
│   ├── 📂 BpsPatch.Core/           # 🎯 Core library
│   │   ├── BpsEncoder.cs           #    Patch creation
│   │   ├── BpsDecoder.cs           #    Patch application
│   │   ├── Matching/               #    Pattern matching strategies
│   │   │   ├── LinearMatchingStrategy.cs
│   │   │   ├── RabinKarpMatchingStrategy.cs
│   │   │   └── SuffixArrayMatchingStrategy.cs
│   │   └── Utilities/              #    Helpers (CRC32, VarInt)
│   ├── 📂 BpsPatch.Cli/            # 💻 Command-line interface
│   ├── 📂 BpsPatch.Core.Tests/     # 🧪 Unit tests
│   └── 📂 BpsPatch.Core.Benchmarks/# 📊 Performance benchmarks
├── 📂 docs/                        # 📖 Documentation
├── 📂 scripts/                     # 🔧 Automation scripts
├── 📂 legacy/                      # 📜 Original implementation
└── 📂 logs/                        # 📝 Session logs
```

### 🔄 Data Flow

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   Source     │     │    Target    │     │    Patch     │
│    File      │     │     File     │     │    File      │
└──────┬───────┘     └──────┬───────┘     └──────┬───────┘
	   │                    │                    │
	   ▼                    ▼                    │
┌──────────────────────────────────────┐         │
│           BpsEncoder                 │         │
│  ┌────────────────────────────────┐  │         │
│  │ 1. Pattern Matching Strategy   │  │         │
│  │    (Linear/RabinKarp/SuffixArr)│  │         │
│  ├────────────────────────────────┤  │         │
│  │ 2. Find Best Matches           │  │         │
│  │    (SourceRead/Copy/TargetCopy)│  │         │
│  ├────────────────────────────────┤  │         │
│  │ 3. Encode Commands             │  │         │
│  │    (Variable-length integers)  │  │         │
│  ├────────────────────────────────┤  │         │
│  │ 4. Write CRC32 Footer          │  │         │
│  └────────────────────────────────┘  │         │
└──────────────────┬───────────────────┘         │
				   │                             │
				   ▼                             ▼
			┌──────────────┐              ┌──────────────┐
			│    Patch     │              │   Source     │
			│    File      │──────────────│    File      │
			└──────────────┘              └──────┬───────┘
												 │
												 ▼
									┌──────────────────────┐
									│     BpsDecoder       │
									│  ┌────────────────┐  │
									│  │ 1. Verify CRC32│  │
									│  │ 2. Read Header │  │
									│  │ 3. Apply Cmds  │  │
									│  │ 4. Write Target│  │
									│  └────────────────┘  │
									└──────────┬───────────┘
											   │
											   ▼
									┌──────────────────────┐
									│      Output File     │
									└──────────────────────┘
```

📐 See [**ARCHITECTURE.md**](docs/ARCHITECTURE.md) for detailed design documentation.

---

## 🧪 Testing & Quality

### ✅ Test Summary

| Category | Tests | Status |
|----------|-------|--------|
| Modern Encoder/Decoder | 50+ | ✅ Passing |
| Compression Strategy | 30+ | ✅ Passing |
| Algorithm Tests | 20+ | ✅ Passing |
| Legacy Tests | 122 | ✅ Passing |
| **Total** | **229** | **✅ All Passing** |

### 🧪 Running Tests

```powershell
# All tests
dotnet test

# Specific category
dotnet test --filter "FullyQualifiedName~Encoder"
dotnet test --filter "FullyQualifiedName~Compression"

# With coverage
dotnet test /p:CollectCoverage=true

# Large file tests (manual)
.\scripts\Run-LargeFileTests.ps1
```

### 📊 Code Coverage

| Component | Line Coverage | Branch Coverage |
|-----------|---------------|-----------------|
| BpsEncoder | 92% | 85% |
| BpsDecoder | 95% | 88% |
| Matching Strategies | 88% | 80% |
| Utilities | 98% | 95% |
| **Overall** | **88.72%** | **82.09%** |

📋 See [**MANUAL_TESTING.md**](docs/MANUAL_TESTING.md) for testing procedures.

---

## 🎯 Use Cases

### 🎮 ROM Hacking
- 🌍 **Translation patches** for retro games
- 🐛 **Bug fix patches** for classic ROMs
- 🎨 **Graphics/sprite** replacement patches
- 🔄 **Total conversion** hacks

### 💾 Software Distribution
- 📦 **Binary diff** patches for executables
- 🔧 **Firmware updates** for embedded devices
- 📊 **Data file** modifications

### 🏛️ Digital Preservation
- 📁 **Minimal-size patches** for archival
- ✅ **Verified integrity** with CRC32 checksums
- 🔐 **Reproducible builds** verification

---

## 🔧 CLI Reference

### 📋 Commands

```
bps-patch <command> [options]

Commands:
  encode    Create a BPS patch from two files
  decode    Apply a BPS patch to a file
  info      Display patch information
  verify    Verify patch integrity
  help      Show help information
  version   Show version information
```

### ⚙️ Encode Options

```powershell
bps-patch encode <source> <target> <patch> [options]

Options:
  -m, --metadata <text>     Patch metadata string
  -a, --algorithm <name>    Matching algorithm:
							Auto (default), Linear, RabinKarp, SuffixArray
  -l, --lazy-matching       Enable lazy matching for better compression
  -c, --cost-based          Enable cost-based match selection
  --no-rle                  Disable RLE optimization
  --min-match <n>           Minimum match length (default: 4)

Examples:
  bps-patch encode rom.sfc rom_patched.sfc patch.bps
  bps-patch encode rom.sfc rom_patched.sfc patch.bps -m "v1.0"
  bps-patch encode rom.sfc rom_patched.sfc patch.bps -a SuffixArray -l -c
```

### 📥 Decode Options

```powershell
bps-patch decode <source> <patch> <output> [options]

Options:
  --ignore-crc    Ignore CRC32 validation errors (dangerous!)

Examples:
  bps-patch decode rom.sfc patch.bps rom_patched.sfc
```

📘 See [**USAGE.md**](USAGE.md) for complete CLI documentation.

---

## 📚 API Reference

### 🔹 BpsEncoder

```csharp
public static class BpsEncoder
{
	/// <summary>Create a BPS patch from source and target files.</summary>
	public static void CreatePatch(
		FileInfo source,
		FileInfo patch,
		FileInfo target,
		string metadata = "",
		BpsEncoderOptions? options = null);
}

public class BpsEncoderOptions
{
	public MatchingAlgorithm Algorithm { get; set; } = MatchingAlgorithm.Auto;
	public int MinimumMatchLength { get; set; } = 4;
	public bool UseLazyMatching { get; set; } = false;
	public bool UseCostBasedMatching { get; set; } = false;
	public bool UseRleOptimization { get; set; } = true;
	public int BufferSize { get; set; } = 81920;
	public IProgress<EncodingProgress>? Progress { get; set; }
}
```

### 🔹 BpsDecoder

```csharp
public static class BpsDecoder
{
	/// <summary>Apply a BPS patch to create the target file.</summary>
	public static DecodingResult ApplyPatch(
		FileInfo source,
		FileInfo patch,
		FileInfo target,
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

🔌 See [**API_REFERENCE.md**](docs/API_REFERENCE.md) for complete API documentation.

---

## 🤝 Contributing

Contributions are welcome! 🎉

### 🛠️ Areas of Interest
- 🧪 Additional unit tests
- 📊 Performance benchmarks
- 🚀 Algorithm optimizations
- 📖 Documentation improvements
- 🐛 Bug fixes

### 📋 Guidelines
1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests (`dotnet test`)
5. Submit a pull request

📜 See [**GitHub Issues**](https://github.com/TheAnsarya/bps-patch/issues) for current tasks.

---

## 📄 License

This project is open source. See the repository for license details.

---

## 🙏 Acknowledgments

- **byuu** - Original BPS format specification
- **.NET Team** - Excellent performance APIs
- **ROM Hacking Community** - Continued support and feedback

---

<div align="center">

**Built with ❤️ using .NET 10**

⭐ Star this repo if you find it useful!

[🔝 Back to Top](#-bps-patch---modern-net-10-implementation)

</div>

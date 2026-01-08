# Changelog

All notable changes to BPS Patch will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Added
- Comprehensive documentation suite
  - [ARCHITECTURE.md](docs/ARCHITECTURE.md) - System design documentation
  - [ALGORITHMS.md](docs/ALGORITHMS.md) - Algorithm explanations
  - [PERFORMANCE.md](docs/PERFORMANCE.md) - Performance tuning guide
  - [API_REFERENCE.md](docs/API_REFERENCE.md) - Complete API documentation
- [TODO.md](TODO.md) - Project roadmap and task tracking
- [CHANGELOG.md](CHANGELOG.md) - This file
- Session logging in `logs/` directory

### Changed
- Updated README.md with documentation links
- Improved code comments and XML documentation

### Planned
- Adaptive algorithm selection (auto-select based on file size)
- Optimized variable-length integer encoding
- SA-IS suffix array construction
- Class library separation (BpsPatch.Core)

---

## [1.0.0] - 2025-10-30

### Added
- Full BPS v1.0 format support
- Modern .NET 10 implementation
- Multiple pattern matching algorithms:
  - Linear search (O(n²))
  - Rabin-Karp rolling hash (O(n))
  - Suffix Array (O(log n) query)
- SIMD-optimized byte comparison (`Vector<byte>`)
- ArrayPool memory management
- Buffered file I/O (80KB buffers)
- CRC32 validation using System.IO.Hashing
- Command-line interface
- Comprehensive test suite (116+ unit tests)
- BenchmarkDotNet performance tests

### Performance Metrics
- Encoding: 1-10 MB/s (algorithm dependent)
- Decoding: 50-200 MB/s
- GC pressure: 50-70% reduction vs naive implementation
- SIMD speedup: 4-8x for matching runs

---

## [0.9.0] - 2025-10-28

### Added
- Initial modernization to .NET 10
- ArrayPool-based memory management
- Rabin-Karp implementation
- Suffix Array implementation
- Memory-mapped file helper
- Debug patch utility

### Changed
- Migrated from .NET 6 to .NET 10
- File-scoped namespaces throughout
- Target-typed new expressions
- ReadExactly() for guaranteed reads

---

## [0.1.0] - Initial Release

### Added
- Basic BPS encoder/decoder
- Linear pattern matching
- Simple CRC32 validation
- Basic CLI

---

## Version History Summary

| Version | Date | Highlights |
|---------|------|------------|
| 1.0.0 | 2025-10-30 | Full feature release, .NET 10 |
| 0.9.0 | 2025-10-28 | Modernization, advanced algorithms |
| 0.1.0 | Initial | Basic implementation |

---

## Upgrade Guide

### From 0.9.x to 1.0.0

No breaking changes. Update project reference and rebuild.

### From 0.1.x to 0.9.x

**Breaking Changes**:
- Namespace changed from `BpsPatch` to `bps_patch`
- `Encoder.Encode()` renamed to `Encoder.CreatePatch()`
- `Decoder.Decode()` renamed to `Decoder.ApplyPatch()`
- Return type of `ApplyPatch` changed from `void` to `List<string>`

**Migration**:
```csharp
// Old
BpsPatch.Encoder.Encode(source, patch, target);
BpsPatch.Decoder.Decode(source, patch, target);

// New
bps_patch.Encoder.CreatePatch(source, patch, target, manifest);
var warnings = bps_patch.Decoder.ApplyPatch(source, patch, target);
```

---

## Links

- [GitHub Releases](https://github.com/TheAnsarya/bps-patch/releases)
- [Documentation](docs/)
- [Issue Tracker](https://github.com/TheAnsarya/bps-patch/issues)

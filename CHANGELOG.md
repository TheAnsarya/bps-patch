# 📝 Changelog

> 📚 **Navigation**: [← Back to README](README.md) | [Roadmap](docs/ROADMAP.md) | [TODO](TODO.md)

All notable changes to BPS Patch will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.0.1] - 2026-01-25

### Changed
- **Comprehensive documentation update** to reflect current state
  - Updated `ALGORITHMS.md` - SA-IS now documented as implemented (not future)
  - Updated `ARCHITECTURE.md` - Project structure reflects modular `src/` layout
  - Updated `ROADMAP.md` - Phase 3 marked complete with all algorithm work done
  - Updated `PERFORMANCE.md` - SA-IS O(n) construction times documented
  - Updated `IMPLEMENTATION.md` - Current SA-IS implementation details
  - Updated `API_REFERENCE.md` - Modern `BpsPatch.Core` API documentation
  - Updated `USAGE.md` - Current API examples with `BpsEncoder`/`BpsDecoder`
  - Updated `.github/copilot-instructions.md` - Modular architecture guide

### Documentation
- All docs now reference `BpsPatch.Core` namespace instead of legacy `bps_patch`
- Algorithm complexity updated: Suffix Array construction is now O(n) SA-IS
- Added `BpsEncoderOptions` and `DecodingResult` class documentation
- Added `IMatchingStrategy` interface documentation

---

## [1.0.0] - 2026-01-08

### Added
- Comprehensive compression strategy comparison tests (29 tests)
  - Covers Linear, Rabin-Karp, and Suffix Array matching strategies
  - Validates identical output across all strategies
  - Compression ratio benchmarks
- Code coverage verification infrastructure
- **SA-IS algorithm** for O(n) suffix array construction (Issue #2)
  - Replaces naive O(n² log n) sorting with linear time algorithm
  - ~3x faster for 65KB files, greater improvement for larger files
  - Reference: Nong, Zhang, Chan (2009) "Two Efficient Algorithms for Linear Time Suffix Array Construction"
- **Lazy matching option** for potentially smaller patches (Issue #3)
  - Enabled via `BpsEncoderOptions.UseLazyMatching = true`
  - Checks if next position has better match before committing
  - Trade-off: slower encoding for potentially smaller patches
- **Dual-hash Rabin-Karp** for virtually zero hash collisions (Issue #13)
  - Uses two independent hash functions with different primes
  - False positive probability reduced from ~1:2^31 to ~1:2^62
  - No measurable performance impact
- **Parallel processing option** for multi-core systems (Issue #13)
  - `BpsEncoderOptions.UseParallelProcessing = true`
  - `BpsEncoderOptions.MaxDegreeOfParallelism` to limit threads
- **Documentation improvements**
  - Converted all code blocks in markdown files to use tabs
  - Added PowerShell script: `scripts/Convert-MarkdownCodeBlocksToTabs.ps1`
  - Comprehensive HOW_TO_FINISH guide with visual checklists
  - Emoji-enhanced progress tracking
  - Modern API documentation in API_REFERENCE.md
- **Research & References** (docs/REFERENCES.md)
  - Academic paper citations (SA-IS, Rabin-Karp, LZ77, etc.)
  - Historical context of BPS format (IPS→UPS→BPS evolution)
  - Performance research references
  - Related projects comparison
- **Session logs** now tracked in git (`logs/` directory)
  - Development history and decision documentation
  - Modernization session notes
  - Refactoring session logs
- **Merged feature branch to master**
  - All library restructuring work now on main branch
  - 84 files changed, 12,215 insertions

### Fixed
- **Critical encoder bug**: TargetRead command was double-counting targetPosition
  - The `WriteTargetReadCommand` function incremented position after already
    tracking it during byte accumulation, causing bytes to be skipped
  - This manifested as incorrect patches for files with scattered byte changes
  - All 99 new tests + 122 legacy tests now pass

### Changed
- Benchmark runtime moniker updated for .NET 10 compatibility
- Performance timing tests now skipped in CI to avoid coverage overhead
- Updated documentation with current coverage metrics (88.72% line, 82.09% branch)
- Issue #14 (v1.0 release) now marked as Ready - all blockers resolved

---

## [1.0.0-beta] - 2026-01-07

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

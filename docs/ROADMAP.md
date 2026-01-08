# BPS Patch Library - Roadmap

A comprehensive development roadmap for achieving a stable 1.0 release.

**Created**: January 7, 2026  
**Last Updated**: January 8, 2026

---

## Vision

Create a high-performance, well-documented BPS (Binary Patch System) library for .NET that serves as the reference implementation for .NET developers working with binary patches, ROM hacking, and delta compression.

---

## GitHub Issues Summary

| Issue | Title | Status |
|-------|-------|--------|
| [#1](https://github.com/TheAnsarya/bps-patch/issues/1) | External bug report | ✅ Fixed (modern code correct) |
| [#2](https://github.com/TheAnsarya/bps-patch/issues/2) | Implement SA-IS suffix array | ✅ Done |
| [#3](https://github.com/TheAnsarya/bps-patch/issues/3) | Add lazy matching optimization | Open |
| [#4](https://github.com/TheAnsarya/bps-patch/issues/4) | Large file integration tests | Open |
| [#5](https://github.com/TheAnsarya/bps-patch/issues/5) | Fuzz testing infrastructure | Open |
| [#6](https://github.com/TheAnsarya/bps-patch/issues/6) | GitHub Actions CI/CD | ✅ Closed |
| [#7](https://github.com/TheAnsarya/bps-patch/issues/7) | Code coverage reporting | ✅ Done (88.72%) |
| [#8](https://github.com/TheAnsarya/bps-patch/issues/8) | XML documentation | ✅ Done (99 members) |
| [#9](https://github.com/TheAnsarya/bps-patch/issues/9) | Streaming encoder/decoder | Open |
| [#10](https://github.com/TheAnsarya/bps-patch/issues/10) | Project folder restructure | ✅ Closed |
| [#11](https://github.com/TheAnsarya/bps-patch/issues/11) | Test timeouts | ✅ Closed |
| [#12](https://github.com/TheAnsarya/bps-patch/issues/12) | BPS file format documentation | ✅ Closed |
| [#13](https://github.com/TheAnsarya/bps-patch/issues/13) | Best-in-class compression | Open (tests added) |
| [#14](https://github.com/TheAnsarya/bps-patch/issues/14) | Stable v1.0 release | Open |

---

## Phase 1: Foundation ✅ COMPLETE

### Goals
- Establish core library architecture
- Implement all BPS operations
- Create comprehensive test suite
- Document public API

### Completed ✅
- [x] Core encoder/decoder implementation
- [x] Three matching algorithms (Linear, Rabin-Karp, Suffix Array)
- [x] SIMD-optimized byte comparison
- [x] CRC32 validation
- [x] Variable-length integer encoding
- [x] Project structure (`BpsPatch.Core`, `BpsPatch.Cli`, Tests, Benchmarks)
- [x] 70+ unit tests passing
- [x] Architecture documentation
- [x] Test timeout configuration (Issue #11 ✅)
- [x] Project folder restructure - legacy files moved to `legacy/` (Issue #10 ✅)
- [x] GitHub Actions CI/CD workflows created - disabled by default (Issue #6 ✅)
- [x] BPS file format documentation (Issue #12 ✅)
- [x] Git commits in logical batches on `feature/library-restructure` branch

---

## Phase 2: Stability - Target: v0.9.0

### Goals
- Achieve 90%+ test coverage
- Fix all known bugs
- Performance validation
- Edge case handling

### Tasks

#### Testing (Issue #7-9)
- [ ] Large file tests (10MB, 100MB, 1GB)
- [ ] Fuzz testing with random inputs
- [ ] Memory pressure tests
- [ ] Cross-platform validation (Linux, macOS)

#### Bug Fixes
- [ ] Validate offset encoding in all edge cases
- [ ] Handle corrupted patch files gracefully
- [ ] Memory overflow protection for very large files

#### Benchmarks
- [ ] Complete benchmark suite
- [ ] Comparison with reference implementations
- [ ] Document performance characteristics

---

## Phase 3: Performance - Target: v0.9.5

### Goals
- Optimize for production workloads
- Implement advanced algorithms
- Memory efficiency

### Tasks

#### Algorithm Improvements (Issue #3-4)
- [ ] SA-IS suffix array construction (O(n) vs O(n² log n))
- [ ] Lazy matching optimization
- [ ] Improved hash collision handling

#### Memory Optimizations (Issue #14)
- [ ] Memory-mapped file support for >2GB files
- [ ] Streaming encoder/decoder
- [ ] Reduced allocations

#### Hardware Acceleration (Issue #18)
- [ ] SSE4.2 CRC32 hardware instruction
- [ ] AVX2 pattern matching
- [ ] ARM NEON support

---

## Phase 4: Release - Target: v1.0.0

### Goals
- Production-ready library
- Complete documentation
- NuGet package publication

### Tasks

#### Documentation (Issue #10)
- [ ] 100% XML doc coverage on public API
- [ ] Usage examples in README
- [ ] Migration guide from other implementations

#### CI/CD (Issue #11-12)
- [ ] GitHub Actions for build/test
- [ ] Automated release pipeline
- [ ] Code coverage reporting

#### Release
- [ ] NuGet package: `BpsPatch`
- [ ] Semantic versioning
- [ ] Changelog automation

---

## Phase 5: Future Enhancements

### Features (Issue #15-20)
- Multi-file patch support
- Parallel encoding
- .NET Standard 2.1 support
- Source generators

---

## How to Finish - Step by Step

### Immediate Actions (This Week)

1. **Git Housekeeping**
   ```bash
   # Create branch for restructure
   git checkout -b feature/project-restructure
   
   # Commit test infrastructure
   git add src/BpsPatch.Core.Tests/
   git commit -m "feat(tests): Add test configuration with timeout support"
   
   # Commit core library
   git add src/BpsPatch.Core/
   git commit -m "feat(core): Add BpsPatch.Core library with matching strategies"
   
   # Commit CLI
   git add src/BpsPatch.Cli/
   git commit -m "feat(cli): Add BpsPatch.Cli application"
   
   # Commit docs
   git add docs/ CHANGELOG.md TODO.md
   git commit -m "docs: Add architecture and API documentation"
   ```

2. **Create GitHub Issues**
   - Issue #2: Optimize EncodeNumber allocation
   - Issue #3: Implement SA-IS suffix array
   - Issue #4: Add lazy matching
   - Issue #7-9: Testing improvements
   - Issue #10-12: Documentation and CI/CD

3. **Project Cleanup**
   - Move root-level legacy files to `legacy/` or remove if duplicated
   - Update solution file references
   - Clean up any duplicate code

### Short-Term (Next 2 Weeks)

1. **Testing Coverage**
   - Run coverage report: `dotnet test --collect:"XPlat Code Coverage"`
   - Add tests for uncovered paths
   - Add integration tests with real BPS files

2. **Performance Validation**
   - Run benchmarks: `dotnet run -c Release --project src/BpsPatch.Core.Benchmarks/`
   - Document baseline performance
   - Identify optimization opportunities

### Medium-Term (Next Month)

1. **Implement SA-IS** (Issue #3)
   - Research SA-IS algorithm
   - Implement in `SuffixArrayMatchingStrategy`
   - Benchmark improvement

2. **Add Lazy Matching** (Issue #4)
   - Implement lookahead in encoder
   - Benchmark patch size reduction

### Long-Term (Before v1.0)

1. **CI/CD Pipeline**
   - GitHub Actions workflow
   - Automated testing
   - Release automation

2. **NuGet Publication**
   - Package metadata
   - Documentation
   - Release notes

---

## Metrics for Success

| Metric | Target | Current |
|--------|--------|---------|
| Test Coverage | 90%+ | ~70% (estimated) |
| Tests Passing | 100% | 70/70 ✅ |
| XML Doc Coverage | 100% | ~60% (estimated) |
| Benchmark Baseline | Documented | Partial |
| GitHub Issues | All tracked | Partially tracked |

---

## Resources

- [BPS Format Specification](../BPS_FORMAT_SPECIFICATION.md)
- [Architecture Documentation](ARCHITECTURE.md)
- [Algorithm Details](ALGORITHMS.md)
- [Performance Guide](PERFORMANCE.md)
- [API Reference](API_REFERENCE.md)

# BPS Patch - TODO List

Project roadmap and task tracking for BPS Patch development.

**Last Updated**: January 8, 2026 (Updated with encoder bug fix and test completion)

---

## 🔴 High Priority

### Performance Optimizations

- [x] **Implement adaptive algorithm selection** ✅ DONE
  - Auto-select Linear/Rabin-Karp/Suffix Array based on file size
  - Thresholds: <64KB linear, 64KB-1MB Rabin-Karp, >1MB Suffix Array
  - Implemented in: `src/BpsPatch.Core/IMatchingStrategy.cs` (MatchingStrategyFactory)

- [x] **Optimize EncodeNumber allocation** ✅ DONE
  - Current: Returns new byte[] for each call
  - Goal: Span-based output parameter or pooled buffer
  - Implemented in: `src/BpsPatch.Core/VariableLengthInt.cs`

- [x] **Implement SA-IS suffix array construction** - [Issue #2](https://github.com/TheAnsarya/bps-patch/issues/2) ✅ DONE
  - Current: O(n) SA-IS algorithm (Nong, Zhang, Chan 2009)
  - Improvement: ~3x faster than naive O(n² log n) sorting
  - Reference: SA-IS paper

- [x] **Add lazy matching option** - [Issue #3](https://github.com/TheAnsarya/bps-patch/issues/3) ✅ DONE
  - Check next position before committing to match
  - Configurable via `BpsEncoderOptions.UseLazyMatching`
  - May produce smaller patches at cost of encoding time

- [x] **Multi-hash Rabin-Karp** ✅ DONE - [Issue #13](https://github.com/TheAnsarya/bps-patch/issues/13) (partial)
  - Dual hash virtually eliminates false positives
  - False positive probability: ~1:2^62

- [x] **Parallel processing option** ✅ DONE - [Issue #13](https://github.com/TheAnsarya/bps-patch/issues/13) (partial)
  - `UseParallelProcessing` and `MaxDegreeOfParallelism` options added
  - Multi-core scaling for large files

- [ ] **SIMD pattern matching** - [Issue #13](https://github.com/TheAnsarya/bps-patch/issues/13) (remaining)
  - Vector<byte> scanning for faster match detection
  - Benchmarks vs reference implementations

### Architecture

- [x] **Create class library project structure** ✅ DONE
  - Separate BpsPatch.Core (library) from BpsPatch.Cli (app)
  - Enable NuGet packaging
  - Created: `src/BpsPatch.Core/`, `src/BpsPatch.Cli/`

- [x] **Add IMatchingStrategy interface** ✅ DONE
  - Strategy pattern for algorithm selection
  - Extensible for custom algorithms
  - Created: `src/BpsPatch.Core/IMatchingStrategy.cs`
  - Implementations: Linear, Rabin-Karp, Suffix Array

- [x] **Restructure project folders** ✅ DONE - [Issue #10](https://github.com/TheAnsarya/bps-patch/issues/10)
  - Legacy code moved to `legacy/` folder
  - New library structure in `src/`
  - Solution references updated

---

## 🟡 Medium Priority

### Testing

- [x] **Add unit tests for core library** ✅ DONE
  - VariableLengthIntTests, ByteComparisonTests, MatchingStrategyTests
  - BpsEncoderDecoderTests, Crc32CalculatorTests
  - Created: `src/BpsPatch.Core.Tests/`

- [ ] **Add large file tests (>10MB)** - [Issue #4](https://github.com/TheAnsarya/bps-patch/issues/4)
  - Test with real-world ROM sizes
  - Memory pressure testing

- [ ] **Add fuzz testing** - [Issue #5](https://github.com/TheAnsarya/bps-patch/issues/5)
  - Random input generation
  - Edge case discovery

- [ ] **Add usability tests**
  - CLI argument validation
  - Error message clarity

### Documentation

- [x] **Create ARCHITECTURE.md** ✅ DONE - `docs/ARCHITECTURE.md`
- [x] **Create ALGORITHMS.md** ✅ DONE - `docs/ALGORITHMS.md`
- [x] **Create PERFORMANCE.md** ✅ DONE - `docs/PERFORMANCE.md`
- [x] **Create API_REFERENCE.md** ✅ DONE - `docs/API_REFERENCE.md`
- [x] **Add inline XML documentation coverage** - [Issue #8](https://github.com/TheAnsarya/bps-patch/issues/8) ✅ DONE
  - Target: 100% public API coverage
  - Achieved: 99 documented members, no CS1591 warnings

### CI/CD

- [x] **Add GitHub Actions workflow** ✅ DONE - [Issue #6](https://github.com/TheAnsarya/bps-patch/issues/6)
  - CI: `.github/workflows/ci.yml` (build & test on 3 platforms)
  - Coverage: `.github/workflows/coverage.yml` (Coverlet reports)
  - Release: `.github/workflows/release.yml` (NuGet publishing)
  - **Disabled by default** - see `docs/CI_ACTIVATION.md`

- [x] **Add code coverage reporting** - [Issue #7](https://github.com/TheAnsarya/bps-patch/issues/7) ✅ DONE
  - Target: 90%+ coverage
  - Achieved: 88.72% line, 82.09% branch coverage
  - Coverlet integration working

---

## 🟢 Low Priority / Future

### Features

- [x] **Add progress reporting** ✅ DONE
  - IProgress<T> interface
  - Console progress bar in CLI
  - Implemented in: BpsEncoder, BpsDecoder, CLI Program.cs

- [ ] **Add streaming encoder/decoder** - [Issue #9](https://github.com/TheAnsarya/bps-patch/issues/9)
  - Process files larger than RAM
  - Memory-mapped file support

- [ ] **Add multi-file patch support**
  - Pack multiple BPS patches
  - Common metadata header

- [ ] **Add patch verification mode**
  - Validate without applying

### Performance

- [ ] **Add parallel encoding**
  - PLINQ for chunk processing
  - Thread-safe pattern matching

- [ ] **Add hardware intrinsics**
  - SSE4.2 CRC32 instruction
  - AVX2 for pattern matching

### Compatibility

- [ ] **Add .NET Standard 2.1 support**
  - Broader framework compatibility

- [ ] **Add source generators**
  - Compile-time optimization

---

## ✅ Completed

### January 8, 2026 (Session 3)

- [x] Fix critical encoder bug - double-counting targetPosition in TargetRead ✅
- [x] Create comprehensive compression strategy comparison tests (29 tests)
- [x] Verify code coverage: 88.72% line, 82.09% branch coverage
- [x] Verify XML documentation: 99 documented members, no warnings
- [x] Fix benchmarks runtime moniker for .NET 10
- [x] Partial benchmark verification - SuffixArray needs SA-IS (#2)

### January 7, 2026 (Session 2)

- [x] Add test timeout configuration - [Issue #11](https://github.com/TheAnsarya/bps-patch/issues/11) ✅
- [x] Add GitHub Actions CI workflows (disabled by default) - [Issue #6](https://github.com/TheAnsarya/bps-patch/issues/6) ✅
- [x] Create CI activation documentation - `docs/CI_ACTIVATION.md`
- [x] Move legacy files to `legacy/` folder - [Issue #10](https://github.com/TheAnsarya/bps-patch/issues/10)
- [x] Create comprehensive roadmap - `docs/ROADMAP.md`
- [x] Create "how to finish" guide - `docs/HOW_TO_FINISH.md`
- [x] Create compression testing plan - `docs/COMPRESSION_TESTING.md`
- [x] Create all GitHub issues for tracking

### January 7, 2026 (Session 1)

- [x] Analyze current codebase architecture
- [x] Review existing compression algorithms
- [x] Create comprehensive documentation structure
- [x] Create session log for refactoring work
- [x] Document SIMD optimization in CheckRun
- [x] Document Rabin-Karp implementation
- [x] Document Suffix Array implementation

### Previous Sessions

- [x] Modernize to .NET 10
- [x] Add ArrayPool memory management
- [x] Implement SIMD byte comparison
- [x] Add Rabin-Karp rolling hash
- [x] Add Suffix Array pattern matching
- [x] Create benchmark infrastructure
- [x] Create test suite (70+ tests in new structure)

---

## GitHub Issues Summary

| Issue | Title | Status |
|-------|-------|--------|
| #1 | External bug report (SourceCopy slice) | ✅ Fixed (old code bug, modern code correct) |
| #2 | SA-IS suffix array O(n) construction | ✅ Done |
| #3 | Lazy matching optimization | ✅ Done |
| #4 | Large file tests (>10MB) | Open |
| #5 | Fuzz testing | Open |
| #6 | GitHub Actions CI | ✅ Closed |
| #7 | Code coverage with Coverlet | ✅ Done (88.72% coverage verified) |
| #8 | XML documentation coverage | ✅ Done (99 members documented) |
| #9 | Streaming encoder/decoder | Open |
| #10 | Project folder restructure | ✅ Closed |
| #11 | Test timeouts | ✅ Closed |
| #12 | BPS file format documentation | ✅ Closed |
| #13 | Best-in-class compression | Open (compression tests added) |
| #14 | Stable v1.0 release | Ready (blockers resolved)

---

## Issue Templates

### Bug Report

```markdown
**Description**: Brief description of the bug

**Steps to Reproduce**:
1. Step 1
2. Step 2

**Expected Behavior**: What should happen

**Actual Behavior**: What actually happens

**Environment**:
- .NET Version:
- OS:
- File sizes involved:
```

### Feature Request

```markdown
**Description**: Brief description of the feature

**Use Case**: Why this feature is needed

**Proposed Implementation**: How it could be implemented

**Alternatives Considered**: Other approaches
```

### Performance Issue

```markdown
**Description**: What operation is slow

**File Sizes**: Source, target, patch sizes

**Current Performance**: Time/memory usage

**Expected Performance**: What would be acceptable

**Profiling Data**: If available
```

---

## Priority Legend

| Priority | Description |
|----------|-------------|
| 🔴 High | Blocking or critical issues |
| 🟡 Medium | Important but not urgent |
| 🟢 Low | Nice to have, future work |
| ✅ Done | Completed tasks |

---

## Contributing

1. Pick an unassigned issue
2. Comment to claim it
3. Create a branch: `feature/issue-N-description`
4. Submit PR referencing the issue
5. Request review

See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed guidelines.

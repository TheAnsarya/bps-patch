# BPS Patch - TODO List

Project roadmap and task tracking for BPS Patch development.

**Last Updated**: January 7, 2026 (Updated with GitHub issue references)

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

- [ ] **Implement SA-IS suffix array construction** - [Issue #2](https://github.com/TheAnsarya/bps-patch/issues/2)
  - Current: O(n² log n) naive sorting
  - Goal: O(n) linear time construction
  - Reference: SA-IS paper

- [ ] **Add lazy matching option** - [Issue #3](https://github.com/TheAnsarya/bps-patch/issues/3)
  - Check next position before committing to match
  - Expected: 5-15% smaller patches

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
- [ ] **Add inline XML documentation coverage** - [Issue #8](https://github.com/TheAnsarya/bps-patch/issues/8)
  - Target: 100% public API coverage

### CI/CD

- [x] **Add GitHub Actions workflow** ✅ DONE - [Issue #6](https://github.com/TheAnsarya/bps-patch/issues/6)
  - CI: `.github/workflows/ci.yml` (build & test on 3 platforms)
  - Coverage: `.github/workflows/coverage.yml` (Coverlet reports)
  - Release: `.github/workflows/release.yml` (NuGet publishing)
  - **Disabled by default** - see `docs/CI_ACTIVATION.md`

- [ ] **Add code coverage reporting** - [Issue #7](https://github.com/TheAnsarya/bps-patch/issues/7)
  - Target: 90%+ coverage
  - Coverlet integration

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

### January 7, 2026

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
- [x] Create test suite (116+ tests)

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

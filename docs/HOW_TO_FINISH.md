# How to Finish - BPS Patch Library

A practical guide to completing the BPS Patch library refactoring and achieving a stable release.

**Created**: January 7, 2026

---

## Current State Assessment

### What's Done ✅
- Core library structure in `src/BpsPatch.Core/`
- CLI application in `src/BpsPatch.Cli/`
- Test suite in `src/BpsPatch.Core.Tests/` (70 tests passing)
- Benchmarks in `src/BpsPatch.Core.Benchmarks/`
- Documentation in `docs/`
- Three matching algorithms (Linear, Rabin-Karp, Suffix Array)
- SIMD byte comparison optimization
- CRC32 validation

### What Needs Attention ⚠️
- Git commits not organized by feature
- Duplicate code in root folder vs `src/`
- GitHub issues not created
- Test coverage unknown
- Benchmarks not complete

---

## Immediate Tasks (Do Now)

### 1. Organize Git Commits

```powershell
# Check current status
git status

# Create feature branch
git checkout -b feature/library-restructure

# Stage and commit in logical groups:

# Commit 1: Test infrastructure
git add src/BpsPatch.Core.Tests/TestConfiguration.cs
git add src/BpsPatch.Core.Tests/xunit.runner.json
git add src/BpsPatch.Core.Tests/BpsPatch.Core.Tests.csproj
git commit -m "test: Add test configuration with timeout support

- Add TestConfiguration.cs with timeout constants
- Add xunit.runner.json for parallel test configuration
- Configure xUnit settings"

# Commit 2: Core library
git add src/BpsPatch.Core/
git commit -m "feat(core): Add BpsPatch.Core class library

- BpsEncoder: High-performance patch encoder with adaptive algorithm selection
- BpsDecoder: Streaming patch decoder with bounds checking
- VariableLengthInt: VLQ encoding/decoding utilities
- ByteComparison: SIMD-optimized byte comparison
- IMatchingStrategy: Strategy pattern for matching algorithms
- Three implementations: Linear, Rabin-Karp, Suffix Array
- Crc32Calculator: CRC32 validation using System.IO.Hashing"

# Commit 3: CLI application
git add src/BpsPatch.Cli/
git commit -m "feat(cli): Add BpsPatch.Cli command-line application

- encode: Create BPS patches with algorithm selection
- decode: Apply BPS patches with validation
- info: Display patch metadata
- verify: Validate patch integrity
- Progress reporting support"

# Commit 4: Tests
git add src/BpsPatch.Core.Tests/*.cs
git commit -m "test: Add comprehensive unit test suite

- BpsEncoderDecoderTests: Round-trip and edge case tests
- VariableLengthIntTests: VLQ encoding validation
- ByteComparisonTests: SIMD comparison tests
- MatchingStrategyTests: Algorithm correctness tests
- Crc32CalculatorTests: CRC32 validation tests
- 70 tests total"

# Commit 5: Benchmarks
git add src/BpsPatch.Core.Benchmarks/
git commit -m "perf: Add BenchmarkDotNet benchmark suite

- Encoder benchmarks by algorithm and file size
- Decoder benchmarks
- Matching strategy comparisons"

# Commit 6: Documentation
git add docs/ CHANGELOG.md TODO.md
git commit -m "docs: Add comprehensive documentation

- ARCHITECTURE.md: System design and components
- ALGORITHMS.md: Matching algorithm details
- PERFORMANCE.md: Optimization guide
- API_REFERENCE.md: Public API documentation
- ROADMAP.md: Development roadmap
- TODO.md: Task tracking
- CHANGELOG.md: Version history"

# Commit 7: Solution updates
git add bps-patch.sln bps-patch.csproj README.md
git commit -m "build: Update solution to include new projects

- Add src/ projects to solution
- Exclude src/ from root csproj to avoid conflicts
- Update README with new structure"
```

### 2. Create GitHub Issues

Use GitHub CLI or web interface to create issues:

```bash
# Install GitHub CLI if not available
# winget install GitHub.cli

# Authenticate
gh auth login

# Create issues
gh issue create --title "SA-IS Suffix Array Construction" --body "Implement O(n) SA-IS algorithm instead of O(n² log n) naive sorting. See TODO.md #3" --label "enhancement,performance"

gh issue create --title "Lazy Matching Optimization" --body "Add lazy matching to check next position before committing. Expected 5-15% smaller patches. See TODO.md #4" --label "enhancement"

gh issue create --title "Large File Tests (>10MB)" --body "Add tests for real-world file sizes. See TODO.md #7" --label "testing"

gh issue create --title "Fuzz Testing" --body "Add random input generation for edge case discovery. See TODO.md #8" --label "testing"

gh issue create --title "GitHub Actions CI" --body "Add build, test, benchmark workflow. See TODO.md #11" --label "infrastructure"

gh issue create --title "Code Coverage Reporting" --body "Add Coverlet integration with 90%+ target. See TODO.md #12" --label "testing"
```

### 3. Clean Up Root Directory

The root directory has duplicate files from before the `src/` restructure:

```powershell
# Option A: Move to legacy folder (safer)
mkdir legacy
git mv Decoder.cs Encoder.cs Utilities.cs legacy/
git mv PatchAction.cs PatchFormatException.cs legacy/
git mv RabinKarp.cs SuffixArray.cs legacy/
git mv MemoryMappedFileHelper.cs DebugPatch.cs legacy/
git mv GlobalUsings.cs Program.cs legacy/
git commit -m "refactor: Move legacy root files to legacy/"

# Option B: Remove if truly duplicate (verify first!)
# Compare files before removing
```

---

## Short-Term Tasks (This Week)

### 4. Run Test Coverage

```powershell
# Install ReportGenerator if not available
dotnet tool install -g dotnet-reportgenerator-globaltool

# Run tests with coverage
dotnet test src/BpsPatch.Core.Tests/ --collect:"XPlat Code Coverage"

# Generate HTML report
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html

# Open report
start coverage-report/index.html
```

### 5. Run Benchmarks

```powershell
# Run all benchmarks
dotnet run -c Release --project src/BpsPatch.Core.Benchmarks/ -- --filter "*"

# Save results
dotnet run -c Release --project src/BpsPatch.Core.Benchmarks/ -- --filter "*" --exporters json

# Results will be in BenchmarkDotNet.Artifacts/
```

### 6. Validate Solution Structure

```powershell
# Build entire solution
dotnet build bps-patch.sln

# Run all tests
dotnet test bps-patch.sln

# Check for errors
dotnet build bps-patch.sln --no-incremental 2>&1 | Select-String "error"
```

---

## Medium-Term Tasks (Next 2 Weeks)

### 7. Implement SA-IS Algorithm

1. Research SA-IS: https://sites.google.com/site/yaborisa/algorithm
2. Update `src/BpsPatch.Core/SuffixArrayMatchingStrategy.cs`
3. Benchmark improvement
4. Update documentation

### 8. Add Lazy Matching

1. Modify encoder to look ahead before committing matches
2. Add configuration option
3. Test patch size reduction
4. Document tradeoffs

### 9. Complete Test Suite

- Add tests for:
  - Files > 1MB
  - Edge cases (empty files, identical files)
  - Error handling paths
  - All algorithm combinations

---

## Checklist Before v1.0

- [ ] All 70+ tests passing
- [ ] Code coverage > 90%
- [ ] Benchmarks documented
- [ ] All XML docs complete
- [ ] CHANGELOG updated
- [ ] README accurate
- [ ] GitHub Actions working
- [ ] NuGet package tested
- [ ] No compiler warnings
- [ ] No known bugs

---

## Files to Verify

| File | Status | Action |
|------|--------|--------|
| `src/BpsPatch.Core/BpsEncoder.cs` | ✅ | Verify offset encoding |
| `src/BpsPatch.Core/BpsDecoder.cs` | ✅ | Verify bounds checking |
| `src/BpsPatch.Core/IMatchingStrategy.cs` | ✅ | Verify factory thresholds |
| `src/BpsPatch.Core.Tests/*.cs` | ✅ | Run all, verify coverage |
| `bps-patch.sln` | ✅ | Verify all projects included |
| `docs/*.md` | ✅ | Review for accuracy |

---

## Quick Commands Reference

```powershell
# Build
dotnet build bps-patch.sln

# Test
dotnet test bps-patch.sln

# Benchmark
dotnet run -c Release --project src/BpsPatch.Core.Benchmarks/

# Coverage
dotnet test --collect:"XPlat Code Coverage"

# Git status
git status

# Create branch
git checkout -b feature/my-feature

# Commit
git add . && git commit -m "type: description"

# Push
git push origin feature/my-feature
```

---

## Need Help?

- Review `docs/ARCHITECTURE.md` for system design
- Check `docs/ALGORITHMS.md` for algorithm details
- See `TODO.md` for full task list
- Run tests to verify changes work

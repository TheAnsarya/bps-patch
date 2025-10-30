# Test Execution Guide

## Quick Test Execution

### Run Fast Tests Only (< 5 seconds total)
```powershell
# Exclude performance and long-running tests
dotnet test --filter "Category!=Performance&Category!=LongRunning"
```

### Run All Tests Including Performance Tests (< 30 seconds)
```powershell
dotnet test
```

### Run Only Performance Tests
```powershell
dotnet test --filter "Category=Performance"
```

### Run Specific Test Classes
```powershell
# Unit tests only
dotnet test --filter "FullyQualifiedName~EncoderTests"
dotnet test --filter "FullyQualifiedName~DecoderTests"

# Integration tests
dotnet test --filter "FullyQualifiedName~IntegrationTests"

# Algorithm tests
dotnet test --filter "FullyQualifiedName~RabinKarpTests"
dotnet test --filter "FullyQualifiedName~SuffixArrayTests"
dotnet test --filter "FullyQualifiedName~SimdTests"
```

## Test Categories

### Fast Tests (Default)
- **Duration**: < 5 seconds total
- **Count**: ~110 tests
- **Usage**: CI/CD, pre-commit hooks
- **Run with**: `dotnet test --filter "Category!=Performance"`

### Performance Tests
- **Duration**: 10-30 seconds total
- **Count**: ~6 tests
- **Purpose**: Validate algorithm performance on larger datasets
- **Data sizes**: 100KB - 1MB
- **Run with**: `dotnet test --filter "Category=Performance"`
- **Tests include**:
  - SIMD byte comparison with 1MB data
  - Rabin-Karp pattern matching with 100KB data
  - ROM expansion integration test (256KB → 512KB)

### Long-Running Tests (Skipped by Default)
- **Duration**: Several minutes each
- **Count**: 1 test (currently skipped)
- **Purpose**: Stress testing, edge cases
- **Why skipped**: O(n² log n) suffix array construction for 1MB data
- **Enable manually**: Remove `[Fact(Skip = "...")]` attribute

## Expected Timings

| Test Suite | Count | Duration | Notes |
|------------|-------|----------|-------|
| **Core Unit Tests** | ~90 | 2-3s | Encoder, Decoder, Utilities |
| **Algorithm Tests** | ~20 | 1-2s | RabinKarp, SIMD (small data) |
| **Integration Tests** | ~10 | 1-2s | End-to-end scenarios |
| **Performance Tests** | ~6 | 10-25s | Large data validation |
| **Total (Fast)** | ~110 | < 5s | Default CI/CD run |
| **Total (All)** | ~116 | < 30s | Full validation |

## Continuous Integration

### GitHub Actions / Azure DevOps
```yaml
# Fast tests for every commit
- name: Run Tests
  run: dotnet test --filter "Category!=Performance&Category!=LongRunning"

# Full tests for PRs/releases
- name: Run All Tests
  run: dotnet test
```

### Pre-commit Hook
```bash
#!/bin/bash
echo "Running fast tests..."
dotnet test --filter "Category!=Performance&Category!=LongRunning"
if [ $? -ne 0 ]; then
    echo "Tests failed. Commit aborted."
    exit 1
fi
```

## Test Execution Best Practices

1. **During Development**: Run fast tests only
   ```powershell
   dotnet test --filter "Category!=Performance"
   ```

2. **Before Committing**: Run fast tests
   ```powershell
   dotnet test --filter "Category!=Performance"
   ```

3. **Before Pull Request**: Run all tests
   ```powershell
   dotnet test
   ```

4. **Performance Tuning**: Run performance tests
   ```powershell
   dotnet test --filter "Category=Performance"
   ```

5. **Debugging Specific Feature**: Run specific test class
   ```powershell
   dotnet test --filter "FullyQualifiedName~RabinKarpTests"
   ```

## Coverage Analysis

```powershell
# Run with coverage (fast tests only)
dotnet test --filter "Category!=Performance" /p:CollectCoverage=true

# Run with coverage (all tests)
dotnet test /p:CollectCoverage=true
```

## Parallel Execution

xUnit runs tests in parallel by default. To control parallelization:

```powershell
# Disable parallel execution (for debugging)
dotnet test -- xUnit.ParallelizeTestCollections=false

# Limit parallel threads
dotnet test -- xUnit.MaxParallelThreads=4
```

## Troubleshooting

### Tests Running Forever
- **Cause**: Performance test with large data (e.g., 1MB suffix array)
- **Solution**: Exclude performance tests or increase timeout
  ```powershell
  dotnet test --filter "Category!=Performance"
  ```

### Out of Memory
- **Cause**: Too many parallel performance tests
- **Solution**: Run sequentially or reduce parallelization
  ```powershell
  dotnet test -- xUnit.ParallelizeTestCollections=false
  ```

### Flaky File I/O Tests
- **Cause**: File locking, antivirus, timing issues
- **Solution**: Tests use `FileShare.ReadWrite` and retry logic
- **Workaround**: Run tests again or disable antivirus temporarily

## Test Output Verbosity

```powershell
# Minimal (default)
dotnet test --verbosity minimal

# Normal (shows test names)
dotnet test --verbosity normal

# Detailed (shows all output)
dotnet test --verbosity detailed

# Diagnostic (debugging)
dotnet test --verbosity diagnostic
```

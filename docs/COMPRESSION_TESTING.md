# BPS Compression Strategies Testing Plan

A systematic approach to testing and optimizing BPS patch compression effectiveness.

**Created**: January 7, 2026

---

## Overview

BPS patches encode differences between source and target files using four actions:
1. **SourceRead**: Copy byte from source at current position
2. **TargetRead**: Write literal byte (from patch data)
3. **SourceCopy**: Copy range from source at arbitrary offset
4. **TargetCopy**: Copy range from already-written target data

The compression effectiveness depends on:
- Pattern matching algorithm efficiency
- Minimum match length threshold
- Offset encoding efficiency
- Action selection heuristics

---

## Current Algorithms

### 1. Linear Search (Default for <64KB)

**How it works:**
- Scans source sequentially for pattern matches
- Simple and effective for small files
- O(n × m) complexity

**Best for:**
- Small files (<64KB)
- Files with localized changes
- Simple patches

**Testing approach:**
```csharp
// Generate test cases
byte[] source = GenerateSequentialData(size: 32 * 1024);  // 32KB
byte[] target = ModifyRandomPositions(source, changeCount: 100);

// Benchmark
var patch = BpsEncoder.Encode(source, target, MatchingAlgorithm.Linear);
Console.WriteLine($"Patch size: {patch.Length} bytes");
Console.WriteLine($"Compression ratio: {(double)patch.Length / target.Length:P}");
```

### 2. Rabin-Karp Rolling Hash (64KB - 1MB)

**How it works:**
- Builds hash table of source patterns
- Rolling hash enables O(1) position updates
- O(n + m) average, O(n × m) worst case

**Best for:**
- Medium files (64KB - 1MB)
- Files with repeated patterns
- General-purpose patching

**Testing approach:**
```csharp
// Test hash collision rate
var strategy = new RabinKarpMatchingStrategy();
strategy.Prepare(sourceData);

int collisions = 0;
for (int i = 0; i < targetData.Length - 4; i++)
{
    var (length, start, _) = strategy.FindBestMatch(sourceData, targetData.AsSpan(i), 4);
    if (start >= 0 && !sourceData.AsSpan(start, length).SequenceEqual(targetData.AsSpan(i, length)))
        collisions++;
}
Console.WriteLine($"Hash collisions: {collisions}");
```

### 3. Suffix Array (>1MB)

**How it works:**
- Builds sorted array of all suffixes
- Binary search for patterns
- O(n log n) construction, O(m log n) search

**Best for:**
- Large files (>1MB)
- Files with many repeated patterns
- ROM images

**Testing approach:**
```csharp
// Test suffix array accuracy
var strategy = new SuffixArrayMatchingStrategy();
strategy.Prepare(sourceData);  // Build suffix array

// Verify all matches are valid
for (int i = 0; i < 1000; i++)
{
    int start = Random.Shared.Next(targetData.Length - 10);
    var pattern = targetData.AsSpan(start, 10);
    var (length, matchStart, _) = strategy.FindBestMatch(sourceData, pattern, 4);
    
    if (length > 0)
    {
        Assert.True(sourceData.AsSpan(matchStart, length).SequenceEqual(pattern[..length]));
    }
}
```

---

## Test Matrix

### File Size Tests

| Size | Algorithm | Expected Time | Expected Ratio |
|------|-----------|---------------|----------------|
| 1 KB | Linear | <1ms | Variable |
| 32 KB | Linear | <10ms | ~2-5% for similar |
| 100 KB | Rabin-Karp | <50ms | ~5-10% |
| 1 MB | Rabin-Karp | <500ms | ~5-15% |
| 10 MB | Suffix Array | <5s | ~5-20% |
| 100 MB | Suffix Array | <60s | ~10-30% |

### Change Pattern Tests

| Pattern | Description | Best Algorithm |
|---------|-------------|----------------|
| Single byte | One byte changed | Any (all fast) |
| Scattered | Random positions | Rabin-Karp |
| Sequential | Contiguous block | Linear |
| Repeated | Pattern duplicated | Suffix Array |
| Inserted | New data added | TargetRead heavy |
| Deleted | Data removed | SourceCopy heavy |

### Real-World Test Cases

1. **ROM Hacking** (Primary use case)
   - Source: Original ROM (2-32 MB)
   - Target: Modified ROM
   - Expected: <5% patch size

2. **Text Files**
   - Source: Original document
   - Target: Edited version
   - Expected: Very small patches

3. **Binary Data**
   - Source: Compiled program
   - Target: New build
   - Expected: Variable, depends on compiler

---

## Benchmark Suite

### Setup

```csharp
[MemoryDiagnoser]
public class CompressionStrategyBenchmarks
{
    private byte[] _source = null!;
    private byte[] _target = null!;

    [Params(1024, 32768, 1048576, 10485760)]
    public int FileSize { get; set; }

    [Params(0.01, 0.05, 0.10, 0.25)]
    public double ChangeRatio { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _source = new byte[FileSize];
        Random.Shared.NextBytes(_source);
        
        _target = (byte[])_source.Clone();
        int changes = (int)(FileSize * ChangeRatio);
        for (int i = 0; i < changes; i++)
        {
            _target[Random.Shared.Next(FileSize)] = (byte)Random.Shared.Next(256);
        }
    }

    [Benchmark(Baseline = true)]
    public byte[] Linear() => BpsEncoder.Encode(_source, _target, MatchingAlgorithm.Linear);

    [Benchmark]
    public byte[] RabinKarp() => BpsEncoder.Encode(_source, _target, MatchingAlgorithm.RabinKarp);

    [Benchmark]
    public byte[] SuffixArray() => BpsEncoder.Encode(_source, _target, MatchingAlgorithm.SuffixArray);

    [Benchmark]
    public byte[] Auto() => BpsEncoder.Encode(_source, _target, MatchingAlgorithm.Auto);
}
```

### Expected Results

| FileSize | ChangeRatio | Linear | Rabin-Karp | Suffix Array |
|----------|-------------|--------|------------|--------------|
| 1 KB | 1% | ✅ Fast | ⚠️ Overhead | ❌ Overkill |
| 1 KB | 10% | ✅ Fast | ⚠️ Overhead | ❌ Overkill |
| 32 KB | 1% | ✅ Good | ✅ Good | ⚠️ Slow setup |
| 32 KB | 10% | ✅ Good | ✅ Better | ⚠️ Slow setup |
| 1 MB | 1% | ⚠️ Slow | ✅ Fast | ✅ Best |
| 1 MB | 10% | ❌ Very slow | ✅ Good | ✅ Best |
| 10 MB | 1% | ❌ Too slow | ⚠️ OK | ✅ Required |
| 10 MB | 10% | ❌ Too slow | ⚠️ Slow | ✅ Required |

---

## Optimization Opportunities

### 1. Lazy Matching (Issue #4)

Instead of committing to first good match, check if next position has better match:

```csharp
// Current (greedy)
var match = FindMatch(position);
if (match.Length >= 4) CommitMatch(match);

// Improved (lazy)
var match = FindMatch(position);
if (match.Length >= 4)
{
    var nextMatch = FindMatch(position + 1);
    if (nextMatch.Length > match.Length + 1)
    {
        // Emit one literal, use better match
        EmitLiteral(data[position]);
        CommitMatch(nextMatch);
    }
    else
    {
        CommitMatch(match);
    }
}
```

**Expected improvement:** 5-15% smaller patches

### 2. Multi-Hash Rabin-Karp

Use multiple hash functions to reduce collisions:

```csharp
// Current
ulong hash = ComputeHash(data, start, length);

// Improved
(ulong h1, ulong h2) = ComputeDualHash(data, start, length);
if (hashTable.TryGet((h1, h2), out var positions))
{
    // Much fewer false positives
}
```

**Expected improvement:** 10-20% faster for collision-heavy data

### 3. SIMD Pattern Matching

Use vector instructions for initial pattern search:

```csharp
// Use AVX2 to find potential match starts
Vector256<byte> pattern = Vector256.Create(target[0]);
var matches = Avx2.CompareEqual(source.AsVector256(), pattern);
if (!Avx2.TestZ(matches, matches))
{
    // Process potential matches
}
```

**Expected improvement:** 2-4x faster pattern scanning

### 4. Parallel Chunk Processing

Divide source into chunks, process in parallel:

```csharp
var chunks = Enumerable.Range(0, source.Length / ChunkSize)
    .AsParallel()
    .Select(i => BuildChunkIndex(source, i * ChunkSize, ChunkSize))
    .ToArray();
```

**Expected improvement:** Linear with CPU cores

---

## Testing Commands

```powershell
# Run compression benchmarks
dotnet run -c Release --project src/BpsPatch.Core.Benchmarks/ -- --filter "CompressionStrategyBenchmarks*"

# Test specific algorithm
dotnet run --project src/BpsPatch.Cli/ -- encode source.bin target.bin patch.bps --algorithm RabinKarp

# Compare patch sizes
foreach ($algo in "Linear", "RabinKarp", "SuffixArray") {
    dotnet run --project src/BpsPatch.Cli/ -- encode source.bin target.bin "patch-$algo.bps" --algorithm $algo
    (Get-Item "patch-$algo.bps").Length
}

# Verify round-trip
dotnet run --project src/BpsPatch.Cli/ -- decode source.bin patch.bps output.bin
Compare-Object (Get-FileHash target.bin) (Get-FileHash output.bin)
```

---

## Success Metrics

| Metric | Target | How to Measure |
|--------|--------|----------------|
| Patch size | <10% of target | `patch.Length / target.Length` |
| Encode time | <1s per MB | Benchmark |
| Decode time | <100ms per MB | Benchmark |
| Memory usage | <2x file size | MemoryDiagnoser |
| Hash collisions | <1% | Instrumented test |
| Match accuracy | 100% | Verification test |

---

## Next Steps

1. Run complete benchmark suite
2. Identify worst-performing scenarios
3. Implement lazy matching
4. Profile memory usage
5. Test with real ROM files
6. Document final recommendations

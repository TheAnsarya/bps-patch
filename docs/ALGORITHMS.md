# 🧮 BPS Patch Algorithms Guide

> 📚 **Navigation**: [← Back to README](../README.md) | [Architecture](ARCHITECTURE.md) | [Performance](PERFORMANCE.md) | [API Reference](API_REFERENCE.md)

This document provides detailed explanations of the algorithms used in the BPS Patch library, including time/space complexity analysis and optimization strategies.

## Table of Contents

- [Overview](#overview)
- [Pattern Matching Algorithms](#pattern-matching-algorithms)
  - [Linear Search](#linear-search)
  - [Rabin-Karp Rolling Hash](#rabin-karp-rolling-hash)
  - [Suffix Array](#suffix-array)
- [Compression Strategies](#compression-strategies)
- [Variable-Length Encoding](#variable-length-encoding)
- [SIMD Optimization](#simd-optimization)
- [Algorithm Selection](#algorithm-selection)
- [Future Optimizations](#future-optimizations)

---

## Overview

BPS patch encoding requires finding optimal matches between source and target data. The quality of pattern matching directly affects:

1. **Patch size**: Better matches = smaller patches
2. **Encoding time**: Faster algorithms = quicker patch creation
3. **Memory usage**: Some algorithms require preprocessing

### Patch Action Costs

| Action | Patch Size Cost | Description |
|--------|-----------------|-------------|
| **SourceRead** | 1-10 bytes | Copy from source at same position |
| **TargetRead** | 1-10 + N bytes | Embed N new bytes in patch |
| **SourceCopy** | 2-15 bytes | Copy from elsewhere in source |
| **TargetCopy** | 2-15 bytes | Copy from earlier in target (RLE) |

**Goal**: Minimize total patch size by maximizing SourceRead/SourceCopy/TargetCopy usage and minimizing TargetRead.

---

## Pattern Matching Algorithms

### Linear Search

The simplest approach: scan all positions in source to find the best match.

**Implementation** (`FindBestRunLinear`):

```csharp
for (int start = 0; start <= checkUntil; start++) {
    (int length, bool reachedEnd) = CheckRun(source[start..], target);
    
    if (length > longestRun) {
        longestRun = length;
        longestStart = start;
        
        // Prune: can't find longer match beyond this point
        checkUntil = Math.Min(checkUntil, source.Length - longestRun);
    }
}
```

**Complexity**:
- **Time**: O(n × m) average, O(n²) worst case
  - n = source length, m = target pattern length
  - With pruning optimization, often better in practice
- **Space**: O(1) - no preprocessing required

**Advantages**:
- ✅ No preprocessing overhead
- ✅ Best for small files (< 64KB)
- ✅ Simple implementation, easy to debug
- ✅ Cache-friendly sequential access

**Disadvantages**:
- ❌ Slow for large files
- ❌ Quadratic worst case (e.g., all bytes identical)

**Optimization: Early Termination**

The algorithm prunes the search space when a long match is found:

```csharp
checkUntil = Math.Min(checkUntil, source.Length - longestRun);
```

This ensures we don't search positions that can't possibly yield a longer match.

---

### Rabin-Karp Rolling Hash

Uses a polynomial rolling hash to quickly identify potential matches, then verifies with byte comparison.

**How It Works**:

1. Compute hash of target pattern
2. Compute hash of first source window
3. Roll through source, updating hash in O(1) per step
4. When hashes match, verify bytes to avoid false positives

**Implementation** (`RabinKarp.FindBestRun`):

```csharp
// Rolling hash update
sourceHash = (sourceHash + PRIME - (source[i-1] * basePower) % PRIME) % PRIME;
sourceHash = (sourceHash * BASE + source[i + patternLength - 1]) % PRIME;

if (sourceHash == patternHash) {
    // Verify match
    if (source.Slice(i, patternLength).SequenceEqual(pattern))
        return (true, i);
}
```

**Hash Function**:

```
hash(s[0..n]) = (s[0] × BASE^(n-1) + s[1] × BASE^(n-2) + ... + s[n-1]) mod PRIME
```

Where:
- `BASE = 257` (next prime after 256)
- `PRIME = 2^31 - 1` (Mersenne prime for efficient modulo)

**Complexity**:
- **Time**: O(n + m) average, O(nm) worst case (many hash collisions)
- **Space**: O(1)

**Advantages**:
- ✅ O(n) average case - much faster than linear for large files
- ✅ No preprocessing required
- ✅ Constant space usage

**Disadvantages**:
- ❌ Hash collisions require verification
- ❌ Not optimal for finding *longest* match (designed for exact pattern search)
- ❌ Worst case same as linear search

**Collision Probability**:

With PRIME = 2^31 - 1 and random data:
- Single comparison: ~1 in 2 billion false positive
- For 1MB file with 4-byte pattern: expected ~500 false positives

Each false positive requires O(m) verification, but this is rare in practice.

---

### Suffix Array

A suffix array is a sorted array of all suffixes of a string, enabling O(log n) binary search for pattern matching.

**Data Structure**:

For string "banana":
```
Suffixes:           Suffix Array (sorted indices):
0: banana           SA[0] = 5 (a)
1: anana            SA[1] = 3 (ana)
2: nana             SA[2] = 1 (anana)
3: ana              SA[3] = 0 (banana)
4: na               SA[4] = 4 (na)
5: a                SA[5] = 2 (nana)
```

**LCP Array** (Longest Common Prefix):
- `LCP[i]` = length of longest common prefix between `SA[i]` and `SA[i-1]`
- Enables efficient range queries for all matches

**Implementation** (`SuffixArray.FindLongestMatch`):

```csharp
// Binary search for range of suffixes starting with pattern's first byte
int startIdx = BinarySearchFirstByteRange(pattern[0], out int endIdx);

// Scan range for longest match
for (int i = startIdx; i <= endIdx; i++) {
    int suffixPos = _suffixArray[i];
    int matchLen = CountMatchingBytes(_data.AsSpan(suffixPos), pattern);
    
    if (matchLen > bestLength) {
        bestLength = matchLen;
        bestStart = suffixPos;
    }
}
```

**Complexity**:
- **Construction**: O(n² log n) naive, O(n) with SA-IS algorithm
- **Query**: O(log n) binary search + O(m) match extension
- **Space**: O(n) for suffix array + O(n) for LCP array

**Advantages**:
- ✅ O(log n) query time after construction
- ✅ Excellent for multiple queries on same source
- ✅ Finds true longest match efficiently

**Disadvantages**:
- ❌ High construction cost (O(n²) current implementation)
- ❌ 2× memory overhead for arrays
- ❌ Overkill for small files or single query

**Future Optimization: SA-IS Algorithm**

The current implementation uses naive O(n² log n) sorting. The SA-IS (Induced Sorting) algorithm achieves O(n) construction:

1. Classify suffixes as S-type or L-type
2. Find LMS (leftmost S-type) substrings
3. Induced sort from LMS positions
4. Recursively solve reduced problem

Reference: [Linear Suffix Array Construction](https://www.researchgate.net/publication/224176324)

---

## Compression Strategies

### Greedy Matching (Current)

At each position, find the longest match and emit it immediately.

```
Position 0: Find best match → Emit
Position N: Find best match → Emit
...
```

**Pros**: Simple, fast
**Cons**: May miss global optima

### Lazy Matching (Proposed)

Before committing to a match, check if the next position has a better match.

```csharp
var match1 = FindBestRun(target[pos..]);
var match2 = FindBestRun(target[pos+1..]);

if (match2.Length > match1.Length + 1) {
    // Skip current position, take better match
    EmitTargetRead(target[pos]);
    pos++;
} else {
    EmitMatch(match1);
    pos += match1.Length;
}
```

**Pros**: Better compression (5-15% smaller patches)
**Cons**: 2× pattern matching calls

### Optimal Parsing (Proposed)

Use dynamic programming to find globally optimal sequence of actions.

```csharp
// dp[i] = minimum cost to encode target[0..i]
dp[0] = 0;
for (int i = 1; i <= target.Length; i++) {
    // Try all possible actions ending at position i
    dp[i] = min(
        dp[i-1] + TargetReadCost(1),           // Single new byte
        dp[i-len] + SourceReadCost(len),       // SourceRead of len bytes
        dp[i-len] + SourceCopyCost(len, off),  // SourceCopy
        dp[i-len] + TargetCopyCost(len, off)   // TargetCopy
    );
}
```

**Pros**: Optimal compression
**Cons**: O(n²) or worse time complexity

---

## Variable-Length Encoding

BPS uses a custom VLQ (Variable-Length Quantity) encoding:

### Encoding Process

```
Input: 300 (binary: 100101100)

Step 1: Extract 7 bits → 0101100 (44), remaining: 10 (2)
Step 2: remaining > 0, decrement and continue
        remaining - 1 = 1, extract 7 bits → 0000001 (1)
Step 3: remaining = 0, set MSB for termination

Output: [44, 129] = [0x2C, 0x81]
```

### Decoding Process

```
Input: [0x2C, 0x81]

Step 1: Read 0x2C, MSB clear → data += 44 × 1 = 44, shift = 128
Step 2: shift += 128 = 128, data += 128 = 172
Step 3: Read 0x81, MSB set → data += (0x81 & 0x7F) × 128 = 172 + 128 = 300
```

### Size Table

| Value Range | Bytes Required |
|-------------|----------------|
| 0 - 127 | 1 |
| 128 - 16,511 | 2 |
| 16,512 - 2,113,663 | 3 |
| 2,113,664 - 270,549,119 | 4 |
| ... | ... |

---

## SIMD Optimization

The `CheckRun` function uses SIMD for bulk byte comparison:

```csharp
if (Vector.IsHardwareAccelerated && maxLength >= Vector<byte>.Count) {
    int vectorLength = Vector<byte>.Count;  // 16 (SSE) or 32 (AVX)
    
    while (length <= maxVectorIndex) {
        var sourceVec = new Vector<byte>(source.Slice(length, vectorLength));
        var targetVec = new Vector<byte>(target.Slice(length, vectorLength));
        
        if (!Vector.EqualsAll(sourceVec, targetVec))
            break;  // Mismatch in this chunk
        
        length += vectorLength;
    }
}
```

**Performance**:
- SSE (128-bit): 16 bytes compared per operation
- AVX (256-bit): 32 bytes compared per operation
- **4-8× speedup** for long matching runs

**Fallback**:
- Scalar comparison for remaining bytes
- Works on all platforms (SIMD auto-disabled if unavailable)

---

## Algorithm Selection

### Current (Manual)

Developers explicitly call the desired algorithm:

```csharp
FindBestRunLinear(source, target, minLen);
FindBestRunRabinKarp(source, target, minLen);
FindBestRunSuffixArray(source, target, minLen);
```

### Proposed (Automatic)

Auto-select based on file characteristics:

```csharp
public static (int, int, bool) FindBestRunAuto(
    ReadOnlySpan<byte> source,
    ReadOnlySpan<byte> target,
    int minLen,
    SuffixArray? cachedSuffixArray = null)
{
    // Heuristics based on benchmarking
    if (source.Length < 65_536) {
        // Small files: linear search is fastest
        return FindBestRunLinear(source, target, minLen);
    }
    else if (source.Length < 1_048_576) {
        // Medium files: Rabin-Karp good balance
        return FindBestRunRabinKarp(source, target, minLen);
    }
    else {
        // Large files: Suffix array (reuse if cached)
        if (cachedSuffixArray != null)
            return cachedSuffixArray.FindLongestMatch(target, minLen);
        
        // One-time construction cost acceptable for large files
        var sa = new SuffixArray(source);
        return sa.FindLongestMatch(target, minLen);
    }
}
```

### Benchmark-Driven Thresholds

| File Size | Recommended Algorithm | Reason |
|-----------|----------------------|--------|
| < 64 KB | Linear | Construction overhead not worth it |
| 64 KB - 1 MB | Rabin-Karp | Good average case, no preprocessing |
| > 1 MB | Suffix Array | Amortized O(log n) queries |

---

## Future Optimizations

### 1. SA-IS Suffix Array Construction

Replace O(n² log n) naive sort with O(n) SA-IS:
- 10-100× faster construction for large files
- Same query performance

### 2. Parallel Pattern Matching

Use PLINQ for independent chunk searches:

```csharp
var results = Enumerable.Range(0, chunks)
    .AsParallel()
    .Select(chunk => FindMatchInChunk(chunk))
    .ToList();
```

### 3. Memory-Mapped Files

For files > available RAM:

```csharp
using var mmf = MemoryMappedFile.CreateFromFile(path);
using var accessor = mmf.CreateViewAccessor();
// Process in chunks without loading entire file
```

### 4. Compression-Aware Matching

Prioritize matches that compress well:
- Longer matches preferred over shorter
- Consider offset encoding cost
- Avoid tiny matches (< 4 bytes)

### 5. Multi-Level Hashing

Hierarchical hash tables for O(1) expected lookup:
1. First-level: hash on first 4 bytes
2. Second-level: hash on first 8 bytes
3. Linear verification for candidates

---

## See Also

- [ARCHITECTURE.md](ARCHITECTURE.md) - System architecture
- [PERFORMANCE.md](PERFORMANCE.md) - Performance tuning
- [BPS_FORMAT_SPECIFICATION.md](../BPS_FORMAT_SPECIFICATION.md) - Format details

## References

1. [Suffix Arrays - Wikipedia](https://en.wikipedia.org/wiki/Suffix_array)
2. [Rabin-Karp Algorithm - Wikipedia](https://en.wikipedia.org/wiki/Rabin%E2%80%93Karp_algorithm)
3. [SA-IS Paper](https://www.researchgate.net/publication/224176324)
4. [LZ77 Compression](https://en.wikipedia.org/wiki/LZ77_and_LZ78)
5. [.NET SIMD](https://learn.microsoft.com/en-us/dotnet/standard/simd)

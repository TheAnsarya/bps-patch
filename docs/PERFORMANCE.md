# ⚡ BPS Patch Performance Guide

> 📚 **Navigation**: [← Back to README](../README.md) | [Algorithms](ALGORITHMS.md) | [Architecture](ARCHITECTURE.md) | [Benchmarks Setup](../BENCHMARKS_SETUP.md)

This document covers performance characteristics, optimization techniques, benchmarking methodology, and tuning recommendations for the BPS Patch library.

## Table of Contents

- [Performance Overview](#performance-overview)
- [Benchmarking](#benchmarking)
- [Memory Optimization](#memory-optimization)
- [I/O Optimization](#io-optimization)
- [Algorithm Performance](#algorithm-performance)
- [Profiling Guide](#profiling-guide)
- [Tuning Recommendations](#tuning-recommendations)

---

## Performance Overview

### Key Metrics

| Operation | Typical Speed | Memory | GC Pressure |
|-----------|--------------|--------|-------------|
| **Encoding (Linear)** | 1-5 MB/s | 2× file size | Low (ArrayPool) |
| **Encoding (Rabin-Karp)** | 5-15 MB/s | 2× file size | Low |
| **Encoding (Suffix Array)** | 10-50 MB/s | 4× file size | Medium |
| **Decoding** | 50-200 MB/s | 2× target size | Low |
| **CRC32** | 500+ MB/s | < 1 KB | None |

### Performance Targets

- **Small files (< 64 KB)**: < 100ms encoding
- **Medium files (64 KB - 1 MB)**: < 1s encoding
- **Large files (> 1 MB)**: < 10s encoding per MB
- **Decoding**: Always < 100ms per MB

---

## Benchmarking

### Running Benchmarks

```powershell
cd bps-patch.Benchmarks
dotnet run -c Release

# Specific benchmark class
dotnet run -c Release -- --filter *EncoderBenchmarks*

# Quick benchmark (fewer iterations)
dotnet run -c Release -- --job short
```

### Benchmark Categories

#### 1. Encoder Benchmarks (`EncoderBenchmarks.cs`)

```csharp
[Benchmark] public void EncodeIdentical_1KB() { ... }
[Benchmark] public void EncodeSmallChange_1KB() { ... }
[Benchmark] public void EncodeLargeChange_1KB() { ... }
[Benchmark] public void EncodeIdentical_1MB() { ... }
```

#### 2. Decoder Benchmarks (`DecoderBenchmarks.cs`)

```csharp
[Benchmark] public void Decode_1KB() { ... }
[Benchmark] public void Decode_1MB() { ... }
```

#### 3. Algorithm Benchmarks (`SimdBenchmarks.cs`)

```csharp
[Benchmark] public void CheckRun_SIMD_1KB() { ... }
[Benchmark] public void CheckRun_Scalar_1KB() { ... }
```

#### 4. CRC32 Benchmarks (`CRC32Benchmarks.cs`)

```csharp
[Benchmark] public void CRC32_File_1MB() { ... }
[Benchmark] public void CRC32_Span_1MB() { ... }
```

### Sample Results

```
| Method                    | Mean       | Allocated |
|-------------------------- |-----------:|----------:|
| EncodeIdentical_1KB       |   0.234 ms |     4 KB  |
| EncodeSmallChange_1KB     |   1.456 ms |     8 KB  |
| EncodeIdentical_1MB       | 234.567 ms |     4 MB  |
| DecodeSmallPatch_1MB      |   5.678 ms |     2 MB  |
| CheckRun_SIMD_1MB         |   0.123 ms |     0 B   |
| CheckRun_Scalar_1MB       |   0.987 ms |     0 B   |
```

### Writing Custom Benchmarks

```csharp
[SimpleJob]
[MemoryDiagnoser]
[MarkdownExporter]
public class MyBenchmarks
{
    private byte[] _source = null!;
    private byte[] _target = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        _source = new byte[1024 * 1024];
        _target = new byte[1024 * 1024];
        // Initialize with test data
    }
    
    [Benchmark(Baseline = true)]
    public (int, bool) CheckRun_SIMD() 
        => Encoder.CheckRun(_source, _target);
    
    [Benchmark]
    public (int, bool) CheckRun_Scalar() 
        => Encoder.CheckRunScalar(_source, _target);
}
```

---

## Memory Optimization

### ArrayPool Usage

The library uses `ArrayPool<byte>.Shared` to reduce GC pressure:

```csharp
byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
try {
    // Use buffer (may be larger than requested)
} finally {
    ArrayPool<byte>.Shared.Return(buffer, clearArray: false);
}
```

**Best Practices**:
- Always return buffers in `finally` block
- Use `clearArray: false` for performance (unless security-sensitive)
- Don't assume exact buffer size (may be larger)

### Stack Allocation

For small, short-lived buffers:

```csharp
Span<byte> header = stackalloc byte[4];
Span<byte> hash = stackalloc byte[12];
```

**Guidelines**:
- Maximum: 1 KB per method
- Must not escape method scope
- Use for fixed-size temporary data

### Memory Pressure Monitoring

```csharp
// Before critical section
long before = GC.GetTotalMemory(false);

// After critical section
long after = GC.GetTotalMemory(false);
long allocated = after - before;
```

### GC Collection Monitoring

```csharp
int gen0Before = GC.CollectionCount(0);
int gen1Before = GC.CollectionCount(1);
int gen2Before = GC.CollectionCount(2);

// Perform operation

int gen0After = GC.CollectionCount(0);
// Gen0 collections indicate memory pressure
```

---

## I/O Optimization

### Buffered Streams

All file operations use 80 KB buffers:

```csharp
private const int BUFFER_SIZE = 81920;

using var stream = new BufferedStream(file.OpenRead(), BUFFER_SIZE);
```

**Why 80 KB?**
- Large enough to amortize system call overhead
- Small enough to fit in L2 cache
- Microsoft's recommended size for BufferedStream

### FileStream Options

```csharp
using var fs = new FileStream(
    path,
    FileMode.Open,
    FileAccess.Read,
    FileShare.ReadWrite,    // Allow concurrent access
    bufferSize: 81920,
    FileOptions.SequentialScan  // Hint for read-ahead
);
```

### ReadExactly vs Read

```csharp
// Old way (may return partial data)
int bytesRead = stream.Read(buffer, 0, length);

// New way (guaranteed full read or exception)
stream.ReadExactly(buffer.AsSpan(0, length));
```

### Avoiding File Locking

```csharp
// FileShare.ReadWrite allows other processes to access
using var fs = new FileStream(path, FileMode.Open, 
    FileAccess.Read, FileShare.ReadWrite);
```

---

## Algorithm Performance

### Linear Search Performance

| Source Size | Target Size | Time (avg) |
|-------------|-------------|------------|
| 1 KB | 1 KB | < 1 ms |
| 10 KB | 10 KB | 5-10 ms |
| 100 KB | 100 KB | 100-500 ms |
| 1 MB | 1 MB | 5-20 seconds |

**Note**: Quadratic growth makes linear impractical for large files.

### Rabin-Karp Performance

| Source Size | Target Size | Time (avg) |
|-------------|-------------|------------|
| 1 KB | 1 KB | < 1 ms |
| 10 KB | 10 KB | 2-5 ms |
| 100 KB | 100 KB | 20-50 ms |
| 1 MB | 1 MB | 200-500 ms |

**Note**: Near-linear growth, but higher constant factor than suffix array for queries.

### Suffix Array Performance

| Source Size | Construction | Query (avg) |
|-------------|--------------|-------------|
| 1 KB | 1 ms | < 0.1 ms |
| 10 KB | 50 ms | < 0.1 ms |
| 100 KB | 1 second | < 0.1 ms |
| 1 MB | 30+ seconds | < 0.1 ms |

**Note**: High construction cost (O(n²) current implementation), but O(log n) queries.

### SIMD Speedup

| Run Length | SIMD Time | Scalar Time | Speedup |
|------------|-----------|-------------|---------|
| 64 bytes | 0.01 µs | 0.05 µs | 5× |
| 1 KB | 0.1 µs | 0.7 µs | 7× |
| 1 MB | 100 µs | 800 µs | 8× |

---

## Profiling Guide

### Visual Studio Profiler

1. Debug → Performance Profiler
2. Select "CPU Usage" and ".NET Object Allocation"
3. Run with release build for accurate results

### dotnet-trace

```powershell
# Install
dotnet tool install -g dotnet-trace

# Collect trace
dotnet-trace collect -- dotnet run -c Release

# Analyze in PerfView or Visual Studio
```

### dotnet-counters

```powershell
# Install
dotnet tool install -g dotnet-counters

# Monitor real-time
dotnet-counters monitor --process-id <PID> --counters System.Runtime
```

### BenchmarkDotNet Diagnostics

```csharp
[MemoryDiagnoser]           // Memory allocations
[ThreadingDiagnoser]        // Thread pool usage
[HardwareCounters(...)]     // CPU cache misses, etc.
[DisassemblyDiagnoser]      // JIT assembly output
```

---

## Tuning Recommendations

### For Small Files (< 64 KB)

- Use default linear search
- Stack allocation for all buffers
- Skip suffix array construction

```csharp
// Optimal settings for small files
var result = Encoder.FindBestRunLinear(source, target, minLength);
```

### For Medium Files (64 KB - 1 MB)

- Use Rabin-Karp algorithm
- ArrayPool for file buffers
- Consider parallel processing

```csharp
// Optimal settings for medium files
var result = Encoder.FindBestRunRabinKarp(source, target, minLength);
```

### For Large Files (> 1 MB)

- Build suffix array once, reuse for all queries
- Use memory-mapped files if > 256 MB
- Consider streaming approach

```csharp
// Build suffix array once
var suffixArray = new SuffixArray(sourceData);

// Reuse for each position in target
for (int pos = 0; pos < target.Length; pos++) {
    var result = suffixArray.FindLongestMatch(target[pos..], minLength);
    // ...
}
```

### For Memory-Constrained Environments

- Reduce buffer sizes
- Process in smaller chunks
- Use streaming decoder

```csharp
// Reduced memory footprint
private const int BUFFER_SIZE = 16384;  // 16 KB instead of 80 KB
```

### For Maximum Throughput

- Use all CPU cores with parallel encoding
- Maximize buffer sizes (up to L3 cache)
- Disable validation during batch processing

```csharp
// Parallel chunk processing
Parallel.For(0, chunks, chunk => {
    ProcessChunk(chunk, sourceSlice, targetSlice);
});
```

---

## Performance Checklist

### Encoding Performance

- [ ] Using appropriate algorithm for file size
- [ ] ArrayPool for file buffers
- [ ] BufferedStream for file I/O
- [ ] SIMD enabled for byte comparison
- [ ] Minimum match length set appropriately (4 bytes default)

### Decoding Performance

- [ ] BufferedStream for patch reading
- [ ] ArrayPool for target buffer
- [ ] CRC32 validation at end (not during)
- [ ] Overlap detection optimized

### Memory Usage

- [ ] No unnecessary allocations in hot paths
- [ ] Buffers returned to ArrayPool
- [ ] stackalloc for small temporary data
- [ ] No string concatenation in loops

### I/O Performance

- [ ] 80 KB buffer size
- [ ] FileShare.ReadWrite for concurrent access
- [ ] Sequential file access patterns
- [ ] Flush only at end of operations

---

## See Also

- [ARCHITECTURE.md](ARCHITECTURE.md) - System design
- [ALGORITHMS.md](ALGORITHMS.md) - Algorithm details
- [BenchmarkDotNet Docs](https://benchmarkdotnet.org/articles/overview.html)
- [.NET Performance](https://learn.microsoft.com/en-us/dotnet/standard/memory-and-spans/)

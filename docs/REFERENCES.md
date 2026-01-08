# 📖 References & Research

> 📚 **Navigation**: [← Back to README](../README.md) | [Algorithms](ALGORITHMS.md) | [Performance](PERFORMANCE.md) | [BPS Format](../BPS_FORMAT_SPECIFICATION.md)

A comprehensive collection of references, research papers, and resources used in developing the BPS Patch library.

---

## Table of Contents

- [BPS Format & History](#bps-format--history)
- [Algorithm References](#algorithm-references)
- [Performance & Optimization](#performance--optimization)
- [.NET Documentation](#net-documentation)
- [Related Projects](#related-projects)
- [Academic Papers](#academic-papers)
- [Books & Articles](#books--articles)

---

## BPS Format & History

### Original Specification

| Resource | Author | Description |
|----------|--------|-------------|
| [beat patcher](https://github.com/blakesmith/beat) | byuu/Near | Reference implementation of BPS format |
| [BPS Format Spec](https://github.com/blakesmith/beat/blob/master/doc/bps.txt) | byuu/Near | Official format documentation |
| [byuu.org](https://byuu.org/) | Near | Original author's website (archived) |

### Historical Context

The BPS format was created by **byuu** (later known as **Near**), a legendary emulator developer who created:
- **bsnes** - Cycle-accurate SNES emulator
- **higan** - Multi-system emulator
- **beat** - BPS patcher tool

BPS was introduced around 2012 to address limitations of earlier patch formats:

| Format | Year | Author | Limitations |
|--------|------|--------|-------------|
| **IPS** | 1993 | Unknown | 16MB limit, no checksums, no metadata |
| **UPS** | 2007 | byuu | XOR-based, less efficient compression |
| **BPS** | 2012 | byuu | Current standard - addresses all above |

### ROM Hacking Community Resources

- [RomHacking.net](https://www.romhacking.net/) - Central hub for ROM hacking
- [RomHacking Forum](https://www.romhacking.net/forum/) - Community discussions
- [Patching Utilities](https://www.romhacking.net/utilities/) - Collection of patching tools
- [Floating IPS](https://www.romhacking.net/utilities/1040/) - Popular multi-format patcher

---

## Algorithm References

### Suffix Array Algorithms

#### SA-IS (Implemented in BpsPatch)

```
Nong, G., Zhang, S., & Chan, W. H. (2009).
"Two Efficient Algorithms for Linear Time Suffix Array Construction"
IEEE Transactions on Computers, 60(10), 1471-1484.
DOI: 10.1109/TC.2010.188
```

**Key Contributions**:
- O(n) time complexity for suffix array construction
- Induced sorting from LMS-substrings
- Low memory overhead compared to alternatives

#### Alternative Algorithms

| Algorithm | Complexity | Reference |
|-----------|------------|-----------|
| **SA-IS** | O(n) | Nong, Zhang, Chan (2009) |
| **DC3/Skew** | O(n) | Kärkkäinen & Sanders (2003) |
| **Divsufsort** | O(n) | Mori (2006) |
| **Naive sort** | O(n² log n) | Standard library sort |

```
Kärkkäinen, J., & Sanders, P. (2003).
"Simple Linear Work Suffix Array Construction"
Automata, Languages and Programming (ICALP 2003).
```

```
Manber, U., & Myers, G. (1993).
"Suffix Arrays: A New Method for On-Line String Searches"
SIAM Journal on Computing, 22(5), 935-948.
DOI: 10.1137/0222058
```

### String Matching Algorithms

#### Rabin-Karp (Implemented in BpsPatch)

```
Karp, R. M., & Rabin, M. O. (1987).
"Efficient Randomized Pattern-Matching Algorithms"
IBM Journal of Research and Development, 31(2), 249-260.
DOI: 10.1147/rd.312.0249
```

**Key Insights**:
- Rolling hash enables O(1) hash updates
- Expected O(n + m) time complexity
- Fingerprinting technique for string comparison

#### Other String Algorithms

| Algorithm | Time | Space | Reference |
|-----------|------|-------|-----------|
| **Knuth-Morris-Pratt** | O(n + m) | O(m) | Knuth, Morris, Pratt (1977) |
| **Boyer-Moore** | O(n/m) best | O(σ + m) | Boyer, Moore (1977) |
| **Aho-Corasick** | O(n + z) | O(m × σ) | Aho, Corasick (1975) |

### Compression Theory

#### LZ77 Foundation

```
Ziv, J., & Lempel, A. (1977).
"A Universal Algorithm for Sequential Data Compression"
IEEE Transactions on Information Theory, 23(3), 337-343.
DOI: 10.1109/TIT.1977.1055714
```

**Relevance to BPS**:
- SourceCopy = LZ77 backward reference
- TargetCopy = LZ77 forward reference (RLE)
- Dictionary-based compression model

#### Delta Compression

```
Hunt, J. J., Vo, K. P., & Tichy, W. F. (1998).
"Delta Algorithms: An Empirical Analysis"
ACM Transactions on Software Engineering and Methodology, 7(2), 192-214.
```

**Key Findings**:
- Comparison of bdiff, vdelta, and xdelta algorithms
- Trade-offs between speed and compression ratio
- Optimal block size selection

---

## Performance & Optimization

### Memory Hierarchy

```
Drepper, U. (2007).
"What Every Programmer Should Know About Memory"
Red Hat, Inc.
URL: https://people.freebsd.org/~lstewart/articles/cpumemory.pdf
```

**Application to BpsPatch**:
- 80KB buffer size chosen to fit L2 cache
- Sequential access patterns for cache efficiency
- ArrayPool to avoid allocation overhead

### SIMD Processing

```
Langdale, G., & Lemire, D. (2019).
"Parsing Gigabytes of JSON per Second"
The VLDB Journal, 28(6), 941-960.
DOI: 10.1007/s00778-019-00578-5
```

**Techniques Applied**:
- Vector<byte> for parallel byte comparison
- 16-32 byte vector width depending on hardware
- 4-8× speedup over scalar code

### Cache-Oblivious Algorithms

```
Frigo, M., Leiserson, C. E., Prokop, H., & Ramachandran, S. (1999).
"Cache-Oblivious Algorithms"
40th Annual Symposium on Foundations of Computer Science (FOCS '99).
DOI: 10.1109/SFFCS.1999.814600
```

---

## .NET Documentation

### Core APIs Used

| API | Purpose | Documentation |
|-----|---------|---------------|
| `Span<T>` | Zero-copy memory slicing | [Span<T>](https://learn.microsoft.com/en-us/dotnet/api/system.span-1) |
| `ArrayPool<T>` | Buffer pooling | [ArrayPool](https://learn.microsoft.com/en-us/dotnet/api/system.buffers.arraypool-1) |
| `Vector<T>` | SIMD operations | [SIMD](https://learn.microsoft.com/en-us/dotnet/standard/simd) |
| `Crc32` | CRC32 computation | [System.IO.Hashing](https://learn.microsoft.com/en-us/dotnet/api/system.io.hashing.crc32) |
| `BufferedStream` | I/O buffering | [BufferedStream](https://learn.microsoft.com/en-us/dotnet/api/system.io.bufferedstream) |

### Performance Guides

- [Memory and Spans](https://learn.microsoft.com/en-us/dotnet/standard/memory-and-spans/)
- [High-Performance Logging](https://learn.microsoft.com/en-us/dotnet/core/extensions/high-performance-logging)
- [GC Fundamentals](https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/fundamentals)
- [Hardware Intrinsics](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.intrinsics)

### Benchmarking

- [BenchmarkDotNet](https://benchmarkdotnet.org/) - Precise .NET benchmarking framework
- [dotnet-trace](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace) - Performance tracing
- [PerfView](https://github.com/Microsoft/perfview) - Performance analysis tool

---

## Related Projects

### BPS Implementations

| Project | Language | Notes |
|---------|----------|-------|
| [beat](https://github.com/blakesmith/beat) | C++ | Original reference implementation |
| [Floating IPS](https://www.romhacking.net/utilities/1040/) | C++ | Popular multi-format patcher |
| [MultiPatch](https://projects.sappharad.com/multipatch/) | Objective-C | macOS patcher |
| [Rom Patcher JS](https://github.com/nicholasopuni31/rom-patcher-js) | JavaScript | Browser-based patcher |

### Delta Compression Tools

| Tool | Format | Notes |
|------|--------|-------|
| [xdelta](https://github.com/jmacd/xdelta) | VCDIFF | Streaming support, large files |
| [bsdiff](https://www.daemonology.net/bsdiff/) | Custom | Optimized for executables |
| [hdiffpatch](https://github.com/sisong/HDiffPatch) | Custom | High-performance delta |

### .NET Binary Libraries

| Library | Purpose |
|---------|---------|
| [DeltaCompressionDotNet](https://github.com/jitbit/DeltaCompressionDotNet) | General delta compression |
| [VCDiff](https://github.com/SnowflakePowered/vcdiff) | VCDIFF implementation |

---

## Academic Papers

### Compression Theory

1. **Fundamental Limits**
   ```
   Shannon, C. E. (1948).
   "A Mathematical Theory of Communication"
   Bell System Technical Journal, 27(3), 379-423.
   ```

2. **Dictionary Compression**
   ```
   Storer, J. A., & Szymanski, T. G. (1982).
   "Data Compression via Textual Substitution"
   Journal of the ACM, 29(4), 928-951.
   ```

3. **Error Detection**
   ```
   Peterson, W. W., & Brown, D. T. (1961).
   "Cyclic Codes for Error Detection"
   Proceedings of the IRE, 49(1), 228-235.
   ```

### String Algorithms

4. **Pattern Matching Lower Bounds**
   ```
   Cole, R. (1994).
   "Tight Bounds on the Complexity of the Boyer-Moore String Matching Algorithm"
   SIAM Journal on Computing, 23(5), 1075-1091.
   ```

5. **Suffix Trees**
   ```
   Weiner, P. (1973).
   "Linear Pattern Matching Algorithms"
   14th Annual Symposium on Switching and Automata Theory.
   ```

---

## Books & Articles

### Recommended Reading

| Book | Author | Topics |
|------|--------|--------|
| *Introduction to Algorithms* | CLRS | String matching, dynamic programming |
| *The Art of Computer Programming, Vol. 3* | Knuth | Sorting, searching |
| *Data Compression: The Complete Reference* | Salomon | Compression theory |
| *Algorithms on Strings, Trees and Sequences* | Gusfield | String algorithms |

### Online Resources

- [Algorithmica.org](https://en.algorithmica.org/) - Modern algorithm engineering
- [cp-algorithms.com](https://cp-algorithms.com/) - Competitive programming algorithms
- [Stanford CS166](https://web.stanford.edu/class/cs166/) - Data structures course

---

## Citation Format

When citing this implementation:

```bibtex
@software{bps-patch,
  author = {TheAnsarya},
  title = {BPS Patch - Modern .NET Implementation},
  year = {2026},
  url = {https://github.com/TheAnsarya/bps-patch},
  note = {.NET 10 implementation of BPS binary patch format}
}
```

---

## Contributing References

Found a useful reference? Submit a PR or open an issue to add it to this document.

Guidelines:
- Include full citation (authors, title, year, DOI if available)
- Explain relevance to the BPS Patch project
- Prefer peer-reviewed sources when available

---

**Last Updated**: January 8, 2026

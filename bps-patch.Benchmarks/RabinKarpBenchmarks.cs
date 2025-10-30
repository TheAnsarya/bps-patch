// ========================================================================================================
// Rabin-Karp vs Linear Search Performance Benchmarks
// ========================================================================================================
// Performance comparison between Rabin-Karp rolling hash (O(n)) and linear search (O(n²)).
// Tests FindBestRunRabinKarp vs FindBestRunLinear with various data patterns and file sizes.
//
// References:
// - Rabin-Karp Algorithm: https://en.wikipedia.org/wiki/Rabin%E2%80%93Karp_algorithm
// - BenchmarkDotNet: https://benchmarkdotnet.org/
// ========================================================================================================

namespace bps_patch.Benchmarks;

/// <summary>
/// Benchmarks comparing Rabin-Karp rolling hash vs linear search for pattern matching.
/// </summary>
[SimpleJob]
[MemoryDiagnoser]
[MarkdownExporter]
public class RabinKarpBenchmarks {
	private byte[] _source1KB = null!;
	private byte[] _source10KB = null!;
	private byte[] _source100KB = null!;
	private byte[] _source1MB = null!;

	private byte[] _patternStart = null!;    // Pattern at start of source
	private byte[] _patternMiddle = null!;   // Pattern in middle of source
	private byte[] _patternEnd = null!;      // Pattern at end of source
	private byte[] _patternAbsent = null!;   // Pattern not in source (worst case)
	private byte[] _patternRepeat = null!;   // Repeating pattern (best case for RK)

	/// <summary>
	/// Set up test data for Rabin-Karp vs Linear Search benchmarks.
	/// </summary>
	[GlobalSetup]
	public void GlobalSetup() {
		var random = new Random(42); // Fixed seed for reproducibility

		// Create source files with various patterns
		_source1KB = CreateSourceData(1024, random);
		_source10KB = CreateSourceData(10 * 1024, random);
		_source100KB = CreateSourceData(100 * 1024, random);
		_source1MB = CreateSourceData(1024 * 1024, random);

		// Create search patterns (32 bytes each)
		_patternStart = new byte[32];
		_patternMiddle = new byte[32];
		_patternEnd = new byte[32];
		_patternAbsent = new byte[32];
		_patternRepeat = new byte[32];

		// Pattern at start
		Array.Copy(_source1MB, 0, _patternStart, 0, 32);

		// Pattern in middle
		Array.Copy(_source1MB, _source1MB.Length / 2, _patternMiddle, 0, 32);

		// Pattern at end (worst case for linear search)
		Array.Copy(_source1MB, _source1MB.Length - 32, _patternEnd, 0, 32);

		// Pattern not in source (worst case: full scan)
		random.NextBytes(_patternAbsent);
		for (int i = 0; i < 32; i++) {
			_patternAbsent[i] = 0xFF; // Unlikely to match
		}

		// Repeating pattern (best case for Rabin-Karp)
		for (int i = 0; i < 32; i++) {
			_patternRepeat[i] = (byte)(i % 8);
		}
	}

	private byte[] CreateSourceData(int size, Random random) {
		byte[] data = new byte[size];

		// Mix of random and patterns for realistic testing
		for (int i = 0; i < size; i++) {
			if (i % 100 < 80) {
				// 80% random data
				data[i] = (byte)random.Next(256);
			} else {
				// 20% repeating pattern
				data[i] = (byte)(i % 16);
			}
		}

		return data;
	}

	// ========================================================================================================
	// Rabin-Karp Benchmarks
	// ========================================================================================================

	[Benchmark(Description = "RabinKarp: Pattern at start 10KB")]
	public (int Length, int Start, bool ReachedEnd) RabinKarp_PatternStart_10KB() =>
		Encoder.FindBestRunRabinKarp(_source10KB, _patternStart);

	[Benchmark(Description = "RabinKarp: Pattern in middle 10KB")]
	public (int Length, int Start, bool ReachedEnd) RabinKarp_PatternMiddle_10KB() =>
		Encoder.FindBestRunRabinKarp(_source10KB, _patternMiddle);

	[Benchmark(Description = "RabinKarp: Pattern at end 10KB")]
	public (int Length, int Start, bool ReachedEnd) RabinKarp_PatternEnd_10KB() =>
		Encoder.FindBestRunRabinKarp(_source10KB, _patternEnd);

	[Benchmark(Description = "RabinKarp: Pattern absent 10KB")]
	public (int Length, int Start, bool ReachedEnd) RabinKarp_PatternAbsent_10KB() =>
		Encoder.FindBestRunRabinKarp(_source10KB, _patternAbsent);

	[Benchmark(Description = "RabinKarp: Pattern at start 100KB")]
	public (int Length, int Start, bool ReachedEnd) RabinKarp_PatternStart_100KB() =>
		Encoder.FindBestRunRabinKarp(_source100KB, _patternStart);

	[Benchmark(Description = "RabinKarp: Pattern in middle 100KB")]
	public (int Length, int Start, bool ReachedEnd) RabinKarp_PatternMiddle_100KB() =>
		Encoder.FindBestRunRabinKarp(_source100KB, _patternMiddle);

	[Benchmark(Description = "RabinKarp: Pattern at end 100KB")]
	public (int Length, int Start, bool ReachedEnd) RabinKarp_PatternEnd_100KB() =>
		Encoder.FindBestRunRabinKarp(_source100KB, _patternEnd);

	[Benchmark(Description = "RabinKarp: Pattern absent 100KB")]
	public (int Length, int Start, bool ReachedEnd) RabinKarp_PatternAbsent_100KB() =>
		Encoder.FindBestRunRabinKarp(_source100KB, _patternAbsent);

	[Benchmark(Description = "RabinKarp: Pattern at start 1MB")]
	public (int Length, int Start, bool ReachedEnd) RabinKarp_PatternStart_1MB() =>
		Encoder.FindBestRunRabinKarp(_source1MB, _patternStart);

	[Benchmark(Description = "RabinKarp: Pattern in middle 1MB")]
	public (int Length, int Start, bool ReachedEnd) RabinKarp_PatternMiddle_1MB() =>
		Encoder.FindBestRunRabinKarp(_source1MB, _patternMiddle);

	[Benchmark(Description = "RabinKarp: Pattern at end 1MB")]
	public (int Length, int Start, bool ReachedEnd) RabinKarp_PatternEnd_1MB() =>
		Encoder.FindBestRunRabinKarp(_source1MB, _patternEnd);

	[Benchmark(Description = "RabinKarp: Pattern absent 1MB")]
	public (int Length, int Start, bool ReachedEnd) RabinKarp_PatternAbsent_1MB() =>
		Encoder.FindBestRunRabinKarp(_source1MB, _patternAbsent);

	// ========================================================================================================
	// Linear Search Benchmarks
	// ========================================================================================================

	[Benchmark(Description = "Linear: Pattern at start 10KB", Baseline = true)]
	public (int Length, int Start, bool ReachedEnd) Linear_PatternStart_10KB() =>
		Encoder.FindBestRunLinear(_source10KB, _patternStart);

	[Benchmark(Description = "Linear: Pattern in middle 10KB")]
	public (int Length, int Start, bool ReachedEnd) Linear_PatternMiddle_10KB() =>
		Encoder.FindBestRunLinear(_source10KB, _patternMiddle);

	[Benchmark(Description = "Linear: Pattern at end 10KB")]
	public (int Length, int Start, bool ReachedEnd) Linear_PatternEnd_10KB() =>
		Encoder.FindBestRunLinear(_source10KB, _patternEnd);

	[Benchmark(Description = "Linear: Pattern absent 10KB")]
	public (int Length, int Start, bool ReachedEnd) Linear_PatternAbsent_10KB() =>
		Encoder.FindBestRunLinear(_source10KB, _patternAbsent);

	[Benchmark(Description = "Linear: Pattern at start 100KB")]
	public (int Length, int Start, bool ReachedEnd) Linear_PatternStart_100KB() =>
		Encoder.FindBestRunLinear(_source100KB, _patternStart);

	[Benchmark(Description = "Linear: Pattern in middle 100KB")]
	public (int Length, int Start, bool ReachedEnd) Linear_PatternMiddle_100KB() =>
		Encoder.FindBestRunLinear(_source100KB, _patternMiddle);

	[Benchmark(Description = "Linear: Pattern at end 100KB")]
	public (int Length, int Start, bool ReachedEnd) Linear_PatternEnd_100KB() =>
		Encoder.FindBestRunLinear(_source100KB, _patternEnd);

	[Benchmark(Description = "Linear: Pattern absent 100KB")]
	public (int Length, int Start, bool ReachedEnd) Linear_PatternAbsent_100KB() =>
		Encoder.FindBestRunLinear(_source100KB, _patternAbsent);

	[Benchmark(Description = "Linear: Pattern at start 1MB")]
	public (int Length, int Start, bool ReachedEnd) Linear_PatternStart_1MB() =>
		Encoder.FindBestRunLinear(_source1MB, _patternStart);

	[Benchmark(Description = "Linear: Pattern in middle 1MB")]
	public (int Length, int Start, bool ReachedEnd) Linear_PatternMiddle_1MB() =>
		Encoder.FindBestRunLinear(_source1MB, _patternMiddle);

	[Benchmark(Description = "Linear: Pattern at end 1MB")]
	public (int Length, int Start, bool ReachedEnd) Linear_PatternEnd_1MB() =>
		Encoder.FindBestRunLinear(_source1MB, _patternEnd);

	[Benchmark(Description = "Linear: Pattern absent 1MB")]
	public (int Length, int Start, bool ReachedEnd) Linear_PatternAbsent_1MB() =>
		Encoder.FindBestRunLinear(_source1MB, _patternAbsent);
}

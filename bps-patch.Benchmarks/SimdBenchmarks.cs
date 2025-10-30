// ========================================================================================================
// SIMD vs Scalar Performance Benchmarks
// ========================================================================================================
// Performance comparison between SIMD-optimized and scalar byte comparison algorithms.
// Tests CheckRun (SIMD) vs CheckRunScalar (byte-by-byte) with various data patterns.
//
// References:
// - SIMD in .NET: https://learn.microsoft.com/en-us/dotnet/standard/simd
// - Vector<T>: https://learn.microsoft.com/en-us/dotnet/api/system.numerics.vector-1
// - BenchmarkDotNet: https://benchmarkdotnet.org/
// ========================================================================================================

namespace bps_patch.Benchmarks;

/// <summary>
/// Benchmarks comparing SIMD (Vector&lt;byte&gt;) vs scalar byte comparison.
/// </summary>
[SimpleJob]
[MemoryDiagnoser]
[MarkdownExporter]
public class SimdBenchmarks {
	private byte[] _identical1KB = null!;
	private byte[] _identical10KB = null!;
	private byte[] _identical100KB = null!;
	private byte[] _identical1MB = null!;

	private byte[] _mismatchStart1KB = null!;
	private byte[] _mismatchMiddle1KB = null!;
	private byte[] _mismatchEnd1KB = null!;

	private byte[] _mismatchStart10KB = null!;
	private byte[] _mismatchMiddle10KB = null!;
	private byte[] _mismatchEnd10KB = null!;

	private byte[] _pattern = null!;

	/// <summary>
	/// Set up test data for SIMD vs Scalar benchmarks.
	/// </summary>
	[GlobalSetup]
	public void GlobalSetup() {
		var random = new Random(42); // Fixed seed for reproducibility

		// Create base pattern (repetitive data, best case for SIMD)
		_pattern = new byte[1024];
		for (int i = 0; i < _pattern.Length; i++) {
			_pattern[i] = (byte)(i % 256);
		}

		// Identical data sets (best case: full match)
		_identical1KB = CreateRepeatingPattern(1024);
		_identical10KB = CreateRepeatingPattern(10 * 1024);
		_identical100KB = CreateRepeatingPattern(100 * 1024);
		_identical1MB = CreateRepeatingPattern(1024 * 1024);

		// Mismatch at start (worst case for early termination)
		_mismatchStart1KB = CreateRepeatingPattern(1024);
		_mismatchStart1KB[0] ^= 0xFF;

		_mismatchStart10KB = CreateRepeatingPattern(10 * 1024);
		_mismatchStart10KB[0] ^= 0xFF;

		// Mismatch in middle (tests SIMD bulk processing)
		_mismatchMiddle1KB = CreateRepeatingPattern(1024);
		_mismatchMiddle1KB[512] ^= 0xFF;

		_mismatchMiddle10KB = CreateRepeatingPattern(10 * 1024);
		_mismatchMiddle10KB[5 * 1024] ^= 0xFF;

		// Mismatch at end (best case: almost full match)
		_mismatchEnd1KB = CreateRepeatingPattern(1024);
		_mismatchEnd1KB[1023] ^= 0xFF;

		_mismatchEnd10KB = CreateRepeatingPattern(10 * 1024);
		_mismatchEnd10KB[10 * 1024 - 1] ^= 0xFF;
	}

	private byte[] CreateRepeatingPattern(int size) {
		byte[] data = new byte[size];
		for (int i = 0; i < size; i++) {
			data[i] = _pattern[i % _pattern.Length];
		}
		return data;
	}

	/// <summary>
	/// Clean up temporary files after benchmarking.
	/// </summary>
	[GlobalCleanup]
	public void GlobalCleanup() {
		// Nothing to clean up
	}

	// ========================================================================================================
	// SIMD (Vector<byte>) Benchmarks
	// ========================================================================================================

	[Benchmark(Description = "SIMD: Identical 1KB")]
	public int Simd_Identical_1KB() =>
		Encoder.CheckRun(_identical1KB, _identical1KB).Length;

	[Benchmark(Description = "SIMD: Identical 10KB")]
	public int Simd_Identical_10KB() =>
		Encoder.CheckRun(_identical10KB, _identical10KB).Length;

	[Benchmark(Description = "SIMD: Identical 100KB")]
	public int Simd_Identical_100KB() =>
		Encoder.CheckRun(_identical100KB, _identical100KB).Length;

	[Benchmark(Description = "SIMD: Identical 1MB")]
	public int Simd_Identical_1MB() =>
		Encoder.CheckRun(_identical1MB, _identical1MB).Length;

	[Benchmark(Description = "SIMD: Mismatch at start 1KB")]
	public int Simd_MismatchStart_1KB() =>
		Encoder.CheckRun(_identical1KB, _mismatchStart1KB).Length;

	[Benchmark(Description = "SIMD: Mismatch in middle 1KB")]
	public int Simd_MismatchMiddle_1KB() =>
		Encoder.CheckRun(_identical1KB, _mismatchMiddle1KB).Length;

	[Benchmark(Description = "SIMD: Mismatch at end 1KB")]
	public int Simd_MismatchEnd_1KB() =>
		Encoder.CheckRun(_identical1KB, _mismatchEnd1KB).Length;

	[Benchmark(Description = "SIMD: Mismatch at start 10KB")]
	public int Simd_MismatchStart_10KB() =>
		Encoder.CheckRun(_identical10KB, _mismatchStart10KB).Length;

	[Benchmark(Description = "SIMD: Mismatch in middle 10KB")]
	public int Simd_MismatchMiddle_10KB() =>
		Encoder.CheckRun(_identical10KB, _mismatchMiddle10KB).Length;

	[Benchmark(Description = "SIMD: Mismatch at end 10KB")]
	public int Simd_MismatchEnd_10KB() =>
		Encoder.CheckRun(_identical10KB, _mismatchEnd10KB).Length;

	// ========================================================================================================
	// Scalar (byte-by-byte) Benchmarks
	// ========================================================================================================

	[Benchmark(Description = "Scalar: Identical 1KB", Baseline = true)]
	public int Scalar_Identical_1KB() =>
		Encoder.CheckRunScalar(_identical1KB, _identical1KB).Length;

	[Benchmark(Description = "Scalar: Identical 10KB")]
	public int Scalar_Identical_10KB() =>
		Encoder.CheckRunScalar(_identical10KB, _identical10KB).Length;

	[Benchmark(Description = "Scalar: Identical 100KB")]
	public int Scalar_Identical_100KB() =>
		Encoder.CheckRunScalar(_identical100KB, _identical100KB).Length;

	[Benchmark(Description = "Scalar: Identical 1MB")]
	public int Scalar_Identical_1MB() =>
		Encoder.CheckRunScalar(_identical1MB, _identical1MB).Length;

	[Benchmark(Description = "Scalar: Mismatch at start 1KB")]
	public int Scalar_MismatchStart_1KB() =>
		Encoder.CheckRunScalar(_identical1KB, _mismatchStart1KB).Length;

	[Benchmark(Description = "Scalar: Mismatch in middle 1KB")]
	public int Scalar_MismatchMiddle_1KB() =>
		Encoder.CheckRunScalar(_identical1KB, _mismatchMiddle1KB).Length;

	[Benchmark(Description = "Scalar: Mismatch at end 1KB")]
	public int Scalar_MismatchEnd_1KB() =>
		Encoder.CheckRunScalar(_identical1KB, _mismatchEnd1KB).Length;

	[Benchmark(Description = "Scalar: Mismatch at start 10KB")]
	public int Scalar_MismatchStart_10KB() =>
		Encoder.CheckRunScalar(_identical10KB, _mismatchStart10KB).Length;

	[Benchmark(Description = "Scalar: Mismatch in middle 10KB")]
	public int Scalar_MismatchMiddle_10KB() =>
		Encoder.CheckRunScalar(_identical10KB, _mismatchMiddle10KB).Length;

	[Benchmark(Description = "Scalar: Mismatch at end 10KB")]
	public int Scalar_MismatchEnd_10KB() =>
		Encoder.CheckRunScalar(_identical10KB, _mismatchEnd10KB).Length;
}

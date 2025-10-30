// ========================================================================================================
// Encoder Performance Benchmarks
// ========================================================================================================
// Performance benchmarks for BPS patch creation with various scenarios:
// - Different file sizes
// - Different change patterns (identical, small changes, large changes)
// - Pattern matching scenarios (repetitive data, random data)
//
// References:
// - BenchmarkDotNet: https://benchmarkdotnet.org/
// - Performance Optimization: https://learn.microsoft.com/en-us/dotnet/standard/memory-and-spans/
// ========================================================================================================

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace bps_patch.Benchmarks;

/// <summary>
/// Benchmarks for Encoder operations.
/// </summary>
[SimpleJob]
[MemoryDiagnoser]
[MarkdownExporter]
public class EncoderBenchmarks {
	private string _source1KB = null!;
	private string _source10KB = null!;
	private string _source100KB = null!;
	private string _source1MB = null!;

	private string _targetIdentical1KB = null!;
	private string _targetSmallChange1KB = null!;
	private string _targetLargeChange1KB = null!;

	private string _targetIdentical10KB = null!;
	private string _targetSmallChange10KB = null!;

	private string _targetIdentical100KB = null!;
	private string _targetSmallChange100KB = null!;

	private string _targetIdentical1MB = null!;
	private string _targetSmallChange1MB = null!;

	private string _patchTemp = null!;

	/// <summary>
	/// Set up test files for benchmarking.
	/// </summary>
	[GlobalSetup]
	public void GlobalSetup() {
		_patchTemp = Path.Combine(Path.GetTempPath(), $"bps_bench_patch_{Guid.NewGuid()}.bps");

		// Create source files
		_source1KB = CreateRandomFile(1024);
		_source10KB = CreateRandomFile(10 * 1024);
		_source100KB = CreateRandomFile(100 * 1024);
		_source1MB = CreateRandomFile(1024 * 1024);

		// Create identical targets
		_targetIdentical1KB = CopyFile(_source1KB);
		_targetIdentical10KB = CopyFile(_source10KB);
		_targetIdentical100KB = CopyFile(_source100KB);
		_targetIdentical1MB = CopyFile(_source1MB);

		// Create targets with small changes (10 bytes changed)
		_targetSmallChange1KB = CreateModifiedFile(_source1KB, 10);
		_targetSmallChange10KB = CreateModifiedFile(_source10KB, 10);
		_targetSmallChange100KB = CreateModifiedFile(_source100KB, 10);
		_targetSmallChange1MB = CreateModifiedFile(_source1MB, 10);

		// Create target with large changes (50% of bytes changed)
		_targetLargeChange1KB = CreateModifiedFile(_source1KB, 512);
	}

	/// <summary>
	/// Clean up temporary files.
	/// </summary>
	[GlobalCleanup]
	public void GlobalCleanup() {
		DeleteFileIfExists(_patchTemp);
		DeleteFileIfExists(_source1KB);
		DeleteFileIfExists(_source10KB);
		DeleteFileIfExists(_source100KB);
		DeleteFileIfExists(_source1MB);
		DeleteFileIfExists(_targetIdentical1KB);
		DeleteFileIfExists(_targetIdentical10KB);
		DeleteFileIfExists(_targetIdentical100KB);
		DeleteFileIfExists(_targetIdentical1MB);
		DeleteFileIfExists(_targetSmallChange1KB);
		DeleteFileIfExists(_targetSmallChange10KB);
		DeleteFileIfExists(_targetSmallChange100KB);
		DeleteFileIfExists(_targetSmallChange1MB);
		DeleteFileIfExists(_targetLargeChange1KB);
	}

	// ============================================================
	// Identical Files (Minimal Patch)
	// ============================================================

	[Benchmark]
	public void Encode_Identical_1KB() {
		Encoder.CreatePatch(
			new FileInfo(_source1KB),
			new FileInfo(_patchTemp),
			new FileInfo(_targetIdentical1KB),
			"");
	}

	[Benchmark]
	public void Encode_Identical_10KB() {
		Encoder.CreatePatch(
			new FileInfo(_source10KB),
			new FileInfo(_patchTemp),
			new FileInfo(_targetIdentical10KB),
			"");
	}

	[Benchmark]
	public void Encode_Identical_100KB() {
		Encoder.CreatePatch(
			new FileInfo(_source100KB),
			new FileInfo(_patchTemp),
			new FileInfo(_targetIdentical100KB),
			"");
	}

	[Benchmark]
	public void Encode_Identical_1MB() {
		Encoder.CreatePatch(
			new FileInfo(_source1MB),
			new FileInfo(_patchTemp),
			new FileInfo(_targetIdentical1MB),
			"");
	}

	// ============================================================
	// Small Changes (10 bytes modified)
	// ============================================================

	[Benchmark]
	public void Encode_SmallChange_1KB() {
		Encoder.CreatePatch(
			new FileInfo(_source1KB),
			new FileInfo(_patchTemp),
			new FileInfo(_targetSmallChange1KB),
			"");
	}

	[Benchmark]
	public void Encode_SmallChange_10KB() {
		Encoder.CreatePatch(
			new FileInfo(_source10KB),
			new FileInfo(_patchTemp),
			new FileInfo(_targetSmallChange10KB),
			"");
	}

	[Benchmark]
	public void Encode_SmallChange_100KB() {
		Encoder.CreatePatch(
			new FileInfo(_source100KB),
			new FileInfo(_patchTemp),
			new FileInfo(_targetSmallChange100KB),
			"");
	}

	[Benchmark]
	public void Encode_SmallChange_1MB() {
		Encoder.CreatePatch(
			new FileInfo(_source1MB),
			new FileInfo(_patchTemp),
			new FileInfo(_targetSmallChange1MB),
			"");
	}

	// ============================================================
	// Large Changes (50% of file modified)
	// ============================================================

	[Benchmark]
	public void Encode_LargeChange_1KB() {
		Encoder.CreatePatch(
			new FileInfo(_source1KB),
			new FileInfo(_patchTemp),
			new FileInfo(_targetLargeChange1KB),
			"");
	}

	// ============================================================
	// Helper Methods
	// ============================================================

	/// <summary>
	/// Creates a temporary file with random data of specified size.
	/// </summary>
	private static string CreateRandomFile(int size) {
		var path = Path.Combine(Path.GetTempPath(), $"bps_bench_{Guid.NewGuid()}.tmp");
		var data = new byte[size];
		Random.Shared.NextBytes(data);
		File.WriteAllBytes(path, data);
		return path;
	}

	/// <summary>
	/// Copies a file to a new temp location.
	/// </summary>
	private static string CopyFile(string source) {
		var dest = Path.Combine(Path.GetTempPath(), $"bps_bench_{Guid.NewGuid()}.tmp");
		File.Copy(source, dest);
		return dest;
	}

	/// <summary>
	/// Creates a modified version of a file with n bytes changed.
	/// </summary>
	private static string CreateModifiedFile(string source, int bytesToChange) {
		var data = File.ReadAllBytes(source);
		var random = new Random(42); // Deterministic for reproducible benchmarks

		// Modify n random bytes
		for (int i = 0; i < bytesToChange && i < data.Length; i++) {
			int index = random.Next(data.Length);
			data[index] = (byte)~data[index]; // Flip all bits
		}

		var dest = Path.Combine(Path.GetTempPath(), $"bps_bench_{Guid.NewGuid()}.tmp");
		File.WriteAllBytes(dest, data);
		return dest;
	}

	/// <summary>
	/// Deletes a file if it exists.
	/// </summary>
	private static void DeleteFileIfExists(string path) {
		if (File.Exists(path)) {
			File.Delete(path);
		}
	}
}

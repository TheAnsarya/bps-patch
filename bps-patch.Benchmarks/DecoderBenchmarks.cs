// ========================================================================================================
// Decoder Performance Benchmarks
// ========================================================================================================
// Performance benchmarks for BPS patch application with various scenarios:
// - Different file sizes
// - Different patch complexities
// - Buffered streaming performance
//
// References:
// - BenchmarkDotNet: https://benchmarkdotnet.org/
// - BufferedStream: https://learn.microsoft.com/en-us/dotnet/api/system.io.bufferedstream
// ========================================================================================================

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace bps_patch.Benchmarks;

/// <summary>
/// Benchmarks for Decoder operations.
/// </summary>
[SimpleJob]
[MemoryDiagnoser]
[MarkdownExporter]
public class DecoderBenchmarks {
	private string _source1KB = null!;
	private string _source10KB = null!;
	private string _source100KB = null!;
	private string _source1MB = null!;

	private string _patch1KB_Identical = null!;
	private string _patch1KB_SmallChange = null!;
	private string _patch10KB_Identical = null!;
	private string _patch10KB_SmallChange = null!;
	private string _patch100KB_Identical = null!;
	private string _patch100KB_SmallChange = null!;
	private string _patch1MB_Identical = null!;
	private string _patch1MB_SmallChange = null!;

	private string _targetTemp = null!;

	/// <summary>
	/// Set up test files and patches for benchmarking.
	/// </summary>
	[GlobalSetup]
	public void GlobalSetup() {
		_targetTemp = Path.Combine(Path.GetTempPath(), $"bps_bench_target_{Guid.NewGuid()}.tmp");

		// Create source files
		_source1KB = CreateRandomFile(1024);
		_source10KB = CreateRandomFile(10 * 1024);
		_source100KB = CreateRandomFile(100 * 1024);
		_source1MB = CreateRandomFile(1024 * 1024);

		// Create patches for identical files
		_patch1KB_Identical = CreatePatch(_source1KB, _source1KB);
		_patch10KB_Identical = CreatePatch(_source10KB, _source10KB);
		_patch100KB_Identical = CreatePatch(_source100KB, _source100KB);
		_patch1MB_Identical = CreatePatch(_source1MB, _source1MB);

		// Create patches for files with small changes
		_patch1KB_SmallChange = CreatePatchWithModification(_source1KB, 10);
		_patch10KB_SmallChange = CreatePatchWithModification(_source10KB, 10);
		_patch100KB_SmallChange = CreatePatchWithModification(_source100KB, 10);
		_patch1MB_SmallChange = CreatePatchWithModification(_source1MB, 10);
	}

	/// <summary>
	/// Clean up temporary files.
	/// </summary>
	[GlobalCleanup]
	public void GlobalCleanup() {
		DeleteFileIfExists(_targetTemp);
		DeleteFileIfExists(_source1KB);
		DeleteFileIfExists(_source10KB);
		DeleteFileIfExists(_source100KB);
		DeleteFileIfExists(_source1MB);
		DeleteFileIfExists(_patch1KB_Identical);
		DeleteFileIfExists(_patch1KB_SmallChange);
		DeleteFileIfExists(_patch10KB_Identical);
		DeleteFileIfExists(_patch10KB_SmallChange);
		DeleteFileIfExists(_patch100KB_Identical);
		DeleteFileIfExists(_patch100KB_SmallChange);
		DeleteFileIfExists(_patch1MB_Identical);
		DeleteFileIfExists(_patch1MB_SmallChange);
	}

	// ============================================================
	// Identical Files (Minimal Patch Application)
	// ============================================================

	[Benchmark]
	public void Decode_Identical_1KB() {
		Decoder.ApplyPatch(
			new FileInfo(_source1KB),
			new FileInfo(_patch1KB_Identical),
			new FileInfo(_targetTemp));
	}

	[Benchmark]
	public void Decode_Identical_10KB() {
		Decoder.ApplyPatch(
			new FileInfo(_source10KB),
			new FileInfo(_patch10KB_Identical),
			new FileInfo(_targetTemp));
	}

	[Benchmark]
	public void Decode_Identical_100KB() {
		Decoder.ApplyPatch(
			new FileInfo(_source100KB),
			new FileInfo(_patch100KB_Identical),
			new FileInfo(_targetTemp));
	}

	[Benchmark]
	public void Decode_Identical_1MB() {
		Decoder.ApplyPatch(
			new FileInfo(_source1MB),
			new FileInfo(_patch1MB_Identical),
			new FileInfo(_targetTemp));
	}

	// ============================================================
	// Small Changes (10 bytes modified)
	// ============================================================

	[Benchmark]
	public void Decode_SmallChange_1KB() {
		Decoder.ApplyPatch(
			new FileInfo(_source1KB),
			new FileInfo(_patch1KB_SmallChange),
			new FileInfo(_targetTemp));
	}

	[Benchmark]
	public void Decode_SmallChange_10KB() {
		Decoder.ApplyPatch(
			new FileInfo(_source10KB),
			new FileInfo(_patch10KB_SmallChange),
			new FileInfo(_targetTemp));
	}

	[Benchmark]
	public void Decode_SmallChange_100KB() {
		Decoder.ApplyPatch(
			new FileInfo(_source100KB),
			new FileInfo(_patch100KB_SmallChange),
			new FileInfo(_targetTemp));
	}

	[Benchmark]
	public void Decode_SmallChange_1MB() {
		Decoder.ApplyPatch(
			new FileInfo(_source1MB),
			new FileInfo(_patch1MB_SmallChange),
			new FileInfo(_targetTemp));
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
	/// Creates a patch file from source to target.
	/// </summary>
	private static string CreatePatch(string source, string target) {
		var patchPath = Path.Combine(Path.GetTempPath(), $"bps_bench_{Guid.NewGuid()}.bps");
		Encoder.CreatePatch(
			new FileInfo(source),
			new FileInfo(patchPath),
			new FileInfo(target),
			"");
		return patchPath;
	}

	/// <summary>
	/// Creates a patch with n bytes modified in target.
	/// </summary>
	private static string CreatePatchWithModification(string source, int bytesToChange) {
		var data = File.ReadAllBytes(source);
		var random = new Random(42); // Deterministic for reproducible benchmarks

		// Modify n random bytes
		for (int i = 0; i < bytesToChange && i < data.Length; i++) {
			int index = random.Next(data.Length);
			data[index] = (byte)~data[index]; // Flip all bits
		}

		var target = Path.Combine(Path.GetTempPath(), $"bps_bench_{Guid.NewGuid()}.tmp");
		File.WriteAllBytes(target, data);

		var patchPath = CreatePatch(source, target);
		File.Delete(target);
		return patchPath;
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

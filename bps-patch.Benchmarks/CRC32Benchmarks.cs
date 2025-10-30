// ========================================================================================================
// CRC32 Computation Benchmarks
// ========================================================================================================
// Performance benchmarks for CRC32 computation with various file sizes and buffer strategies.
//
// Benchmarks:
// - Various file sizes: 1KB to 10MB
// - Buffer boundary conditions (80KB BufferedStream)
// - ComputeCRC32 vs ComputeCRC32Bytes
//
// References:
// - BenchmarkDotNet: https://benchmarkdotnet.org/articles/guides/choosing-run-strategy.html
// - System.IO.Hashing.Crc32: https://learn.microsoft.com/en-us/dotnet/api/system.io.hashing.crc32
// ========================================================================================================

namespace bps_patch.Benchmarks;

/// <summary>
/// Benchmarks for CRC32 computation operations.
/// </summary>
[SimpleJob(RuntimeMoniker.Net100)]
[MemoryDiagnoser]
[MarkdownExporter]
public class CRC32Benchmarks {
	private string _tempFile1KB = null!;
	private string _tempFile10KB = null!;
	private string _tempFile100KB = null!;
	private string _tempFile1MB = null!;
	private string _tempFile10MB = null!;

	/// <summary>
	/// Set up test files with various sizes.
	/// </summary>
	[GlobalSetup]
	public void GlobalSetup() {
		_tempFile1KB = CreateTempFile(1024);
		_tempFile10KB = CreateTempFile(10 * 1024);
		_tempFile100KB = CreateTempFile(100 * 1024);
		_tempFile1MB = CreateTempFile(1024 * 1024);
		_tempFile10MB = CreateTempFile(10 * 1024 * 1024);
	}

	/// <summary>
	/// Clean up temporary files.
	/// </summary>
	[GlobalCleanup]
	public void GlobalCleanup() {
		DeleteFileIfExists(_tempFile1KB);
		DeleteFileIfExists(_tempFile10KB);
		DeleteFileIfExists(_tempFile100KB);
		DeleteFileIfExists(_tempFile1MB);
		DeleteFileIfExists(_tempFile10MB);
	}

	// ============================================================
	// CRC32 Computation Benchmarks - Various File Sizes
	// ============================================================

	[Benchmark]
	public uint CRC32_1KB() => Utilities.ComputeCRC32(new FileInfo(_tempFile1KB));

	[Benchmark]
	public uint CRC32_10KB() => Utilities.ComputeCRC32(new FileInfo(_tempFile10KB));

	[Benchmark]
	public uint CRC32_100KB() => Utilities.ComputeCRC32(new FileInfo(_tempFile100KB));

	[Benchmark]
	public uint CRC32_1MB() => Utilities.ComputeCRC32(new FileInfo(_tempFile1MB));

	[Benchmark]
	public uint CRC32_10MB() => Utilities.ComputeCRC32(new FileInfo(_tempFile10MB));

	// ============================================================
	// CRC32Bytes Benchmarks
	// ============================================================

	[Benchmark]
	public byte[] CRC32Bytes_1KB() => Utilities.ComputeCRC32Bytes(new FileInfo(_tempFile1KB));

	[Benchmark]
	public byte[] CRC32Bytes_1MB() => Utilities.ComputeCRC32Bytes(new FileInfo(_tempFile1MB));

	// ============================================================
	// Helper Methods
	// ============================================================

	/// <summary>
	/// Creates a temporary file with random data of specified size.
	/// </summary>
	private static string CreateTempFile(int size) {
		var path = Path.Combine(Path.GetTempPath(), $"bps_bench_{Guid.NewGuid()}.tmp");
		var data = new byte[size];
		Random.Shared.NextBytes(data);
		File.WriteAllBytes(path, data);
		return path;
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

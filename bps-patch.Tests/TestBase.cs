namespace bps_patch.Tests;

/// <summary>
/// Base class for all test classes providing common utilities for file handling.
/// Ensures proper file sharing to avoid lock issues during parallel test execution.
/// </summary>
public abstract class TestBase : IDisposable {
	/// <summary>
	/// List of temporary files created by this test for cleanup.
	/// </summary>
	private readonly List<string> _tempFiles = [];

	/// <summary>
	/// Unique instance identifier to prevent file collisions across test instances.
	/// </summary>
	private readonly string _instanceId = Guid.NewGuid().ToString("N")[..8];

	/// <summary>
	/// Creates a temporary file path and tracks it for cleanup.
	/// Each file gets a unique name combining class name, instance ID, thread ID, and GUID.
	/// </summary>
	protected string GetCleanTempFile(string? prefix = null) {
		var className = GetType().Name.Replace("Tests", "");
		var threadId = Environment.CurrentManagedThreadId;
		var uniqueId = Guid.NewGuid().ToString("N")[..12]; // Shorter GUID

		var fileName = prefix != null
			? $"bps_{className}_{prefix}_{_instanceId}_{threadId}_{uniqueId}.tmp"
			: $"bps_{className}_{_instanceId}_{threadId}_{uniqueId}.tmp";

		var path = Path.Combine(Path.GetTempPath(), fileName);
		_tempFiles.Add(path);
		return path;
	}

	/// <summary>
	/// Writes bytes to a file with proper file sharing to avoid locking issues.
	/// Uses FileShare.Read to allow concurrent reads during write.
	/// Ensures file handle is properly closed and file is readable before returning.
	/// </summary>
	protected static void WriteAllBytesWithSharing(string path, byte[] bytes) {
		// Write the file - allow sharing for reads
		using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read)) {
			stream.Write(bytes);
			stream.Flush(true); // Force flush to disk
		} // Explicit dispose

		// Verify the file is readable to ensure write handle is fully released
		// This prevents race conditions where subsequent reads fail
		bool success = false;
		for (int attempt = 0; attempt < 5; attempt++) {
			try {
				// Try to open for reading - this will fail if write handle isn't released
				using (var testRead = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
					// File is accessible
					success = true;
				} // Dispose immediately

				if (success) break; // Exit retry loop
			} catch (IOException) when (attempt < 4) {
				// File still locked, wait and retry
				Thread.Sleep(10);
			}
		}
	}

	/// <summary>
	/// Reads all bytes from a file with proper sharing mode.
	/// </summary>
	protected static byte[] ReadAllBytesWithSharing(string path) {
		// Retry logic for transient file access issues
		for (int attempt = 0; attempt < 3; attempt++) {
			try {
				using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				var buffer = new byte[stream.Length];
				stream.ReadExactly(buffer);
				return buffer;
			} catch (IOException) when (attempt < 2) {
				Thread.Sleep(10);
			}
		}

		// Final attempt without catching
		using var finalStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		var finalBuffer = new byte[finalStream.Length];
		finalStream.ReadExactly(finalBuffer);
		return finalBuffer;
	}

	/// <summary>
	/// Safely deletes a file if it exists, suppressing any errors.
	/// </summary>
	protected static void SafeDelete(string path) {
		try {
			if (File.Exists(path)) {
				// Try to delete with retry for locked files
				for (int i = 0; i < 3; i++) {
					try {
						File.Delete(path);
						break;
					} catch (IOException) when (i < 2) {
						// File might be locked, wait a bit
						Thread.Sleep(10);
					}
				}
			}
		} catch {
			// Ignore cleanup errors - temp files will be cleaned by OS eventually
		}
	}

	/// <summary>
	/// Cleanup: Deletes all temporary files created by this test.
	/// </summary>
	public virtual void Dispose() {
		foreach (var file in _tempFiles) {
			SafeDelete(file);
		}
		_tempFiles.Clear();
		GC.SuppressFinalize(this);
	}
}

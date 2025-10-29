using System.IO.Hashing;
using System.Text;

namespace bps_patch;

/// <summary>
/// Utility methods for CRC32 computation and validation.
/// See: https://learn.microsoft.com/en-us/dotnet/api/system.io.hashing.crc32
/// </summary>
static class Utilities {
	/// <summary>
	/// Magic constant: CRC32(data + CRC32(data)) always equals this value.
	/// Used to validate patch file integrity without knowing original CRC.
	/// See: https://en.wikipedia.org/wiki/Cyclic_redundancy_check
	/// </summary>
	public const uint CRC32_RESULT_CONSTANT = 0x2144df1c;

	/// <summary>
	/// Computes CRC32 checksum for a file using buffered reading.
	/// </summary>
	/// <param name="sourceFile">File to compute CRC32 for.</param>
	/// <returns>CRC32 checksum as unsigned 32-bit integer.</returns>
	public static uint ComputeCRC32(FileInfo sourceFile) {
		// Open file for reading
		using var source = sourceFile.OpenRead();

		// Allocate 80KB buffer on stack for efficient I/O
		// See: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/stackalloc
		Span<byte> buffer = stackalloc byte[81920];

		// Create CRC32 hasher instance
		var crc32 = new Crc32();

		// Read file in chunks and update CRC32 incrementally
		int bytesRead;
		while ((bytesRead = source.Read(buffer)) > 0) {
			// Append chunk to running CRC32 calculation
			crc32.Append(buffer[..bytesRead]);
		}

		// Get final hash value (4 bytes)
		Span<byte> hashBytes = stackalloc byte[4];
		crc32.GetHashAndReset(hashBytes);

		// Convert bytes to uint (little-endian)
		// See: https://learn.microsoft.com/en-us/dotnet/api/system.bitconverter
		return BitConverter.ToUInt32(hashBytes);
	}

	/// <summary>
	/// Computes CRC32 checksum for a file and returns as byte array.
	/// </summary>
	/// <param name="sourceFile">File to compute CRC32 for.</param>
	/// <returns>CRC32 checksum as 4-byte array (little-endian).</returns>
	public static byte[] ComputeCRC32Bytes(FileInfo sourceFile) {
		// Open file for reading
		using var source = sourceFile.OpenRead();

		// Allocate 80KB buffer on stack for efficient I/O
		Span<byte> buffer = stackalloc byte[81920];

		// Create CRC32 hasher instance
		var crc32 = new Crc32();

		// Read file in chunks and update CRC32 incrementally
		int bytesRead;
		while ((bytesRead = source.Read(buffer)) > 0) {
			// Append chunk to running CRC32 calculation
			crc32.Append(buffer[..bytesRead]);
		}

		// Get final hash value as byte array
		byte[] result = new byte[4];
		crc32.GetHashAndReset(result);

		return result;
	}
}





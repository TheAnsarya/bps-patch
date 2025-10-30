// ========================================================================================================
// Memory-Mapped File Support for BPS Encoder/Decoder
// ========================================================================================================
// Provides memory-mapped file access for processing very large files (> RAM size).
// Uses System.IO.MemoryMappedFiles to map files into virtual memory with OS-managed paging.
//
// Benefits:
// - Process files larger than available RAM
// - OS handles paging automatically
// - Efficient random access without loading entire file
//
// References:
// - Memory-Mapped Files: https://learn.microsoft.com/en-us/dotnet/standard/io/memory-mapped-files
// - Virtual Memory: https://en.wikipedia.org/wiki/Virtual_memory
// ========================================================================================================

using System.IO.MemoryMappedFiles;

namespace bps_patch;

/// <summary>
/// Provides memory-mapped file access for large file processing.
/// </summary>
public static class MemoryMappedFileHelper {
	/// <summary>
	/// Threshold for using memory-mapped files vs loading into RAM.
	/// Files larger than 256MB use memory-mapped access.
	/// </summary>
	public const long MEMORY_MAPPED_THRESHOLD = 256 * 1024 * 1024; // 256 MB

	/// <summary>
	/// Creates a memory-mapped view accessor for reading a file.
	/// </summary>
	/// <param name="file">File to map.</param>
	/// <returns>Memory-mapped file and view accessor.</returns>
	public static (MemoryMappedFile MappedFile, MemoryMappedViewAccessor Accessor) CreateReadAccessor(FileInfo file) {
		var mappedFile = MemoryMappedFile.CreateFromFile(
			file.FullName,
			FileMode.Open,
			null, // mapName
			0,    // capacity (0 = use file size)
			MemoryMappedFileAccess.Read);

		var accessor = mappedFile.CreateViewAccessor(
			0,      // offset
			0,      // size (0 = entire file)
			MemoryMappedFileAccess.Read);

		return (mappedFile, accessor);
	}

	/// <summary>
	/// Creates a memory-mapped view accessor for writing a file.
	/// </summary>
	/// <param name="file">File to map.</param>
	/// <param name="capacity">Size of the mapped file.</param>
	/// <returns>Memory-mapped file and view accessor.</returns>
	public static (MemoryMappedFile MappedFile, MemoryMappedViewAccessor Accessor) CreateWriteAccessor(FileInfo file, long capacity) {
		var mappedFile = MemoryMappedFile.CreateFromFile(
			file.FullName,
			FileMode.Create,
			null,     // mapName
			capacity,
			MemoryMappedFileAccess.ReadWrite);

		var accessor = mappedFile.CreateViewAccessor(
			0,        // offset
			capacity,
			MemoryMappedFileAccess.ReadWrite);

		return (mappedFile, accessor);
	}

	/// <summary>
	/// Reads a byte array from a memory-mapped file accessor.
	/// For small regions, more efficient than loading entire file.
	/// </summary>
	/// <param name="accessor">View accessor to read from.</param>
	/// <param name="offset">Offset in the file.</param>
	/// <param name="length">Number of bytes to read.</param>
	/// <returns>Byte array with data.</returns>
	public static byte[] ReadBytes(MemoryMappedViewAccessor accessor, long offset, int length) {
		byte[] buffer = new byte[length];
		accessor.ReadArray(offset, buffer, 0, length);
		return buffer;
	}

	/// <summary>
	/// Writes a byte array to a memory-mapped file accessor.
	/// </summary>
	/// <param name="accessor">View accessor to write to.</param>
	/// <param name="offset">Offset in the file.</param>
	/// <param name="data">Data to write.</param>
	public static void WriteBytes(MemoryMappedViewAccessor accessor, long offset, byte[] data) {
		accessor.WriteArray(offset, data, 0, data.Length);
	}

	/// <summary>
	/// Determines if a file should use memory-mapped access based on size.
	/// </summary>
	/// <param name="fileSize">Size of the file in bytes.</param>
	/// <returns>True if file should use memory-mapped access.</returns>
	public static bool ShouldUseMemoryMapped(long fileSize) {
		return fileSize > MEMORY_MAPPED_THRESHOLD;
	}

	/// <summary>
	/// Computes CRC32 of a memory-mapped file in chunks.
	/// Avoids loading entire file into RAM.
	/// </summary>
	/// <param name="accessor">View accessor to read from.</param>
	/// <param name="length">Total length of data.</param>
	/// <param name="chunkSize">Chunk size for processing (default 1MB).</param>
	/// <returns>CRC32 hash bytes.</returns>
	public static byte[] ComputeCRC32Chunked(MemoryMappedViewAccessor accessor, long length, int chunkSize = 1024 * 1024) {
		var crc = new System.IO.Hashing.Crc32();
		byte[] buffer = new byte[chunkSize];

		long offset = 0;
		while (offset < length) {
			int toRead = (int)Math.Min(chunkSize, length - offset);
			accessor.ReadArray(offset, buffer, 0, toRead);
			crc.Append(buffer.AsSpan(0, toRead));
			offset += toRead;
		}

		return crc.GetHashAndReset();
	}

	/// <summary>
	/// Compares two byte regions in memory-mapped files for equality.
	/// Useful for SourceRead/SourceCopy matching in encoder.
	/// </summary>
	/// <param name="accessor1">First view accessor.</param>
	/// <param name="offset1">Offset in first file.</param>
	/// <param name="accessor2">Second view accessor.</param>
	/// <param name="offset2">Offset in second file.</param>
	/// <param name="length">Number of bytes to compare.</param>
	/// <returns>True if regions are equal.</returns>
	public static bool CompareRegions(
		MemoryMappedViewAccessor accessor1, long offset1,
		MemoryMappedViewAccessor accessor2, long offset2,
		int length) {

		// Compare in chunks to avoid large allocations
		const int chunkSize = 4096; // 4KB chunks
		byte[] buffer1 = new byte[chunkSize];
		byte[] buffer2 = new byte[chunkSize];

		int remaining = length;
		long pos1 = offset1;
		long pos2 = offset2;

		while (remaining > 0) {
			int toCompare = Math.Min(chunkSize, remaining);

			accessor1.ReadArray(pos1, buffer1, 0, toCompare);
			accessor2.ReadArray(pos2, buffer2, 0, toCompare);

			if (!buffer1.AsSpan(0, toCompare).SequenceEqual(buffer2.AsSpan(0, toCompare))) {
				return false;
			}

			pos1 += toCompare;
			pos2 += toCompare;
			remaining -= toCompare;
		}

		return true;
	}

	/// <summary>
	/// Finds the length of matching bytes starting at given positions.
	/// Optimized for memory-mapped files with chunked reading.
	/// </summary>
	/// <param name="accessor1">First view accessor.</param>
	/// <param name="offset1">Offset in first file.</param>
	/// <param name="maxLength1">Maximum bytes available in first file from offset.</param>
	/// <param name="accessor2">Second view accessor.</param>
	/// <param name="offset2">Offset in second file.</param>
	/// <param name="maxLength2">Maximum bytes available in second file from offset.</param>
	/// <returns>Number of matching bytes.</returns>
	public static int CountMatchingBytes(
		MemoryMappedViewAccessor accessor1, long offset1, int maxLength1,
		MemoryMappedViewAccessor accessor2, long offset2, int maxLength2) {

		int maxLength = Math.Min(maxLength1, maxLength2);
		const int chunkSize = 4096; // 4KB chunks
		byte[] buffer1 = new byte[chunkSize];
		byte[] buffer2 = new byte[chunkSize];

		int totalMatched = 0;
		long pos1 = offset1;
		long pos2 = offset2;

		while (totalMatched < maxLength) {
			int toCheck = Math.Min(chunkSize, maxLength - totalMatched);

			accessor1.ReadArray(pos1, buffer1, 0, toCheck);
			accessor2.ReadArray(pos2, buffer2, 0, toCheck);

			// Compare bytes until mismatch
			for (int i = 0; i < toCheck; i++) {
				if (buffer1[i] != buffer2[i]) {
					return totalMatched + i;
				}
			}

			totalMatched += toCheck;
			pos1 += toCheck;
			pos2 += toCheck;
		}

		return totalMatched;
	}
}

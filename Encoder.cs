using System.Text;
using System.Numerics;

namespace bps_patch;

/// <summary>
/// Creates BPS (Binary Patch System) patch files by analyzing differences between source and target files.
/// Uses modern .NET performance features: ArrayPool, Span&lt;T&gt;, stackalloc, and buffered I/O.
/// See: https://github.com/blakesmith/beat/blob/master/doc/bps.txt
/// </summary>
static class Encoder {
	// 80KB buffer for optimal file I/O performance
	// See: https://learn.microsoft.com/en-us/dotnet/api/system.io.bufferedstream
	private const int BUFFER_SIZE = 81920;

	// Minimum bytes required to encode a match (avoid overhead for tiny matches)
	private const int MIN_MATCH_LENGTH = 4;

	/// <summary>
	/// Creates a BPS patch file by comparing source and target files.
	/// Uses ArrayPool for memory efficiency and buffered I/O for performance.
	/// See: https://learn.microsoft.com/en-us/dotnet/api/system.buffers.arraypool-1
	/// </summary>
	/// <param name="sourceFile">Original file to patch from.</param>
	/// <param name="patchFile">Output patch file to create.</param>
	/// <param name="targetFile">Desired result file after patching.</param>
	/// <param name="manifest">Metadata/manifest string (typically XML).</param>
	public static void CreatePatch(FileInfo sourceFile, FileInfo patchFile, FileInfo targetFile, string manifest) {
		// Ensure files fit in memory (int.MaxValue limit for arrays)
		if (targetFile.Length > int.MaxValue) {
			throw new ArgumentException($"{nameof(targetFile)} is larger than maximum size of {int.MaxValue} bytes");
		}

		if (sourceFile.Length > int.MaxValue) {
			throw new ArgumentException($"{nameof(sourceFile)} is larger than maximum size of {int.MaxValue} bytes");
		}

		// Refresh FileInfo to ensure up-to-date file system information
		// This prevents file locking issues on Windows after recent file writes
		sourceFile.Refresh();
		targetFile.Refresh();
		patchFile.Refresh();

		// Rent buffers from ArrayPool to reduce GC pressure
		// See: https://learn.microsoft.com/en-us/dotnet/standard/collections/thread-safe/how-to-create-an-object-pool
		byte[] sourceData = ArrayPool<byte>.Shared.Rent((int)sourceFile.Length);
		byte[] targetData = ArrayPool<byte>.Shared.Rent((int)targetFile.Length);

		try {
			// Load source file into buffer
			// ReadExactly ensures all bytes are read (prevents partial reads)
			// Use FileShare.ReadWrite to allow concurrent access (e.g., for CRC32 computation)
			using (var sourceStream = new FileStream(sourceFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
				sourceStream.ReadExactly(sourceData.AsSpan(0, (int)sourceFile.Length));
			}

			// Load target file into buffer
			using (var targetStream = new FileStream(targetFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
				targetStream.ReadExactly(targetData.AsSpan(0, (int)targetFile.Length));
			}

			// Create read-only spans for efficient memory access
			// See: https://learn.microsoft.com/en-us/dotnet/api/system.readonlyspan-1
			ReadOnlySpan<byte> source = sourceData.AsSpan(0, (int)sourceFile.Length);
			ReadOnlySpan<byte> target = targetData.AsSpan(0, (int)targetFile.Length);

			// Copy reference for local function access (can't capture ref structs)
			byte[] targetCopy = targetData;
			int targetFileLength = (int)targetFile.Length;

		// Local function to write accumulated TargetRead command
		// Defined before patch stream creation so it can be used in both scopes
		void WriteTargetReadCommand(Stream patchStream, ref int targetPos, ref int readLength, ref int readStart) {
			if (readLength > 0) {
				// Encode TargetRead command
				var command = EncodeNumber((ulong)(((readLength - 1) << 2) + (byte)PatchAction.TargetRead));
				patchStream.Write(command);

				// Write raw bytes from target file
				patchStream.Write(targetCopy.AsSpan(readStart, readLength));

				targetPos += readLength;
				readLength = 0;
				readStart = -1;
			}
		}

		// Create patch file with buffered output for performance
		// Close stream before computing CRC32 to avoid file locking issues
		using (var patch = new BufferedStream(patchFile.OpenWrite(), BUFFER_SIZE)) {			// Write BPS header: "BPS1"
			// Using stackalloc for small temporary buffer (no heap allocation)
			Span<byte> header = stackalloc byte[4];
			Encoding.UTF8.GetBytes("BPS1", header);
			patch.Write(header);

			// Write file sizes using variable-length encoding
			// See: https://en.wikipedia.org/wiki/Variable-length_quantity
			patch.Write(EncodeNumber((ulong)sourceFile.Length));
			patch.Write(EncodeNumber((ulong)targetFile.Length));
			patch.Write(EncodeNumber((ulong)manifest.Length));

			// Write manifest/metadata (typically XML v1.0, UTF-8)
			if (manifest.Length > 0) {
				byte[] manifestBytes = Encoding.UTF8.GetBytes(manifest);
				patch.Write(manifestBytes);
			}

			// Process target file to find optimal patch commands
			int targetReadLength = 0;  // Accumulator for TargetRead bytes
			int targetReadStart = -1;   // Start position of current TargetRead run
			int targetPosition = 0;     // Current position in target file

			// Iterate through target file, finding best match for each position
			while (targetPosition < target.Length) {
				// Find next optimal patch action (SourceRead, SourceCopy, TargetCopy, or TargetRead)
				(PatchAction mode, int length, int start) = FindNextRun(source, target, targetPosition);

				if (mode == PatchAction.TargetRead) {
					// Accumulate consecutive TargetRead bytes (new data)
					targetReadLength++;
					if (targetReadStart == -1) {
						targetReadStart = targetPosition;
					}
			} else {
				// Write accumulated TargetRead command first (if any)
				WriteTargetReadCommand(patch, ref targetPosition, ref targetReadLength, ref targetReadStart);					// Encode command: (length - 1) << 2 | action_type
					// See: https://github.com/blakesmith/beat/blob/master/doc/bps.txt
					var command = EncodeNumber((ulong)(((length - 1) << 2) + (byte)mode));
					patch.Write(command);

					// SourceCopy and TargetCopy include offset (signed zigzag encoding)
					if (mode != PatchAction.SourceRead) {
						var offset = start - targetPosition;
						var isNegative = offset < 0;

						// Zigzag encoding: (abs(n) << 1) | sign_bit
						// See: https://developers.google.com/protocol-buffers/docs/encoding#signed-ints
						var offsetValue = ((ulong)Math.Abs(offset) << 1) + (isNegative ? 1UL : 0);
						var offsetBytes = EncodeNumber(offsetValue);
						patch.Write(offsetBytes);
					}

					targetPosition += length;
				}
		}

		// Write any remaining TargetRead data
		WriteTargetReadCommand(patch, ref targetPosition, ref targetReadLength, ref targetReadStart);
		patch.Flush();

		// Write source and target CRC32s (but NOT patch CRC32 yet)
		// Use span-based CRC32 for source/target since data is already in memory
		byte[] sourceCrc = Utilities.ComputeCRC32Bytes(source);
		byte[] targetCrc = Utilities.ComputeCRC32Bytes(target);
		patch.Write(sourceCrc);
		patch.Write(targetCrc);
		patch.Flush();
	} // Close patch file

	// Refresh FileInfo and compute CRC32 of patch file (header + commands + source_crc + target_crc)
	// See: https://learn.microsoft.com/en-us/dotnet/api/system.io.fileinfo.refresh
	patchFile.Refresh();
	byte[] patchCrc = Utilities.ComputeCRC32Bytes(patchFile);

	// Reopen patch file in append mode to write final patch CRC32
	// When decoder computes CRC32(entire_patch_including_patch_crc), result will be 0x2144df1c
	// This is the CRC32 "residue" property - see BPS specification
	using (var patchAppend = new FileStream(patchFile.FullName, FileMode.Append, FileAccess.Write, FileShare.Read)) {
		patchAppend.Write(patchCrc);
		patchAppend.Flush();
	}
} finally {
	// Always return rented arrays to pool (even on exception)
	// See: https://learn.microsoft.com/en-us/dotnet/api/system.buffers.arraypool-1.return
	ArrayPool<byte>.Shared.Return(sourceData);
	ArrayPool<byte>.Shared.Return(targetData);
}
}

/// <summary>
/// Encodes a number using variable-length encoding (7 bits per byte).
	/// Uses stackalloc for zero heap allocation.
	/// See: https://en.wikipedia.org/wiki/Variable-length_quantity
	/// </summary>
	/// <param name="number">Number to encode.</param>
	/// <returns>Encoded bytes (1-10 bytes for ulong).</returns>
	public static byte[] EncodeNumber(ulong number) {
		// Allocate maximum possible size on stack (10 bytes for ulong)
		Span<byte> buffer = stackalloc byte[10];
		int index = 0;

		while (true) {
			// Extract lowest 7 bits
			byte x = (byte)(number & 0x7f);
			number >>= 7;

			if (number == 0) {
				// Final byte: set MSB to indicate termination
				buffer[index++] = (byte)(0x80 | x);
				return buffer[..index].ToArray();
			}

			// Continuation byte: MSB clear
			buffer[index++] = x;
			number--;
		}
	}

	/// <summary>
	/// Finds the next optimal patch action for the current target position.
	/// Checks SourceRead, SourceCopy, and TargetCopy in order of preference.
	/// </summary>
	/// <param name="source">Source file data.</param>
	/// <param name="target">Target file data.</param>
	/// <param name="targetPosition">Current position in target.</param>
	/// <returns>Tuple of (action type, length, start position).</returns>
	public static (PatchAction Mode, int Length, int Start) FindNextRun(
		ReadOnlySpan<byte> source,
		ReadOnlySpan<byte> target,
		int targetPosition) {

		PatchAction mode = PatchAction.TargetRead;
		int longestRun = MIN_MATCH_LENGTH - 1;
		int longestStart = -1;

		// Check 1: SourceRead (identical bytes at same position in source)
		if (targetPosition < source.Length) {
			(int length, bool reachedEnd) = CheckRun(source[targetPosition..], target[targetPosition..]);

			if (length > longestRun) {
				mode = PatchAction.SourceRead;
				longestRun = length;

				// Early exit if we matched to end of target
				if (reachedEnd) {
					return (mode, longestRun, -1);
				}
			}
		}

		// Check 2: SourceCopy (matching bytes from elsewhere in source)
		{
			(int length, int start, bool reachedEnd) = FindBestRun(source, target[targetPosition..], longestRun + 1);

			if (length > longestRun) {
				mode = PatchAction.SourceCopy;
				longestRun = length;
				longestStart = start;

				// Early exit if we matched to end of target
				if (reachedEnd) {
					return (mode, longestRun, start);
				}
			}
		}

		// Check 3: TargetCopy (RLE-like repetition from earlier in target)
		{
			(int length, int start, bool reachedEnd) = FindBestRun(target, target[targetPosition..], longestRun + 1);

			if (length > longestRun) {
				mode = PatchAction.TargetCopy;
				longestRun = length;
				longestStart = start;

				// Early exit if we matched to end of target
				if (reachedEnd) {
					return (mode, longestRun, start);
				}
			}
		}

		return (mode, longestRun, longestStart);
	}

	/// <summary>
	/// Finds the best matching run in source for the given target pattern.
	/// Uses linear search (O(n²) worst case) - suitable for small files.
	/// For large files, consider using FindBestRunRabinKarp() for O(n) average case.
	/// See: https://en.wikipedia.org/wiki/Suffix_array
	/// </summary>
	/// <param name="source">Data to search in.</param>
	/// <param name="target">Pattern to search for.</param>
	/// <param name="minimumLongestRun">Minimum match length to consider.</param>
	/// <param name="checkUntilMax">Maximum position to check (-1 for all).</param>
	/// <returns>Tuple of (match length, start position, reached end flag).</returns>
	public static (int Length, int Start, bool ReachedEnd) FindBestRun(
		ReadOnlySpan<byte> source,
		ReadOnlySpan<byte> target,
		int minimumLongestRun = MIN_MATCH_LENGTH,
		int checkUntilMax = -1) {

		return FindBestRunLinear(source, target, minimumLongestRun, checkUntilMax);
	}

	/// <summary>
	/// Linear search implementation of FindBestRun (original algorithm).
	/// O(n²) worst case, but simple and effective for small files.
	/// </summary>
	public static (int Length, int Start, bool ReachedEnd) FindBestRunLinear(
		ReadOnlySpan<byte> source,
		ReadOnlySpan<byte> target,
		int minimumLongestRun = MIN_MATCH_LENGTH,
		int checkUntilMax = -1) {

		// Early exit if not enough data
		if (target.IsEmpty || source.Length < minimumLongestRun) {
			return (0, -1, false);
		}

		// Calculate search limit
		int checkUntil = checkUntilMax == -1
			? source.Length - minimumLongestRun
			: Math.Min(checkUntilMax, source.Length - minimumLongestRun);

		int longestRun = 0;
		int longestStart = -1;

		// Linear search through source for best match
		for (int currentStart = 0; currentStart <= checkUntil; currentStart++) {
			(int length, bool reachedEnd) = CheckRun(source[currentStart..], target);

			if (length > longestRun) {
				longestRun = length;
				longestStart = currentStart;

				// Prune search space: no point checking positions that can't beat current best
				checkUntil = Math.Min(checkUntil, source.Length - longestRun);

				// Early exit if matched entire target
				if (reachedEnd) {
					return (longestRun, longestStart, true);
				}
			}
		}

		// Return best match found (or failure)
		if (longestRun >= minimumLongestRun) {
			return (longestRun, longestStart, false);
		}

		return (0, -1, false);
	}

	/// <summary>
	/// Rabin-Karp rolling hash implementation of FindBestRun.
	/// O(n) average case, excellent for large files with repetitive patterns.
	/// </summary>
	public static (int Length, int Start, bool ReachedEnd) FindBestRunRabinKarp(
		ReadOnlySpan<byte> source,
		ReadOnlySpan<byte> target,
		int minimumLongestRun = MIN_MATCH_LENGTH,
		int checkUntilMax = -1) {

		return RabinKarp.FindBestRun(source, target, minimumLongestRun, checkUntilMax);
	}

	/// <summary>
	/// Checks how many consecutive bytes match between source and target.
	/// Uses SIMD (Vector&lt;byte&gt;) for bulk comparison when possible.
	/// See: https://learn.microsoft.com/en-us/dotnet/api/system.numerics.vector-1
	/// </summary>
	/// <param name="source">First data span.</param>
	/// <param name="target">Second data span.</param>
	/// <returns>Tuple of (match length, reached end flag).</returns>
	public static (int Length, bool ReachedEnd) CheckRun(ReadOnlySpan<byte> source, ReadOnlySpan<byte> target) {
		// Handle empty spans
		if (source.IsEmpty || target.IsEmpty) {
			return (0, false);
		}

		// Find maximum possible match length
		int maxLength = Math.Min(source.Length, target.Length);
		int length = 0;

		// SIMD optimization: Use Vector<byte> for bulk comparison
		// Process chunks of Vector<byte>.Count bytes at a time (typically 16 or 32 bytes)
		if (Vector.IsHardwareAccelerated && maxLength >= Vector<byte>.Count) {
			int vectorLength = Vector<byte>.Count;
			int maxVectorIndex = maxLength - vectorLength;

			// Process vectors while we have full-size chunks remaining
			while (length <= maxVectorIndex) {
				var sourceVec = new Vector<byte>(source.Slice(length, vectorLength));
				var targetVec = new Vector<byte>(target.Slice(length, vectorLength));

				// Compare entire vector at once
				if (!Vector.EqualsAll(sourceVec, targetVec)) {
					// Mismatch found in this vector - break to scalar comparison
					break;
				}

				length += vectorLength;
			}
		}

		// Scalar comparison for remaining bytes (or if SIMD not available)
		while (length < maxLength && source[length] == target[length]) {
			length++;
		}

		// Check if we matched entire target
		bool reachedEnd = length == target.Length;

		return (length, reachedEnd);
	}

	/// <summary>
	/// Scalar (non-SIMD) version of CheckRun for benchmarking comparison.
	/// Compares bytes one at a time without Vector optimizations.
	/// </summary>
	/// <param name="source">First data span.</param>
	/// <param name="target">Second data span.</param>
	/// <returns>Tuple of (match length, reached end flag).</returns>
	public static (int Length, bool ReachedEnd) CheckRunScalar(ReadOnlySpan<byte> source, ReadOnlySpan<byte> target) {
		// Handle empty spans
		if (source.IsEmpty || target.IsEmpty) {
			return (0, false);
		}

		// Find maximum possible match length
		int maxLength = Math.Min(source.Length, target.Length);
		int length = 0;

		// Compare bytes until mismatch or end
		while (length < maxLength && source[length] == target[length]) {
			length++;
		}

		// Check if we matched entire target
		bool reachedEnd = length == target.Length;

		return (length, reachedEnd);
	}
}

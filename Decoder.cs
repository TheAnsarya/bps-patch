namespace bps_patch;

/// <summary>
/// Applies BPS (Binary Patch System) patches to reconstruct target files from source files.
/// Uses modern .NET performance features: ArrayPool, Span&lt;T&gt;, stackalloc, and buffered I/O.
/// See: https://github.com/blakesmith/beat/blob/master/doc/bps.txt
/// </summary>
static class Decoder {
	// Minimum valid BPS patch size (header + sizes + footer)
	public const int MIN_PATCH_SIZE = 19;

	// 80KB buffer for optimal file I/O performance
	// See: https://learn.microsoft.com/en-us/dotnet/api/system.io.bufferedstream
	private const int BUFFER_SIZE = 81920;

	/// <summary>
	/// Applies a BPS patch to a source file to create the target file.
	/// Returns list of warning messages for hash mismatches, if any.
	/// See: https://learn.microsoft.com/en-us/dotnet/api/system.buffers.arraypool-1
	/// </summary>
	/// <param name="sourceFile">Original file to patch.</param>
	/// <param name="patchFile">BPS patch file containing differences.</param>
	/// <param name="targetFile">Output file to create.</param>
	/// <returns>List of warnings (empty if all checks pass).</returns>
	public static List<string> ApplyPatch(FileInfo sourceFile, FileInfo patchFile, FileInfo targetFile) {
		// Refresh FileInfo to ensure up-to-date file system information
		// This prevents file locking issues on Windows after recent file writes
		sourceFile.Refresh();
		patchFile.Refresh();
		targetFile.Refresh();

		// Validate patch file size
		if (patchFile.Length < MIN_PATCH_SIZE) {
			throw new PatchFormatException("Patch file too small (minimum 19 bytes)");
		}

		// Open source and patch files with buffering
		// Use FileShare.ReadWrite to allow concurrent access (e.g., for CRC32 computation)
		using var source = new FileStream(sourceFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		using var patch = new BufferedStream(new FileStream(patchFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), BUFFER_SIZE);

		// Verify BPS header: "BPS1"
		// Using stackalloc for small temporary buffer (no heap allocation)
		// See: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/stackalloc
		Span<byte> header = stackalloc byte[4];
		if (patch.Read(header) != 4 || header[0] != 'B' || header[1] != 'P' || header[2] != 'S' || header[3] != '1') {
			throw new PatchFormatException("Invalid BPS header or version mismatch");
		}

		// Read and validate source size from patch
		if ((long)DecodeNumber(patch) != sourceFile.Length) {
			throw new ArgumentException($"{nameof(sourceFile)} - source size mismatch");
		}

		// Read target size from patch
		uint targetSize = (uint)DecodeNumber(patch);

		// Ensure target size fits in memory (int.MaxValue limit for arrays)
		if (targetSize > int.MaxValue) {
			throw new ArgumentException($"Target file size exceeds maximum supported size: {targetSize}");
		}

		// Rent buffer from ArrayPool to reduce GC pressure
		// See: https://learn.microsoft.com/en-us/dotnet/standard/collections/thread-safe/how-to-create-an-object-pool
		byte[] targetData = ArrayPool<byte>.Shared.Rent((int)targetSize);

		try {
			// Create memory stream for building target in memory
			// (Reading from file during write can cause issues with unflushed data)
			using var target = new MemoryStream(targetData, 0, (int)targetSize, true);

			// Read manifest/metadata size
			int metadataSize = (int)DecodeNumber(patch);

			// Rent metadata buffer if needed
			byte[]? metadata = metadataSize > 0 ? ArrayPool<byte>.Shared.Rent(metadataSize) : null;

			try {
				// Read metadata bytes (typically XML)
				if (metadata != null) {
					patch.ReadExactly(metadata.AsSpan(0, metadataSize));
					// var manifest = Encoding.UTF8.GetString(metadata.AsSpan(0, metadataSize));
				}

				// Read CRC32 hashes from end of patch file
				// Format: source_crc32 | target_crc32 | patch_crc32 (12 bytes total)
				var readUntil = patchFile.Length - 12;
				var patchPos = patch.Position;
				patch.Position = readUntil;

				// Read all 3 CRC32 values at once using stackalloc
				Span<byte> hashBuffer = stackalloc byte[12];
				patch.ReadExactly(hashBuffer);

				// Convert bytes to uint using BitConverter (little-endian)
				// See: https://learn.microsoft.com/en-us/dotnet/api/system.bitconverter
				uint sourceHash = BitConverter.ToUInt32(hashBuffer[0..4]);
				uint targetHash = BitConverter.ToUInt32(hashBuffer[4..8]);
				uint patchHash = BitConverter.ToUInt32(hashBuffer[8..12]);

				// Restore patch position to continue reading commands
				patch.Position = patchPos;

				// Process patch commands
				// sourceRelativeOffset tracks position for SourceCopy commands
				// targetRelativeOffset tracks position for TargetCopy commands
				long sourceRelativeOffset = 0;
				long targetRelativeOffset = 0;

				// Read and execute patch commands until we reach the CRC32 footer
				while (patch.Position < readUntil) {
					// Decode command: (length - 1) << 2 | action_type
					int length = (int)DecodeNumber(patch);
					var mode = (PatchAction)(length & 3);  // Extract action from lower 2 bits
					length = (length >> 2) + 1;             // Extract and adjust length

					switch (mode) {
						case PatchAction.SourceRead:
							// Copy unchanged data from source file at current position
							source.Position = target.Position;
							int bytesRead = source.Read(targetData.AsSpan((int)target.Position, length));

							if (bytesRead != length) {
								throw new PatchFormatException("Unexpected end of source file");
							}

							target.Position += length;
							break;

						case PatchAction.TargetRead:
							// Copy new bytes directly from patch file
							patch.ReadExactly(targetData.AsSpan((int)target.Position, length));
							target.Position += length;
							break;

						case PatchAction.SourceCopy:
						case PatchAction.TargetCopy:
							// Decode offset (signed zigzag encoding)
							// See: https://developers.google.com/protocol-buffers/docs/encoding#signed-ints
							int offset = (int)DecodeNumber(patch);
							offset = ((offset & 1) != 0) ? -(offset >> 1) : (offset >> 1);

							if (mode == PatchAction.SourceCopy) {
								// Copy from another location in source file
								sourceRelativeOffset += offset;
								source.Position = sourceRelativeOffset;
								source.ReadExactly(targetData.AsSpan((int)target.Position, length));
								target.Position += length;
								sourceRelativeOffset += length;
							} else {
								// TargetCopy: Copy from earlier in target file
								// May overlap (e.g., RLE pattern: copy byte N to N+1, N+1 to N+2, etc.)
								targetRelativeOffset += offset;
								var srcSpan = targetData.AsSpan((int)targetRelativeOffset, length);
								var dstSpan = targetData.AsSpan((int)target.Position, length);

								// Detect overlapping copy (source and destination overlap)
								// See: https://learn.microsoft.com/en-us/dotnet/api/system.span-1.copyto
								if (targetRelativeOffset < target.Position && targetRelativeOffset + length > target.Position) {
									// Overlapping: must copy byte-by-byte to handle RLE correctly
									for (int i = 0; i < length; i++) {
										dstSpan[i] = srcSpan[i];
									}
								} else {
									// Non-overlapping: use bulk copy for performance
									srcSpan.CopyTo(dstSpan);
								}

								target.Position += length;
								targetRelativeOffset += length;
							}
							break;
					}
				}

				// Write target file with buffering
				using var targetWriter = new BufferedStream(targetFile.OpenWrite(), BUFFER_SIZE);
				targetWriter.Write(targetData.AsSpan(0, (int)targetSize));

				// Validate integrity with CRC32 checks
				// Using collection expression [] for modern C# 12 syntax
				// See: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/collection-expressions
				List<string> warnings = [];             // Check patch file CRC32 (CRC32(patch_data + patch_crc32) == magic constant)
				if (Utilities.ComputeCRC32(patchFile) != Utilities.CRC32_RESULT_CONSTANT) {
					warnings.Add($"{nameof(patchFile)} hash mismatch");
				}

				// Check source file CRC32
				if (Utilities.ComputeCRC32(sourceFile) != sourceHash) {
					warnings.Add($"{nameof(sourceFile)} hash mismatch");
				}

				// Check target file size
				if (targetFile.Length != targetSize) {
					warnings.Add($"{nameof(targetFile)} size mismatch");
				}

				// Check target file CRC32
				if (Utilities.ComputeCRC32(targetFile) != targetHash) {
					warnings.Add($"{nameof(targetFile)} hash mismatch");
				}

				return warnings;
			} finally {
				// Always return metadata buffer to pool (even on exception)
				if (metadata != null) {
					ArrayPool<byte>.Shared.Return(metadata);
				}
			}
		} finally {
			// Always return target buffer to pool (even on exception)
			// See: https://learn.microsoft.com/en-us/dotnet/api/system.buffers.arraypool-1.return
			ArrayPool<byte>.Shared.Return(targetData);
		}
	}

	/// <summary>
	/// Decodes a variable-length encoded number from the stream.
	/// Uses 7 bits per byte with MSB indicating termination.
	/// See: https://en.wikipedia.org/wiki/Variable-length_quantity
	/// </summary>
	/// <param name="stream">Stream to read from.</param>
	/// <returns>Decoded number.</returns>
	private static ulong DecodeNumber(Stream stream) {
		ulong data = 0;
		ulong shift = 1;

		while (true) {
			// Read next byte
			int x = stream.ReadByte();

			if (x == -1) {
				throw new PatchFormatException("Unexpected end of patch file");
			}

			// Extract 7 bits of data (MSB is continuation flag)
			data += (ulong)(x & 0x7f) * shift;

			// Check MSB: if set, this is the final byte
			if ((x & 0x80) != 0) {
				break;
			}

			// Prepare for next byte
			shift <<= 7;
			data += shift;
		}

		return data;
	}
}

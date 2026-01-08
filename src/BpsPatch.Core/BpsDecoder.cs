// ========================================================================================================
// BPS Decoder - Patch Application
// ========================================================================================================
// Applies BPS (Binary Patch System) patches to reconstruct target files from source files.
// Features ArrayPool memory management, buffered I/O, and comprehensive validation.
//
// References:
// - BPS Specification: https://github.com/blakesmith/beat/blob/master/doc/bps.txt
// ========================================================================================================

namespace BpsPatch.Core;

/// <summary>
/// Result of a BPS patch application.
/// </summary>
public sealed class BpsDecodeResult {
	/// <summary>
	/// Whether the patch was applied successfully.
	/// </summary>
	public bool Success { get; init; } = true;

	/// <summary>
	/// Warning messages (non-fatal issues like CRC mismatches).
	/// </summary>
	public List<string> Warnings { get; init; } = [];

	/// <summary>
	/// Metadata from the patch file.
	/// </summary>
	public string Metadata { get; init; } = "";

	/// <summary>
	/// Target file size as specified in the patch.
	/// </summary>
	public long TargetSize { get; init; }

	/// <summary>
	/// Source file size as specified in the patch.
	/// </summary>
	public long SourceSize { get; init; }
}

/// <summary>
/// Options for BPS patch decoding.
/// </summary>
public sealed class BpsDecoderOptions {
	/// <summary>
	/// I/O buffer size in bytes (default 80KB).
	/// </summary>
	public int BufferSize { get; set; } = 81920;

	/// <summary>
	/// Whether to skip CRC32 validation (faster but less safe).
	/// </summary>
	public bool SkipValidation { get; set; } = false;

	/// <summary>
	/// Progress callback invoked during decoding.
	/// </summary>
	public IProgress<DecodingProgress>? Progress { get; set; }
}

/// <summary>
/// Progress information during BPS decoding.
/// </summary>
public readonly struct DecodingProgress {
	/// <summary>
	/// Current position in patch file.
	/// </summary>
	public long Position { get; init; }

	/// <summary>
	/// Total size of patch commands.
	/// </summary>
	public long Total { get; init; }

	/// <summary>
	/// Current phase of decoding.
	/// </summary>
	public string Phase { get; init; }

	/// <summary>
	/// Progress as percentage (0-100).
	/// </summary>
	public double Percentage => Total > 0 ? (double)Position / Total * 100 : 0;
}

/// <summary>
/// Applies BPS patches to reconstruct target files.
/// </summary>
/// <remarks>
/// <para>
/// The decoder reads a BPS patch file and applies its commands to reconstruct
/// the target file from the source file.
/// </para>
/// <para>
/// Features:
/// </para>
/// <list type="bullet">
/// <item><description>ArrayPool memory management</description></item>
/// <item><description>Buffered I/O</description></item>
/// <item><description>CRC32 validation with warnings</description></item>
/// <item><description>Overlap handling for TargetCopy (RLE)</description></item>
/// <item><description>Progress reporting</description></item>
/// </list>
/// </remarks>
public static class BpsDecoder {
	/// <summary>
	/// Minimum valid BPS patch size (header + sizes + footer).
	/// </summary>
	public const int MinimumPatchSize = 19;

	/// <summary>
	/// Applies a BPS patch to a source file.
	/// </summary>
	/// <param name="sourceFile">Original file.</param>
	/// <param name="patchFile">BPS patch file.</param>
	/// <param name="targetFile">Output file to create.</param>
	/// <param name="options">Decoding options (null for defaults).</param>
	/// <returns>Result containing warnings and metadata.</returns>
	/// <exception cref="BpsFormatException">Patch file is malformed.</exception>
	/// <exception cref="ArgumentException">Source size mismatch.</exception>
	public static BpsDecodeResult ApplyPatch(
		FileInfo sourceFile,
		FileInfo patchFile,
		FileInfo targetFile,
		BpsDecoderOptions? options = null) {
		options ??= new BpsDecoderOptions();

		sourceFile.Refresh();
		patchFile.Refresh();

		if (patchFile.Length < MinimumPatchSize) {
			throw new BpsFormatException($"Patch file too small (minimum {MinimumPatchSize} bytes)");
		}

		using var source = new FileStream(sourceFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		using var patch = new BufferedStream(
			new FileStream(patchFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
			options.BufferSize);

		// Verify header
		Span<byte> header = stackalloc byte[4];
		if (patch.Read(header) != 4 ||
			header[0] != 'B' || header[1] != 'P' || header[2] != 'S' || header[3] != '1') {
			throw new BpsFormatException("Invalid BPS header (expected 'BPS1')");
		}

		// Read sizes
		ulong sourceSize = VariableLengthInt.Decode(patch);
		if ((long)sourceSize != sourceFile.Length) {
			throw new ArgumentException($"Source size mismatch (expected {sourceSize}, got {sourceFile.Length})", nameof(sourceFile));
		}

		ulong targetSize = VariableLengthInt.Decode(patch);
		if (targetSize > int.MaxValue) {
			throw new ArgumentException($"Target size exceeds maximum ({targetSize} > {int.MaxValue})");
		}

		ulong metadataSize = VariableLengthInt.Decode(patch);

		// Read metadata
		string metadata = "";
		if (metadataSize > 0) {
			byte[] metadataBytes = ArrayPool<byte>.Shared.Rent((int)metadataSize);
			try {
				patch.ReadExactly(metadataBytes.AsSpan(0, (int)metadataSize));
				metadata = Encoding.UTF8.GetString(metadataBytes.AsSpan(0, (int)metadataSize));
			} finally {
				ArrayPool<byte>.Shared.Return(metadataBytes);
			}
		}

		// Read CRC32 values from footer
		long commandsEnd = patchFile.Length - 12;
		long patchPos = patch.Position;
		patch.Position = commandsEnd;

		Span<byte> hashBuffer = stackalloc byte[12];
		patch.ReadExactly(hashBuffer);

		uint expectedSourceCrc = BitConverter.ToUInt32(hashBuffer[0..4]);
		uint expectedTargetCrc = BitConverter.ToUInt32(hashBuffer[4..8]);
		uint expectedPatchCrc = BitConverter.ToUInt32(hashBuffer[8..12]);

		patch.Position = patchPos;

		// Allocate target buffer
		byte[] targetBuffer = ArrayPool<byte>.Shared.Rent((int)targetSize);

		try {
			using var targetStream = new MemoryStream(targetBuffer, 0, (int)targetSize, writable: true);

			// Process commands
			long sourceRelativeOffset = 0;
			long targetRelativeOffset = 0;
			long lastProgressReport = 0;

			while (patch.Position < commandsEnd) {
				// Progress reporting
				if (options.Progress != null && patch.Position - lastProgressReport > 4096) {
					options.Progress.Report(new DecodingProgress {
						Position = patch.Position - patchPos,
						Total = commandsEnd - patchPos,
						Phase = "Decoding"
					});
					lastProgressReport = patch.Position;
				}

				// Decode command
				int command = (int)VariableLengthInt.Decode(patch);
				var action = (BpsAction)(command & 3);
				int length = (command >> 2) + 1;

				switch (action) {
					case BpsAction.SourceRead:
						source.Position = targetStream.Position;
						int bytesRead = source.Read(targetBuffer.AsSpan((int)targetStream.Position, length));
						if (bytesRead != length) {
							throw new BpsFormatException("Unexpected end of source file");
						}
						targetStream.Position += length;
						break;

					case BpsAction.TargetRead:
						patch.ReadExactly(targetBuffer.AsSpan((int)targetStream.Position, length));
						targetStream.Position += length;
						break;

					case BpsAction.SourceCopy:
					case BpsAction.TargetCopy:
						int offset = (int)VariableLengthInt.Decode(patch);
						offset = ((offset & 1) != 0) ? -(offset >> 1) : (offset >> 1);

						if (action == BpsAction.SourceCopy) {
							sourceRelativeOffset += offset;
							if (sourceRelativeOffset < 0 || sourceRelativeOffset >= source.Length) {
								throw new BpsFormatException($"Invalid source offset: {sourceRelativeOffset}");
							}
							source.Position = sourceRelativeOffset;
							source.ReadExactly(targetBuffer.AsSpan((int)targetStream.Position, length));
							targetStream.Position += length;
							sourceRelativeOffset += length;
						} else // TargetCopy
						  {
							targetRelativeOffset += offset;
							if (targetRelativeOffset < 0 || targetRelativeOffset >= targetStream.Length) {
								throw new BpsFormatException($"Invalid target offset: {targetRelativeOffset}");
							}
							var srcSpan = targetBuffer.AsSpan((int)targetRelativeOffset, length);
							var dstSpan = targetBuffer.AsSpan((int)targetStream.Position, length);

							// Handle overlapping copy (RLE pattern)
							if (targetRelativeOffset < targetStream.Position &&
								targetRelativeOffset + length > targetStream.Position) {
								for (int i = 0; i < length; i++) {
									dstSpan[i] = srcSpan[i];
								}
							} else {
								srcSpan.CopyTo(dstSpan);
							}

							targetStream.Position += length;
							targetRelativeOffset += length;
						}
						break;
				}
			}

			// Write target file
			targetFile.Refresh();
			using (var targetWriter = new BufferedStream(targetFile.OpenWrite(), options.BufferSize)) {
				targetWriter.Write(targetBuffer.AsSpan(0, (int)targetSize));
				targetWriter.Flush();
			}

			targetFile.Refresh();

			// Validate
			List<string> warnings = [];

			if (!options.SkipValidation) {
				// Patch CRC
				if (Crc32Calculator.Compute(patchFile) != Crc32Calculator.ResultConstant) {
					warnings.Add("Patch file CRC32 mismatch");
				}

				// Source CRC
				if (Crc32Calculator.Compute(sourceFile) != expectedSourceCrc) {
					warnings.Add("Source file CRC32 mismatch");
				}

				// Target size
				if (targetFile.Length != (long)targetSize) {
					warnings.Add($"Target file size mismatch (expected {targetSize}, got {targetFile.Length})");
				}

				// Target CRC
				if (Crc32Calculator.Compute(targetFile) != expectedTargetCrc) {
					warnings.Add("Target file CRC32 mismatch");
				}
			}

			// Final progress
			options.Progress?.Report(new DecodingProgress {
				Position = commandsEnd - patchPos,
				Total = commandsEnd - patchPos,
				Phase = "Complete"
			});

			return new BpsDecodeResult {
				Success = warnings.Count == 0,
				Warnings = warnings,
				Metadata = metadata,
				SourceSize = (long)sourceSize,
				TargetSize = (long)targetSize
			};
		} finally {
			ArrayPool<byte>.Shared.Return(targetBuffer);
		}
	}

	/// <summary>
	/// Reads patch metadata without applying the patch.
	/// </summary>
	/// <param name="patchFile">BPS patch file.</param>
	/// <returns>Patch information including sizes and metadata.</returns>
	public static BpsDecodeResult ReadPatchInfo(FileInfo patchFile) {
		patchFile.Refresh();

		if (patchFile.Length < MinimumPatchSize) {
			throw new BpsFormatException($"Patch file too small (minimum {MinimumPatchSize} bytes)");
		}

		using var patch = new FileStream(patchFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

		// Verify header
		Span<byte> header = stackalloc byte[4];
		if (patch.Read(header) != 4 ||
			header[0] != 'B' || header[1] != 'P' || header[2] != 'S' || header[3] != '1') {
			throw new BpsFormatException("Invalid BPS header");
		}

		ulong sourceSize = VariableLengthInt.Decode(patch);
		ulong targetSize = VariableLengthInt.Decode(patch);
		ulong metadataSize = VariableLengthInt.Decode(patch);

		string metadata = "";
		if (metadataSize > 0 && metadataSize < 1024 * 1024) // Limit metadata read
		{
			byte[] metadataBytes = new byte[metadataSize];
			patch.ReadExactly(metadataBytes);
			metadata = Encoding.UTF8.GetString(metadataBytes);
		}

		return new BpsDecodeResult {
			Success = true,
			Metadata = metadata,
			SourceSize = (long)sourceSize,
			TargetSize = (long)targetSize
		};
	}
}

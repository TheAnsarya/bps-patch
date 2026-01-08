// ========================================================================================================
// BPS Encoder - Patch Creation
// ========================================================================================================
// Creates BPS (Binary Patch System) patch files by analyzing differences between source and target.
// Features adaptive algorithm selection, ArrayPool memory management, and SIMD optimization.
//
// References:
// - BPS Specification: https://github.com/blakesmith/beat/blob/master/doc/bps.txt
// - ArrayPool: https://learn.microsoft.com/en-us/dotnet/api/system.buffers.arraypool-1
// ========================================================================================================

namespace BpsPatch.Core;

/// <summary>
/// Options for BPS patch encoding.
/// </summary>
public sealed class BpsEncoderOptions {
	/// <summary>
	/// Pattern matching algorithm to use.
	/// Default: Auto (selects based on file size)
	/// </summary>
	public MatchingAlgorithm Algorithm { get; set; } = MatchingAlgorithm.Auto;

	/// <summary>
	/// Minimum match length to consider (default 4).
	/// Smaller values may produce smaller patches but increase encoding time.
	/// </summary>
	public int MinimumMatchLength { get; set; } = 4;

	/// <summary>
	/// I/O buffer size in bytes (default 80KB).
	/// </summary>
	public int BufferSize { get; set; } = 81920;

	/// <summary>
	/// Enable lazy matching for potentially smaller patches.
	/// Increases encoding time but may improve compression.
	/// </summary>
	public bool UseLazyMatching { get; set; } = false;

	/// <summary>
	/// Enable cost-based match selection for optimal compression.
	/// Considers offset encoding overhead when selecting matches.
	/// </summary>
	public bool UseCostBasedMatching { get; set; } = false;

	/// <summary>
	/// Enable RLE optimization for detecting repeated patterns in target.
	/// Improves compression for files with repeating byte sequences.
	/// </summary>
	public bool UseRleOptimization { get; set; } = true;

	/// <summary>
	/// Enable parallel processing for improved performance on multi-core systems.
	/// Currently affects suffix array construction and hash table building.
	/// </summary>
	public bool UseParallelProcessing { get; set; } = false;

	/// <summary>
	/// Maximum degree of parallelism (0 = use all cores).
	/// Only used when UseParallelProcessing is true.
	/// </summary>
	public int MaxDegreeOfParallelism { get; set; } = 0;

	/// <summary>
	/// Progress callback invoked during encoding.
	/// </summary>
	public IProgress<EncodingProgress>? Progress { get; set; }
}

/// <summary>
/// Progress information during BPS encoding.
/// </summary>
public readonly struct EncodingProgress {
	/// <summary>
	/// Current position in target file.
	/// </summary>
	public long Position { get; init; }

	/// <summary>
	/// Total size of target file.
	/// </summary>
	public long Total { get; init; }

	/// <summary>
	/// Current phase of encoding.
	/// </summary>
	public string Phase { get; init; }

	/// <summary>
	/// Progress as percentage (0-100).
	/// </summary>
	public double Percentage => Total > 0 ? (double)Position / Total * 100 : 0;
}

/// <summary>
/// Creates BPS patch files from source and target file differences.
/// </summary>
/// <remarks>
/// <para>
/// The encoder analyzes differences between source and target files and produces
/// a BPS patch file that can reconstruct the target from the source.
/// </para>
/// <para>
/// Features:
/// </para>
/// <list type="bullet">
/// <item><description>Adaptive algorithm selection (Linear/Rabin-Karp/Suffix Array)</description></item>
/// <item><description>ArrayPool memory management for reduced GC pressure</description></item>
/// <item><description>SIMD-optimized byte comparison</description></item>
/// <item><description>Buffered I/O for performance</description></item>
/// <item><description>Progress reporting support</description></item>
/// </list>
/// </remarks>
public static class BpsEncoder {
	/// <summary>
	/// Creates a BPS patch file from source and target files.
	/// </summary>
	/// <param name="sourceFile">Original file.</param>
	/// <param name="patchFile">Output patch file.</param>
	/// <param name="targetFile">Desired result file.</param>
	/// <param name="metadata">Optional metadata string (e.g., "Patch v1.0").</param>
	/// <param name="options">Encoding options (null for defaults).</param>
	/// <exception cref="ArgumentException">File exceeds maximum supported size.</exception>
	/// <exception cref="IOException">Error reading/writing files.</exception>
	public static void CreatePatch(
		FileInfo sourceFile,
		FileInfo patchFile,
		FileInfo targetFile,
		string metadata = "",
		BpsEncoderOptions? options = null) {
		options ??= new BpsEncoderOptions();

		// Validate file sizes
		sourceFile.Refresh();
		targetFile.Refresh();

		if (sourceFile.Length > int.MaxValue) {
			throw new ArgumentException($"Source file exceeds maximum size of {int.MaxValue} bytes", nameof(sourceFile));
		}

		if (targetFile.Length > int.MaxValue) {
			throw new ArgumentException($"Target file exceeds maximum size of {int.MaxValue} bytes", nameof(targetFile));
		}

		// Load files into memory using ArrayPool
		byte[] sourceBuffer = ArrayPool<byte>.Shared.Rent((int)sourceFile.Length);
		byte[] targetBuffer = ArrayPool<byte>.Shared.Rent((int)targetFile.Length);

		try {
			// Read source file
			using (var sourceStream = new FileStream(sourceFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
				sourceStream.ReadExactly(sourceBuffer.AsSpan(0, (int)sourceFile.Length));
			}

			// Read target file
			using (var targetStream = new FileStream(targetFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
				targetStream.ReadExactly(targetBuffer.AsSpan(0, (int)targetFile.Length));
			}

			ReadOnlySpan<byte> source = sourceBuffer.AsSpan(0, (int)sourceFile.Length);
			ReadOnlySpan<byte> target = targetBuffer.AsSpan(0, (int)targetFile.Length);

			// Create patch
			CreatePatchInternal(
				source,
				target,
				patchFile,
				metadata,
				options,
				sourceBuffer,
				targetBuffer);
		} finally {
			ArrayPool<byte>.Shared.Return(sourceBuffer, clearArray: false);
			ArrayPool<byte>.Shared.Return(targetBuffer, clearArray: false);
		}
	}

	/// <summary>
	/// Creates a BPS patch from in-memory source and target data.
	/// </summary>
	/// <param name="source">Source data.</param>
	/// <param name="target">Target data.</param>
	/// <param name="patchFile">Output patch file.</param>
	/// <param name="metadata">Optional metadata.</param>
	/// <param name="options">Encoding options.</param>
	public static void CreatePatch(
		ReadOnlySpan<byte> source,
		ReadOnlySpan<byte> target,
		FileInfo patchFile,
		string metadata = "",
		BpsEncoderOptions? options = null) {
		options ??= new BpsEncoderOptions();

		// Need to copy to arrays for local function access
		byte[] sourceArray = source.ToArray();
		byte[] targetArray = target.ToArray();

		CreatePatchInternal(source, target, patchFile, metadata, options, sourceArray, targetArray);
	}

	private static void CreatePatchInternal(
		ReadOnlySpan<byte> source,
		ReadOnlySpan<byte> target,
		FileInfo patchFile,
		string metadata,
		BpsEncoderOptions options,
		byte[] sourceArray,
		byte[] targetArray) {
		// Create matching strategy
		var strategy = MatchingStrategyFactory.Create(options.Algorithm, source.Length);
		strategy.Prepare(source);

		// Encoding buffer for variable-length integers (use array to allow capture in local function)
		byte[] encodeBuffer = new byte[VariableLengthInt.MaxEncodedLength];

		// Local function for writing accumulated TargetRead
		// Note: targetPos is NOT incremented here because it was already advanced
		// during the accumulation phase (one increment per byte)
		void WriteTargetReadCommand(Stream patch, ref int readLength, ref int readStart) {
			if (readLength > 0) {
				ulong command = (ulong)(((readLength - 1) << 2) + (byte)BpsAction.TargetRead);
				int cmdLen = VariableLengthInt.Encode(command, encodeBuffer);
				patch.Write(encodeBuffer, 0, cmdLen);
				patch.Write(targetArray, readStart, readLength);

				readLength = 0;
				readStart = -1;
			}
		}

		// Create patch file
		patchFile.Refresh();
		using (var patch = new BufferedStream(patchFile.OpenWrite(), options.BufferSize)) {
			// Write header
			Span<byte> header = stackalloc byte[4];
			Encoding.UTF8.GetBytes("BPS1", header);
			patch.Write(header);

			// Write sizes
			int len = VariableLengthInt.Encode((ulong)source.Length, encodeBuffer);
			patch.Write(encodeBuffer, 0, len);

			len = VariableLengthInt.Encode((ulong)target.Length, encodeBuffer);
			patch.Write(encodeBuffer, 0, len);

			len = VariableLengthInt.Encode((ulong)metadata.Length, encodeBuffer);
			patch.Write(encodeBuffer, 0, len);

			// Write metadata
			if (metadata.Length > 0) {
				byte[] metadataBytes = Encoding.UTF8.GetBytes(metadata);
				patch.Write(metadataBytes);
			}

			// Process target
			int targetReadLength = 0;
			int targetReadStart = -1;
			int targetPosition = 0;
			long lastProgressReport = 0;

			// Track relative offsets for SourceCopy/TargetCopy encoding
			// These track where the decoder's copy pointer will be
			long sourceRelativeOffset = 0;
			long targetRelativeOffset = 0;

			// Determine if using advanced matching
			bool useAdvancedMatching = options.UseCostBasedMatching || options.UseRleOptimization;

			while (targetPosition < target.Length) {
				// Report progress periodically
				if (options.Progress != null && targetPosition - lastProgressReport > 65536) {
					options.Progress.Report(new EncodingProgress {
						Position = targetPosition,
						Total = target.Length,
						Phase = "Encoding"
					});
					lastProgressReport = targetPosition;
				}

				// Find best match (use advanced options if enabled)
				var (mode, length, start) = useAdvancedMatching
					? FindNextRunWithOptions(
						source,
						target,
						targetPosition,
						strategy,
						options.MinimumMatchLength,
						options.UseCostBasedMatching,
						options.UseRleOptimization,
						sourceRelativeOffset,
						targetRelativeOffset)
					: FindNextRun(
						source,
						target,
						targetPosition,
						strategy,
						options.MinimumMatchLength);

				// Lazy matching: check if next position has a better match
				// If so, emit one literal byte now and use the better match next iteration
				if (options.UseLazyMatching && mode != BpsAction.TargetRead && targetPosition + 1 < target.Length) {
					var (nextMode, nextLength, _) = useAdvancedMatching
						? FindNextRunWithOptions(
							source,
							target,
							targetPosition + 1,
							strategy,
							options.MinimumMatchLength,
							options.UseCostBasedMatching,
							options.UseRleOptimization,
							sourceRelativeOffset,
							targetRelativeOffset)
						: FindNextRun(
							source,
							target,
							targetPosition + 1,
							strategy,
							options.MinimumMatchLength);

					// If next position has a significantly better match, emit literal and defer
					// Use threshold of length + 2 to account for the extra literal byte cost
					if (nextMode != BpsAction.TargetRead && nextLength > length + 2) {
						// Emit this byte as TargetRead, let next iteration pick up the better match
						mode = BpsAction.TargetRead;
					}
				}

				if (mode == BpsAction.TargetRead) {
					// Accumulate TargetRead bytes
					targetReadLength++;
					if (targetReadStart == -1) {
						targetReadStart = targetPosition;
					}
					targetPosition++;
				} else {
					// Write accumulated TargetRead first
					WriteTargetReadCommand(patch, ref targetReadLength, ref targetReadStart);

					// Encode command
					ulong command = (ulong)(((length - 1) << 2) + (byte)mode);
					int cmdLen = VariableLengthInt.Encode(command, encodeBuffer);
					patch.Write(encodeBuffer, 0, cmdLen);

					// Write offset for SourceCopy/TargetCopy
					if (mode == BpsAction.SourceCopy) {
						// Offset is relative to sourceRelativeOffset
						long offset = start - sourceRelativeOffset;
						bool isNegative = offset < 0;
						ulong offsetValue = ((ulong)Math.Abs(offset) << 1) + (isNegative ? 1UL : 0);
						int offLen = VariableLengthInt.Encode(offsetValue, encodeBuffer);
						patch.Write(encodeBuffer, 0, offLen);

						// Update relative offset after copy
						sourceRelativeOffset = start + length;
					} else if (mode == BpsAction.TargetCopy) {
						// Offset is relative to targetRelativeOffset
						long offset = start - targetRelativeOffset;
						bool isNegative = offset < 0;
						ulong offsetValue = ((ulong)Math.Abs(offset) << 1) + (isNegative ? 1UL : 0);
						int offLen = VariableLengthInt.Encode(offsetValue, encodeBuffer);
						patch.Write(encodeBuffer, 0, offLen);

						// Update relative offset after copy
						targetRelativeOffset = start + length;
					}

					targetPosition += length;
				}
			}

			// Write remaining TargetRead
			WriteTargetReadCommand(patch, ref targetReadLength, ref targetReadStart);

			// Write CRC32s
			byte[] sourceCrc = Crc32Calculator.ComputeBytes(source);
			byte[] targetCrc = Crc32Calculator.ComputeBytes(target);
			patch.Write(sourceCrc);
			patch.Write(targetCrc);
			patch.Flush();
		}

		// Append patch CRC32
		patchFile.Refresh();
		byte[] patchCrc = Crc32Calculator.ComputeBytes(patchFile);

		using (var patchAppend = new FileStream(patchFile.FullName, FileMode.Append, FileAccess.Write, FileShare.Read)) {
			patchAppend.Write(patchCrc);
			patchAppend.Flush();
		}

		// Final progress report
		options.Progress?.Report(new EncodingProgress {
			Position = target.Length,
			Total = target.Length,
			Phase = "Complete"
		});
	}

	/// <summary>
	/// Finds the optimal patch action for the current position.
	/// </summary>
	private static (BpsAction Mode, int Length, int Start) FindNextRun(
		ReadOnlySpan<byte> source,
		ReadOnlySpan<byte> target,
		int targetPosition,
		IMatchingStrategy strategy,
		int minimumMatchLength) {
		return FindNextRunWithOptions(source, target, targetPosition, strategy, minimumMatchLength,
			useCostBased: false, useRle: false, sourceRelOffset: 0, targetRelOffset: 0);
	}

	/// <summary>
	/// Finds the optimal patch action for the current position with advanced options.
	/// </summary>
	private static (BpsAction Mode, int Length, int Start) FindNextRunWithOptions(
		ReadOnlySpan<byte> source,
		ReadOnlySpan<byte> target,
		int targetPosition,
		IMatchingStrategy strategy,
		int minimumMatchLength,
		bool useCostBased,
		bool useRle,
		long sourceRelOffset,
		long targetRelOffset) {
		BpsAction mode = BpsAction.TargetRead;
		int longestRun = minimumMatchLength - 1;
		int longestStart = -1;
		int bestCost = int.MaxValue; // Lower is better: data_bytes + command_overhead

		ReadOnlySpan<byte> targetSlice = target[targetPosition..];

		// Check 1: SourceRead (same position in source)
		if (targetPosition < source.Length) {
			var (length, reachedEnd) = ByteComparison.CountMatching(
				source[targetPosition..],
				targetSlice);

			if (length >= minimumMatchLength) {
				int cost = useCostBased ? CalculateMatchCost(BpsAction.SourceRead, length, 0) : -length;

				if (useCostBased ? cost < bestCost : length > longestRun) {
					mode = BpsAction.SourceRead;
					longestRun = length;
					bestCost = cost;

					if (reachedEnd) {
						return (mode, longestRun, -1);
					}
				}
			}
		}

		// Check 2: SourceCopy (elsewhere in source)
		{
			var (length, start, reachedEnd) = strategy.FindBestMatch(
				source,
				targetSlice,
				minimumMatchLength);

			if (length >= minimumMatchLength) {
				long offset = start - sourceRelOffset;
				int cost = useCostBased ? CalculateMatchCost(BpsAction.SourceCopy, length, offset) : -length;

				if (useCostBased ? cost < bestCost : length > longestRun) {
					mode = BpsAction.SourceCopy;
					longestRun = length;
					longestStart = start;
					bestCost = cost;

					if (reachedEnd) {
						return (mode, longestRun, start);
					}
				}
			}
		}

		// Check 3: TargetCopy (earlier in target - RLE patterns)
		if (targetPosition > 0) {
			var (length, start, reachedEnd) = strategy.FindBestMatch(
				target[..targetPosition],
				targetSlice,
				minimumMatchLength);

			if (length >= minimumMatchLength) {
				long offset = start - targetRelOffset;
				int cost = useCostBased ? CalculateMatchCost(BpsAction.TargetCopy, length, offset) : -length;

				if (useCostBased ? cost < bestCost : length > longestRun) {
					mode = BpsAction.TargetCopy;
					longestRun = length;
					longestStart = start;
					bestCost = cost;

					if (reachedEnd) {
						return (mode, longestRun, start);
					}
				}
			}
		}

		// Check 4: RLE optimization - detect repeating byte sequences
		if (useRle && targetPosition > 0) {
			int rleLength = DetectRlePattern(target, targetPosition);
			if (rleLength >= minimumMatchLength && rleLength > longestRun) {
				// RLE can be encoded as TargetCopy from previous byte
				int rleStart = targetPosition - 1;
				long offset = rleStart - targetRelOffset;
				int cost = useCostBased ? CalculateMatchCost(BpsAction.TargetCopy, rleLength, offset) : -rleLength;

				if (useCostBased ? cost < bestCost : rleLength > longestRun) {
					mode = BpsAction.TargetCopy;
					longestRun = rleLength;
					longestStart = rleStart;
				}
			}
		}

		return (mode, longestRun, longestStart);
	}

	/// <summary>
	/// Calculates the cost of a match in terms of patch bytes.
	/// Lower cost is better.
	/// </summary>
	private static int CalculateMatchCost(BpsAction mode, int length, long offset) {
		// Cost = command_bytes + offset_bytes - data_saved
		// We want to minimize patch size, so matches that save more data are better

		// Command encoding: variable-length integer for ((length-1) << 2 | mode)
		int commandBytes = VariableLengthInt.EncodedLength((ulong)((length - 1) << 2));

		int offsetBytes = 0;
		if (mode == BpsAction.SourceCopy || mode == BpsAction.TargetCopy) {
			// Offset encoding: variable-length integer for (abs(offset) << 1 | sign)
			ulong encodedOffset = ((ulong)Math.Abs(offset) << 1) + (offset < 0 ? 1UL : 0);
			offsetBytes = VariableLengthInt.EncodedLength(encodedOffset);
		}

		// Return negative savings (so lower is better)
		// Savings = length bytes not written to patch
		// Cost = overhead bytes added
		// Net cost = overhead - savings (negative means good)
		return commandBytes + offsetBytes - length;
	}

	/// <summary>
	/// Detects RLE (Run-Length Encoding) patterns in the target.
	/// Returns the length of the repeating sequence starting at targetPosition.
	/// </summary>
	private static int DetectRlePattern(ReadOnlySpan<byte> target, int targetPosition) {
		if (targetPosition <= 0 || targetPosition >= target.Length) {
			return 0;
		}

		byte previousByte = target[targetPosition - 1];
		int length = 0;

		while (targetPosition + length < target.Length &&
			   target[targetPosition + length] == previousByte &&
			   length < 0x7FFFFFFF) // Prevent overflow
		{
			length++;
		}

		return length;
	}
}

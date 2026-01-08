// ========================================================================================================
// BPS Patch Action Enum
// ========================================================================================================
// Defines the four fundamental patch operations in the BPS (Binary Patch System) format.
// Each action describes how to reconstruct a portion of the target file from available data.
//
// References:
// - BPS Specification: https://github.com/blakesmith/beat/blob/master/doc/bps.txt
// ========================================================================================================

namespace BpsPatch.Core;

/// <summary>
/// Defines the four BPS patch operations used to reconstruct target data.
/// </summary>
/// <remarks>
/// <para>
/// Each command in a BPS patch file encodes both the action type (in the lower 2 bits)
/// and the length of the operation. The action determines where data is copied from:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="SourceRead"/>: Copy from source at same position (most efficient)</description></item>
/// <item><description><see cref="TargetRead"/>: New data embedded in patch (least efficient)</description></item>
/// <item><description><see cref="SourceCopy"/>: Copy from elsewhere in source</description></item>
/// <item><description><see cref="TargetCopy"/>: Copy from earlier in target (RLE-like)</description></item>
/// </list>
/// </remarks>
public enum BpsAction : byte {
	/// <summary>
	/// Copy bytes from the source file at the current target position.
	/// Most efficient action when source and target share data at the same location.
	/// No additional data is stored in the patch file.
	/// </summary>
	/// <example>
	/// If target[100..200] equals source[100..200], use SourceRead of length 100.
	/// </example>
	SourceRead = 0,

	/// <summary>
	/// Read new bytes directly from the patch file.
	/// Least efficient action - stores raw data in the patch.
	/// Used when target data doesn't exist in source.
	/// </summary>
	/// <example>
	/// For completely new data not found in source, embed it with TargetRead.
	/// </example>
	TargetRead = 1,

	/// <summary>
	/// Copy bytes from a different position in the source file.
	/// Includes an offset to specify the copy location.
	/// Useful when data is relocated or duplicated.
	/// </summary>
	/// <example>
	/// If target[100..200] equals source[500..600], use SourceCopy with offset.
	/// </example>
	SourceCopy = 2,

	/// <summary>
	/// Copy bytes from an earlier position in the target file.
	/// Enables RLE-like compression when target contains repeated patterns.
	/// May involve overlapping regions (copy byte-by-byte for RLE).
	/// </summary>
	/// <example>
	/// For RLE pattern [0xAA, 0xAA, 0xAA, ...], use TargetCopy overlapping itself.
	/// </example>
	TargetCopy = 3
}

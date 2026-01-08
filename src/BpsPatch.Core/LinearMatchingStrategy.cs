// ========================================================================================================
// Linear Matching Strategy
// ========================================================================================================
// Simple O(n²) linear search for pattern matching.
// Best for small files (< 64KB) where preprocessing overhead isn't worth it.
//
// Features:
// - No preprocessing required
// - Cache-friendly sequential access
// - Early termination optimization
// - SIMD-accelerated byte comparison
// ========================================================================================================

namespace BpsPatch.Core;

/// <summary>
/// Linear search pattern matching strategy.
/// </summary>
/// <remarks>
/// <para>
/// Scans through the search data sequentially, checking each position for matches.
/// Uses SIMD-accelerated comparison via <see cref="ByteComparison.CountMatching"/>.
/// </para>
/// <para>
/// Time complexity: O(n × m) average, O(n²) worst case
/// Space complexity: O(1)
/// </para>
/// </remarks>
public sealed class LinearMatchingStrategy : IMatchingStrategy {
	/// <inheritdoc/>
	public string Name => "Linear";

	/// <inheritdoc/>
	public void Prepare(ReadOnlySpan<byte> sourceData) {
		// No preprocessing required for linear search
	}

	/// <inheritdoc/>
	public (int Length, int Start, bool ReachedEnd) FindBestMatch(
		ReadOnlySpan<byte> searchData,
		ReadOnlySpan<byte> pattern,
		int minimumLength = 4) {
		// Early exit if not enough data
		if (pattern.IsEmpty || searchData.Length < minimumLength) {
			return (0, -1, false);
		}

		// Calculate search limit
		int checkUntil = searchData.Length - minimumLength;

		int longestRun = 0;
		int longestStart = -1;

		// Linear search through source for best match
		for (int currentStart = 0; currentStart <= checkUntil; currentStart++) {
			var (length, reachedEnd) = ByteComparison.CountMatching(
				searchData[currentStart..],
				pattern);

			if (length > longestRun) {
				longestRun = length;
				longestStart = currentStart;

				// Prune search space: no point checking positions that can't beat current best
				checkUntil = Math.Min(checkUntil, searchData.Length - longestRun);

				// Early exit if matched entire pattern
				if (reachedEnd) {
					return (longestRun, longestStart, true);
				}
			}
		}

		// Return best match found (or failure)
		if (longestRun >= minimumLength) {
			return (longestRun, longestStart, false);
		}

		return (0, -1, false);
	}
}

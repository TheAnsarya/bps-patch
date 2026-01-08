// ========================================================================================================
// Rabin-Karp Matching Strategy
// ========================================================================================================
// Multi-hash rolling hash algorithm for O(n) average-case pattern matching.
// Uses dual hashes to virtually eliminate false positives from hash collisions.
//
// References:
// - Rabin-Karp Algorithm: https://en.wikipedia.org/wiki/Rabin%E2%80%93Karp_algorithm
// - Karp, R.; Rabin, M. (1987). "Efficient randomized pattern-matching algorithms"
// ========================================================================================================

namespace BpsPatch.Core;

/// <summary>
/// Rabin-Karp rolling hash pattern matching strategy with dual-hash collision resistance.
/// </summary>
/// <remarks>
/// <para>
/// Uses polynomial rolling hash to quickly identify potential matches.
/// Dual hash (two independent primes) virtually eliminates false positives.
/// When hashes match, verifies with actual byte comparison for certainty.
/// </para>
/// <para>
/// Time complexity: O(n + m) average, O(n × m) worst case (extremely rare with dual hash)
/// Space complexity: O(1)
/// </para>
/// </remarks>
public sealed class RabinKarpMatchingStrategy : IMatchingStrategy {
	// Primary hash parameters (Mersenne prime for efficient modulo)
	private const ulong Prime1 = 2147483647;  // 2^31 - 1
	private const ulong Base1 = 257;          // Next prime after 256

	// Secondary hash parameters (different prime for collision resistance)
	private const ulong Prime2 = 1073741789;  // Large prime < 2^30
	private const ulong Base2 = 263;          // Different prime base

	/// <inheritdoc/>
	public string Name => "RabinKarp";

	/// <inheritdoc/>
	public void Prepare(ReadOnlySpan<byte> sourceData) {
		// No preprocessing required for Rabin-Karp
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

		// Try to find matches using rolling hash
		// Start with pattern size = minimumLength and grow
		int patternLength = Math.Min(minimumLength, pattern.Length);

		while (patternLength <= pattern.Length) {
			var result = FindMatchWithHash(searchData, pattern[..patternLength], checkUntil);

			if (result.Found) {
				// Hash match found - verify with actual byte comparison
				var (verifiedLength, reachedEnd) = ByteComparison.CountMatching(
					searchData[result.Position..],
					pattern);

				if (verifiedLength > longestRun) {
					longestRun = verifiedLength;
					longestStart = result.Position;

					// Update search limit
					checkUntil = Math.Min(checkUntil, searchData.Length - longestRun);

					// Early exit if matched entire pattern
					if (reachedEnd) {
						return (longestRun, longestStart, true);
					}
				}

				// Grow pattern length to find potentially longer matches
				patternLength = Math.Min(longestRun + 1, pattern.Length);
			} else {
				// No match found at this length - stop growing
				break;
			}
		}

		// Return best match found (or failure)
		if (longestRun >= minimumLength) {
			return (longestRun, longestStart, false);
		}

		return (0, -1, false);
	}

	/// <summary>
	/// Finds a substring match using dual rolling hash.
	/// </summary>
	private static (bool Found, int Position) FindMatchWithHash(
		ReadOnlySpan<byte> source,
		ReadOnlySpan<byte> pattern,
		int maxPosition) {
		if (pattern.Length > source.Length || maxPosition < 0) {
			return (false, -1);
		}

		// Calculate dual hash of pattern
		var (patternHash1, patternHash2) = ComputeDualHash(pattern);

		// Pre-compute BASE^(patternLength-1) % PRIME for rolling hash
		ulong basePower1 = ModPow(Base1, (ulong)(pattern.Length - 1), Prime1);
		ulong basePower2 = ModPow(Base2, (ulong)(pattern.Length - 1), Prime2);

		// Calculate initial dual hash for first window
		var (sourceHash1, sourceHash2) = ComputeDualHash(source[..pattern.Length]);

		// Check first window
		if (sourceHash1 == patternHash1 && sourceHash2 == patternHash2) {
			if (source[..pattern.Length].SequenceEqual(pattern)) {
				return (true, 0);
			}
		}

		// Roll through source using dual rolling hash
		for (int i = 1; i <= maxPosition && i + pattern.Length <= source.Length; i++) {
			// Remove leftmost byte from both hashes
			sourceHash1 = (sourceHash1 + Prime1 - (source[i - 1] * basePower1) % Prime1) % Prime1;
			sourceHash2 = (sourceHash2 + Prime2 - (source[i - 1] * basePower2) % Prime2) % Prime2;

			// Add rightmost byte to both hashes
			sourceHash1 = (sourceHash1 * Base1 + source[i + pattern.Length - 1]) % Prime1;
			sourceHash2 = (sourceHash2 * Base2 + source[i + pattern.Length - 1]) % Prime2;

			// Check if both hashes match (virtually eliminates false positives)
			if (sourceHash1 == patternHash1 && sourceHash2 == patternHash2) {
				if (source.Slice(i, pattern.Length).SequenceEqual(pattern)) {
					return (true, i);
				}
			}
		}

		return (false, -1);
	}

	/// <summary>
	/// Computes dual polynomial rolling hash for a byte sequence.
	/// Using two independent hashes virtually eliminates false positives.
	/// </summary>
	private static (ulong Hash1, ulong Hash2) ComputeDualHash(ReadOnlySpan<byte> data) {
		ulong hash1 = 0;
		ulong hash2 = 0;
		foreach (byte b in data) {
			hash1 = (hash1 * Base1 + b) % Prime1;
			hash2 = (hash2 * Base2 + b) % Prime2;
		}
		return (hash1, hash2);
	}

	/// <summary>
	/// Modular exponentiation using binary exponentiation.
	/// </summary>
	private static ulong ModPow(ulong baseValue, ulong exponent, ulong modulus) {
		if (modulus == 1) {
			return 0;
		}

		ulong result = 1;
		baseValue %= modulus;

		while (exponent > 0) {
			if ((exponent & 1) == 1) {
				result = (result * baseValue) % modulus;
			}
			exponent >>= 1;
			baseValue = (baseValue * baseValue) % modulus;
		}

		return result;
	}
}

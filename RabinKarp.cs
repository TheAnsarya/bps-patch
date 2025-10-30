using System.Numerics;

namespace bps_patch;

/// <summary>
/// Rabin-Karp rolling hash implementation for fast substring matching.
/// Uses polynomial rolling hash with modular arithmetic for O(n) average-case performance.
/// See: https://en.wikipedia.org/wiki/Rabin%E2%80%93Karp_algorithm
/// </summary>
static class RabinKarp {
	// Prime number for modular arithmetic (large prime reduces hash collisions)
	// Using 2^31 - 1 (Mersenne prime) for efficient modulo operation
	private const ulong PRIME = 2147483647;

	// Base for polynomial rolling hash (should be > 256 for byte data)
	// Using 257 (next prime after 256) for optimal distribution
	private const ulong BASE = 257;

	/// <summary>
	/// Finds the best matching substring using Rabin-Karp rolling hash algorithm.
	/// O(n) average-case performance vs O(n²) for naive linear search.
	/// </summary>
	/// <param name="source">Data to search in (haystack).</param>
	/// <param name="target">Pattern to search for (needle).</param>
	/// <param name="minimumLongestRun">Minimum match length to consider.</param>
	/// <param name="checkUntilMax">Maximum position to check (-1 for all).</param>
	/// <returns>Tuple of (match length, start position, reached end flag).</returns>
	public static (int Length, int Start, bool ReachedEnd) FindBestRun(
		ReadOnlySpan<byte> source,
		ReadOnlySpan<byte> target,
		int minimumLongestRun = 4,
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

		// Try to find matches using rolling hash
		// Start with pattern size = minimumLongestRun and grow
		int patternLength = Math.Min(minimumLongestRun, target.Length);

		while (patternLength <= target.Length) {
			var result = FindMatchWithHash(source, target[..patternLength], checkUntil);

			if (result.Found) {
				// Hash match found - verify with actual byte comparison
				(int verifiedLength, bool reachedEnd) = Encoder.CheckRun(
					source[result.Position..],
					target);

				if (verifiedLength > longestRun) {
					longestRun = verifiedLength;
					longestStart = result.Position;

					// Update search limit to prune positions that can't beat current best
					checkUntil = Math.Min(checkUntil, source.Length - longestRun);

					// Early exit if matched entire target
					if (reachedEnd) {
						return (longestRun, longestStart, true);
					}
				}

				// Grow pattern length to find potentially longer matches
				patternLength = Math.Min(longestRun + 1, target.Length);
			} else {
				// No match found at this length - stop growing
				break;
			}
		}

		// Return best match found (or failure)
		if (longestRun >= minimumLongestRun) {
			return (longestRun, longestStart, false);
		}

		return (0, -1, false);
	}

	/// <summary>
	/// Finds a substring match using rolling hash.
	/// </summary>
	/// <param name="source">Data to search in.</param>
	/// <param name="pattern">Pattern to find.</param>
	/// <param name="maxPosition">Maximum starting position to check.</param>
	/// <returns>Tuple of (found flag, position).</returns>
	private static (bool Found, int Position) FindMatchWithHash(
		ReadOnlySpan<byte> source,
		ReadOnlySpan<byte> pattern,
		int maxPosition) {

		if (pattern.Length > source.Length || maxPosition < 0) {
			return (false, -1);
		}

		// Calculate hash of pattern
		ulong patternHash = ComputeHash(pattern);

		// Pre-compute BASE^(patternLength-1) % PRIME for rolling hash
		// This is used to remove the leftmost character when rolling
		ulong basePower = ModPow(BASE, (ulong)(pattern.Length - 1), PRIME);

		// Calculate initial hash for first window of source
		ulong sourceHash = ComputeHash(source[..pattern.Length]);

		// Check first window
		if (sourceHash == patternHash) {
			// Hash match - verify with byte comparison to avoid false positives
			if (source[..pattern.Length].SequenceEqual(pattern)) {
				return (true, 0);
			}
		}

		// Roll through source using rolling hash
		for (int i = 1; i <= maxPosition && i + pattern.Length <= source.Length; i++) {
			// Remove leftmost byte from hash
			sourceHash = (sourceHash + PRIME - (source[i - 1] * basePower) % PRIME) % PRIME;

			// Add rightmost byte to hash
			sourceHash = (sourceHash * BASE + source[i + pattern.Length - 1]) % PRIME;

			// Check if hashes match
			if (sourceHash == patternHash) {
				// Hash match - verify with byte comparison
				if (source.Slice(i, pattern.Length).SequenceEqual(pattern)) {
					return (true, i);
				}
			}
		}

		return (false, -1);
	}

	/// <summary>
	/// Computes polynomial rolling hash for a byte sequence.
	/// Hash = (byte[0] * BASE^(n-1) + byte[1] * BASE^(n-2) + ... + byte[n-1]) mod PRIME
	/// </summary>
	/// <param name="data">Data to hash.</param>
	/// <returns>Hash value.</returns>
	private static ulong ComputeHash(ReadOnlySpan<byte> data) {
		ulong hash = 0;

		foreach (byte b in data) {
			hash = (hash * BASE + b) % PRIME;
		}

		return hash;
	}

	/// <summary>
	/// Modular exponentiation: computes (base^exponent) mod modulus efficiently.
	/// Uses binary exponentiation for O(log exponent) performance.
	/// See: https://en.wikipedia.org/wiki/Modular_exponentiation
	/// </summary>
	/// <param name="baseValue">Base value.</param>
	/// <param name="exponent">Exponent.</param>
	/// <param name="modulus">Modulus.</param>
	/// <returns>(base^exponent) mod modulus.</returns>
	private static ulong ModPow(ulong baseValue, ulong exponent, ulong modulus) {
		if (modulus == 1) return 0;

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

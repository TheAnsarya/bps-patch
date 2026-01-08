// ========================================================================================================
// Rabin-Karp Matching Strategy
// ========================================================================================================
// Rolling hash algorithm for O(n) average-case pattern matching.
// Good balance of performance and simplicity for medium-sized files.
//
// References:
// - Rabin-Karp Algorithm: https://en.wikipedia.org/wiki/Rabin%E2%80%93Karp_algorithm
// ========================================================================================================

namespace BpsPatch.Core;

/// <summary>
/// Rabin-Karp rolling hash pattern matching strategy.
/// </summary>
/// <remarks>
/// <para>
/// Uses polynomial rolling hash to quickly identify potential matches.
/// When hashes match, verifies with actual byte comparison to avoid false positives.
/// </para>
/// <para>
/// Time complexity: O(n + m) average, O(n × m) worst case (hash collisions)
/// Space complexity: O(1)
/// </para>
/// </remarks>
public sealed class RabinKarpMatchingStrategy : IMatchingStrategy
{
    // Prime number for modular arithmetic (Mersenne prime for efficient modulo)
    private const ulong Prime = 2147483647;  // 2^31 - 1

    // Base for polynomial hash (next prime after 256)
    private const ulong Base = 257;

    /// <inheritdoc/>
    public string Name => "RabinKarp";

    /// <inheritdoc/>
    public void Prepare(ReadOnlySpan<byte> sourceData)
    {
        // No preprocessing required for Rabin-Karp
    }

    /// <inheritdoc/>
    public (int Length, int Start, bool ReachedEnd) FindBestMatch(
        ReadOnlySpan<byte> searchData,
        ReadOnlySpan<byte> pattern,
        int minimumLength = 4)
    {
        // Early exit if not enough data
        if (pattern.IsEmpty || searchData.Length < minimumLength)
        {
            return (0, -1, false);
        }

        // Calculate search limit
        int checkUntil = searchData.Length - minimumLength;

        int longestRun = 0;
        int longestStart = -1;

        // Try to find matches using rolling hash
        // Start with pattern size = minimumLength and grow
        int patternLength = Math.Min(minimumLength, pattern.Length);

        while (patternLength <= pattern.Length)
        {
            var result = FindMatchWithHash(searchData, pattern[..patternLength], checkUntil);

            if (result.Found)
            {
                // Hash match found - verify with actual byte comparison
                var (verifiedLength, reachedEnd) = ByteComparison.CountMatching(
                    searchData[result.Position..],
                    pattern);

                if (verifiedLength > longestRun)
                {
                    longestRun = verifiedLength;
                    longestStart = result.Position;

                    // Update search limit
                    checkUntil = Math.Min(checkUntil, searchData.Length - longestRun);

                    // Early exit if matched entire pattern
                    if (reachedEnd)
                    {
                        return (longestRun, longestStart, true);
                    }
                }

                // Grow pattern length to find potentially longer matches
                patternLength = Math.Min(longestRun + 1, pattern.Length);
            }
            else
            {
                // No match found at this length - stop growing
                break;
            }
        }

        // Return best match found (or failure)
        if (longestRun >= minimumLength)
        {
            return (longestRun, longestStart, false);
        }

        return (0, -1, false);
    }

    /// <summary>
    /// Finds a substring match using rolling hash.
    /// </summary>
    private static (bool Found, int Position) FindMatchWithHash(
        ReadOnlySpan<byte> source,
        ReadOnlySpan<byte> pattern,
        int maxPosition)
    {
        if (pattern.Length > source.Length || maxPosition < 0)
        {
            return (false, -1);
        }

        // Calculate hash of pattern
        ulong patternHash = ComputeHash(pattern);

        // Pre-compute BASE^(patternLength-1) % PRIME for rolling hash
        ulong basePower = ModPow(Base, (ulong)(pattern.Length - 1), Prime);

        // Calculate initial hash for first window
        ulong sourceHash = ComputeHash(source[..pattern.Length]);

        // Check first window
        if (sourceHash == patternHash)
        {
            if (source[..pattern.Length].SequenceEqual(pattern))
            {
                return (true, 0);
            }
        }

        // Roll through source using rolling hash
        for (int i = 1; i <= maxPosition && i + pattern.Length <= source.Length; i++)
        {
            // Remove leftmost byte from hash
            sourceHash = (sourceHash + Prime - (source[i - 1] * basePower) % Prime) % Prime;

            // Add rightmost byte to hash
            sourceHash = (sourceHash * Base + source[i + pattern.Length - 1]) % Prime;

            // Check if hashes match
            if (sourceHash == patternHash)
            {
                if (source.Slice(i, pattern.Length).SequenceEqual(pattern))
                {
                    return (true, i);
                }
            }
        }

        return (false, -1);
    }

    /// <summary>
    /// Computes polynomial rolling hash for a byte sequence.
    /// </summary>
    private static ulong ComputeHash(ReadOnlySpan<byte> data)
    {
        ulong hash = 0;
        foreach (byte b in data)
        {
            hash = (hash * Base + b) % Prime;
        }
        return hash;
    }

    /// <summary>
    /// Modular exponentiation using binary exponentiation.
    /// </summary>
    private static ulong ModPow(ulong baseValue, ulong exponent, ulong modulus)
    {
        if (modulus == 1) return 0;

        ulong result = 1;
        baseValue %= modulus;

        while (exponent > 0)
        {
            if ((exponent & 1) == 1)
            {
                result = (result * baseValue) % modulus;
            }
            exponent >>= 1;
            baseValue = (baseValue * baseValue) % modulus;
        }

        return result;
    }
}

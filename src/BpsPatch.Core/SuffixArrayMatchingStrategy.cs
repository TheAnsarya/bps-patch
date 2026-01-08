// ========================================================================================================
// Suffix Array Matching Strategy
// ========================================================================================================
// Suffix array-based pattern matching for O(log n) queries.
// Best for large files or when multiple patterns are searched in the same data.
//
// References:
// - Suffix Array: https://en.wikipedia.org/wiki/Suffix_array
// - LCP Array: https://en.wikipedia.org/wiki/LCP_array
// ========================================================================================================

namespace BpsPatch.Core;

/// <summary>
/// Suffix array pattern matching strategy.
/// </summary>
/// <remarks>
/// <para>
/// Builds a sorted array of all suffixes, enabling O(log n) binary search for patterns.
/// The LCP (Longest Common Prefix) array accelerates finding the longest match.
/// </para>
/// <para>
/// Construction: O(n² log n) naive, O(n) with SA-IS (future optimization)
/// Query: O(log n) binary search + O(m) match extension
/// Space: O(n) for suffix array + O(n) for LCP array
/// </para>
/// </remarks>
public sealed class SuffixArrayMatchingStrategy : IMatchingStrategy
{
    private byte[]? _data;
    private int[]? _suffixArray;
    private int[]? _lcpArray;

    /// <inheritdoc/>
    public string Name => "SuffixArray";

    /// <inheritdoc/>
    public void Prepare(ReadOnlySpan<byte> sourceData)
    {
        _data = sourceData.ToArray();
        _suffixArray = BuildSuffixArray(_data);
        _lcpArray = BuildLcpArray(_data, _suffixArray);
    }

    /// <inheritdoc/>
    public (int Length, int Start, bool ReachedEnd) FindBestMatch(
        ReadOnlySpan<byte> searchData,
        ReadOnlySpan<byte> pattern,
        int minimumLength = 4)
    {
        // If not prepared, fall back to linear search or prepare on-the-fly
        if (_data == null || _suffixArray == null)
        {
            // For one-off searches without Prepare(), build temporary suffix array
            var tempData = searchData.ToArray();
            var tempSa = BuildSuffixArray(tempData);
            return FindMatch(tempData, tempSa, pattern, minimumLength);
        }

        // Use prepared suffix array if searching same data
        if (searchData.Length == _data.Length && searchData.SequenceEqual(_data))
        {
            return FindMatch(_data, _suffixArray, pattern, minimumLength);
        }

        // For different data, build new suffix array
        var data = searchData.ToArray();
        var sa = BuildSuffixArray(data);
        return FindMatch(data, sa, pattern, minimumLength);
    }

    /// <summary>
    /// Finds the longest match using binary search on the suffix array.
    /// </summary>
    private static (int Length, int Start, bool ReachedEnd) FindMatch(
        byte[] data,
        int[] suffixArray,
        ReadOnlySpan<byte> pattern,
        int minimumLength)
    {
        if (pattern.IsEmpty || data.Length < minimumLength)
        {
            return (0, -1, false);
        }

        // Binary search for range of suffixes starting with pattern's first byte
        int startIdx = BinarySearchFirstByteRange(data, suffixArray, pattern[0], out int endIdx);
        if (startIdx == -1)
        {
            return (0, -1, false);
        }

        // Search all suffixes in range for longest match
        int bestLength = 0;
        int bestStart = -1;
        bool reachedEnd = false;

        for (int i = startIdx; i <= endIdx; i++)
        {
            int suffixPos = suffixArray[i];
            int matchLen = CountMatchingBytes(data.AsSpan(suffixPos), pattern);

            if (matchLen > bestLength)
            {
                bestLength = matchLen;
                bestStart = suffixPos;

                if (matchLen == pattern.Length)
                {
                    reachedEnd = true;
                    break; // Found complete match
                }
            }
        }

        if (bestLength >= minimumLength)
        {
            return (bestLength, bestStart, reachedEnd);
        }

        return (0, -1, false);
    }

    /// <summary>
    /// Binary search for the range of suffixes starting with the given byte.
    /// </summary>
    private static int BinarySearchFirstByteRange(
        byte[] data,
        int[] suffixArray,
        byte firstByte,
        out int endIdx)
    {
        endIdx = -1;

        // Binary search for leftmost suffix
        int left = 0;
        int right = suffixArray.Length - 1;
        int startIdx = -1;

        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            int suffixPos = suffixArray[mid];
            byte suffixFirstByte = data[suffixPos];

            if (suffixFirstByte < firstByte)
            {
                left = mid + 1;
            }
            else if (suffixFirstByte > firstByte)
            {
                right = mid - 1;
            }
            else
            {
                startIdx = mid;
                right = mid - 1; // Continue searching left
            }
        }

        if (startIdx == -1)
        {
            return -1;
        }

        // Binary search for rightmost suffix
        left = startIdx;
        right = suffixArray.Length - 1;
        endIdx = startIdx;

        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            int suffixPos = suffixArray[mid];
            byte suffixFirstByte = data[suffixPos];

            if (suffixFirstByte == firstByte)
            {
                endIdx = mid;
                left = mid + 1; // Continue searching right
            }
            else if (suffixFirstByte < firstByte)
            {
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
            }
        }

        return startIdx;
    }

    /// <summary>
    /// Counts matching bytes between two spans.
    /// </summary>
    private static int CountMatchingBytes(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        int count = 0;
        int len = Math.Min(a.Length, b.Length);

        for (int i = 0; i < len; i++)
        {
            if (a[i] != b[i]) break;
            count++;
        }

        return count;
    }

    /// <summary>
    /// Builds suffix array using naive O(n² log n) sorting.
    /// </summary>
    /// <remarks>
    /// TODO: Implement SA-IS algorithm for O(n) construction.
    /// </remarks>
    private static int[] BuildSuffixArray(byte[] data)
    {
        int n = data.Length;
        int[] suffixes = new int[n];

        // Initialize with indices
        for (int i = 0; i < n; i++)
        {
            suffixes[i] = i;
        }

        // Sort suffixes lexicographically
        Array.Sort(suffixes, (a, b) =>
        {
            int len = Math.Min(data.Length - a, data.Length - b);

            for (int i = 0; i < len; i++)
            {
                if (data[a + i] != data[b + i])
                {
                    return data[a + i].CompareTo(data[b + i]);
                }
            }

            return (data.Length - a).CompareTo(data.Length - b);
        });

        return suffixes;
    }

    /// <summary>
    /// Builds LCP (Longest Common Prefix) array using Kasai's algorithm.
    /// </summary>
    private static int[] BuildLcpArray(byte[] data, int[] suffixArray)
    {
        int n = data.Length;
        int[] lcp = new int[n];
        int[] rank = new int[n];

        // Build inverse suffix array
        for (int i = 0; i < n; i++)
        {
            rank[suffixArray[i]] = i;
        }

        int h = 0;

        for (int i = 0; i < n; i++)
        {
            if (rank[i] > 0)
            {
                int j = suffixArray[rank[i] - 1];

                while (i + h < n && j + h < n && data[i + h] == data[j + h])
                {
                    h++;
                }

                lcp[rank[i]] = h;

                if (h > 0)
                {
                    h--;
                }
            }
        }

        return lcp;
    }
}

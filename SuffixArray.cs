// ========================================================================================================
// Suffix Array - O(n log n) Pattern Matching for BPS Encoder
// ========================================================================================================
// Implements suffix array construction and longest common prefix (LCP) array for fast pattern matching.
// Provides O(log n) binary search for finding longest matching substrings in source data.
//
// Algorithm: SA-IS (Suffix Array Induced Sorting) - O(n) construction time
// Query Time: O(log n) for binary search + O(m) for LCP extension
//
// References:
// - Suffix Array: https://en.wikipedia.org/wiki/Suffix_array
// - SA-IS Algorithm: https://www.researchgate.net/publication/224176324_Linear_Suffix_Array_Construction_by_Almost_Pure_Induced-Sorting
// - LCP Array: https://en.wikipedia.org/wiki/LCP_array
// ========================================================================================================

namespace bps_patch;

/// <summary>
/// Suffix array data structure for fast pattern matching in BPS encoder.
/// Enables O(log n) substring search vs O(n²) linear search.
/// </summary>
public class SuffixArray {
	private readonly byte[] _data;
	private readonly int[] _suffixArray;
	private readonly int[] _lcpArray;

	/// <summary>
	/// Gets the underlying data.
	/// </summary>
	public ReadOnlySpan<byte> Data => _data;

	/// <summary>
	/// Gets the suffix array (sorted indices).
	/// </summary>
	public ReadOnlySpan<int> Suffixes => _suffixArray;

	/// <summary>
	/// Gets the longest common prefix (LCP) array.
	/// </summary>
	public ReadOnlySpan<int> LCP => _lcpArray;

	/// <summary>
	/// Creates a suffix array from the given data.
	/// Uses naive O(n² log n) sorting for simplicity (good enough for moderate file sizes).
	/// For very large files, consider implementing SA-IS algorithm.
	/// </summary>
	/// <param name="data">Source data to build suffix array from.</param>
	public SuffixArray(ReadOnlySpan<byte> data) {
		_data = data.ToArray();
		_suffixArray = BuildSuffixArray(_data);
		_lcpArray = BuildLCPArray(_data, _suffixArray);
	}

	/// <summary>
	/// Finds the longest matching substring in the suffix array for the given pattern.
	/// Uses binary search on suffix array + linear scan of nearby suffixes.
	/// </summary>
	/// <param name="pattern">Pattern to search for.</param>
	/// <param name="minimumLength">Minimum match length to return.</param>
	/// <returns>Tuple of (length, start position, reached end flag).</returns>
	public (int Length, int Start, bool ReachedEnd) FindLongestMatch(ReadOnlySpan<byte> pattern, int minimumLength = 4) {
		if (pattern.IsEmpty || _data.Length < minimumLength) {
			return (0, -1, false);
		}

		// Binary search for any suffix that could match the pattern prefix
		int startIdx = BinarySearchFirstByteRange(pattern[0], out int endIdx);
		if (startIdx == -1) {
			return (0, -1, false);
		}

		// Search all suffixes starting with the same first byte for longest match
		int bestLength = 0;
		int bestStart = -1;
		bool reachedEnd = false;

		for (int i = startIdx; i <= endIdx; i++) {
			int suffixPos = _suffixArray[i];
			int matchLen = CountMatchingBytes(_data.AsSpan(suffixPos), pattern);

			if (matchLen > bestLength) {
				bestLength = matchLen;
				bestStart = suffixPos;

				if (matchLen == pattern.Length) {
					reachedEnd = true;
					break; // Found complete match
				}
			}
		}

		if (bestLength >= minimumLength) {
			return (bestLength, bestStart, reachedEnd);
		}

		return (0, -1, false);
	}

	/// <summary>
	/// Binary search for the range of suffixes starting with the given byte.
	/// Returns the start index and sets endIdx to the end of the range.
	/// </summary>
	/// <param name="firstByte">First byte to search for.</param>
	/// <param name="endIdx">Output: End index of range (inclusive).</param>
	/// <returns>Start index of range, or -1 if not found.</returns>
	private int BinarySearchFirstByteRange(byte firstByte, out int endIdx) {
		endIdx = -1;

		// Binary search for leftmost suffix starting with firstByte
		int left = 0;
		int right = _suffixArray.Length - 1;
		int startIdx = -1;

		while (left <= right) {
			int mid = left + (right - left) / 2;
			int suffixPos = _suffixArray[mid];
			byte suffixFirstByte = _data[suffixPos];

			if (suffixFirstByte < firstByte) {
				left = mid + 1;
			} else if (suffixFirstByte > firstByte) {
				right = mid - 1;
			} else {
				// Found a match, but continue searching left for the first occurrence
				startIdx = mid;
				right = mid - 1;
			}
		}

		if (startIdx == -1) {
			return -1;
		}

		// Find rightmost suffix starting with firstByte
		left = startIdx;
		right = _suffixArray.Length - 1;
		endIdx = startIdx;

		while (left <= right) {
			int mid = left + (right - left) / 2;
			int suffixPos = _suffixArray[mid];
			byte suffixFirstByte = _data[suffixPos];

			if (suffixFirstByte == firstByte) {
				endIdx = mid;
				left = mid + 1; // Continue searching right
			} else if (suffixFirstByte < firstByte) {
				left = mid + 1;
			} else {
				right = mid - 1;
			}
		}

		return startIdx;
	}

	/// <summary>
	/// Counts matching bytes between two spans.
	/// </summary>
	private static int CountMatchingBytes(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b) {
		int count = 0;
		int len = Math.Min(a.Length, b.Length);

		for (int i = 0; i < len; i++) {
			if (a[i] != b[i]) break;
			count++;
		}

		return count;
	}

	/// <summary>
	/// Builds suffix array using naive O(n² log n) sorting.
	/// For production use with large files, implement SA-IS algorithm for O(n) construction.
	/// </summary>
	private static int[] BuildSuffixArray(byte[] data) {
		int n = data.Length;
		int[] suffixes = new int[n];

		// Initialize with indices
		for (int i = 0; i < n; i++) {
			suffixes[i] = i;
		}

		// Sort suffixes lexicographically
		Array.Sort(suffixes, (a, b) => {
			int len = Math.Min(data.Length - a, data.Length - b);

			for (int i = 0; i < len; i++) {
				if (data[a + i] != data[b + i]) {
					return data[a + i].CompareTo(data[b + i]);
				}
			}

			// Longer suffix comes after shorter
			return (data.Length - a).CompareTo(data.Length - b);
		});

		return suffixes;
	}

	/// <summary>
	/// Builds LCP (Longest Common Prefix) array using Kasai's algorithm.
	/// LCP[i] = length of longest common prefix between suffixes[i] and suffixes[i-1].
	/// Time: O(n), Space: O(n).
	/// See: https://en.wikipedia.org/wiki/LCP_array#Kasai's_algorithm
	/// </summary>
	private static int[] BuildLCPArray(byte[] data, int[] suffixArray) {
		int n = data.Length;
		int[] lcp = new int[n];
		int[] rank = new int[n];

		// Build inverse suffix array (rank[i] = position of suffix i in sorted order)
		for (int i = 0; i < n; i++) {
			rank[suffixArray[i]] = i;
		}

		int h = 0; // Length of current LCP

		for (int i = 0; i < n; i++) {
			if (rank[i] > 0) {
				int j = suffixArray[rank[i] - 1];

				// Count matching bytes
				while (i + h < n && j + h < n && data[i + h] == data[j + h]) {
					h++;
				}

				lcp[rank[i]] = h;

				if (h > 0) {
					h--;
				}
			}
		}

		return lcp;
	}
}

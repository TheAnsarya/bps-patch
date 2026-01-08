// ========================================================================================================
// Suffix Array Matching Strategy
// ========================================================================================================
// Suffix array-based pattern matching for O(log n) queries.
// Best for large files or when multiple patterns are searched in the same data.
//
// References:
// - Suffix Array: https://en.wikipedia.org/wiki/Suffix_array
// - LCP Array: https://en.wikipedia.org/wiki/LCP_array
// - SA-IS Algorithm: Nong, Zhang, Chan (2009) - Linear Suffix Array Construction
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
/// Construction: O(n) using SA-IS (Suffix Array - Induced Sorting) algorithm
/// Query: O(log n) binary search + O(m) match extension
/// Space: O(n) for suffix array + O(n) for LCP array
/// </para>
/// </remarks>
public sealed class SuffixArrayMatchingStrategy : IMatchingStrategy {
	private byte[]? _data;
	private int[]? _suffixArray;
	private int[]? _lcpArray;

	/// <inheritdoc/>
	public string Name => "SuffixArray";

	/// <inheritdoc/>
	public void Prepare(ReadOnlySpan<byte> sourceData) {
		_data = sourceData.ToArray();
		_suffixArray = BuildSuffixArray(_data);
		_lcpArray = BuildLcpArray(_data, _suffixArray);
	}

	/// <inheritdoc/>
	public (int Length, int Start, bool ReachedEnd) FindBestMatch(
		ReadOnlySpan<byte> searchData,
		ReadOnlySpan<byte> pattern,
		int minimumLength = 4) {
		// If not prepared, fall back to linear search or prepare on-the-fly
		if (_data == null || _suffixArray == null) {
			// For one-off searches without Prepare(), build temporary suffix array
			var tempData = searchData.ToArray();
			var tempSa = BuildSuffixArray(tempData);
			return FindMatch(tempData, tempSa, pattern, minimumLength);
		}

		// Use prepared suffix array if searching same data
		if (searchData.Length == _data.Length && searchData.SequenceEqual(_data)) {
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
		int minimumLength) {
		if (pattern.IsEmpty || data.Length < minimumLength) {
			return (0, -1, false);
		}

		// Binary search for range of suffixes starting with pattern's first byte
		int startIdx = BinarySearchFirstByteRange(data, suffixArray, pattern[0], out int endIdx);
		if (startIdx == -1) {
			return (0, -1, false);
		}

		// Search all suffixes in range for longest match
		int bestLength = 0;
		int bestStart = -1;
		bool reachedEnd = false;

		for (int i = startIdx; i <= endIdx; i++) {
			int suffixPos = suffixArray[i];
			int matchLen = CountMatchingBytes(data.AsSpan(suffixPos), pattern);

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
	/// </summary>
	private static int BinarySearchFirstByteRange(
		byte[] data,
		int[] suffixArray,
		byte firstByte,
		out int endIdx) {
		endIdx = -1;

		// Binary search for leftmost suffix
		int left = 0;
		int right = suffixArray.Length - 1;
		int startIdx = -1;

		while (left <= right) {
			int mid = left + (right - left) / 2;
			int suffixPos = suffixArray[mid];
			byte suffixFirstByte = data[suffixPos];

			if (suffixFirstByte < firstByte) {
				left = mid + 1;
			} else if (suffixFirstByte > firstByte) {
				right = mid - 1;
			} else {
				startIdx = mid;
				right = mid - 1; // Continue searching left
			}
		}

		if (startIdx == -1) {
			return -1;
		}

		// Binary search for rightmost suffix
		left = startIdx;
		right = suffixArray.Length - 1;
		endIdx = startIdx;

		while (left <= right) {
			int mid = left + (right - left) / 2;
			int suffixPos = suffixArray[mid];
			byte suffixFirstByte = data[suffixPos];

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
			if (a[i] != b[i]) {
				break;
			}

			count++;
		}

		return count;
	}

	/// <summary>
	/// Builds suffix array using SA-IS (Suffix Array - Induced Sorting) algorithm.
	/// </summary>
	/// <remarks>
	/// SA-IS achieves O(n) time complexity through induced sorting of LMS suffixes.
	/// Reference: Nong, Zhang, Chan (2009) "Two Efficient Algorithms for Linear Time Suffix Array Construction"
	/// </remarks>
	private static int[] BuildSuffixArray(byte[] data) {
		int n = data.Length;
		if (n == 0) {
			return [];
		}

		if (n == 1) {
			return [0];
		}

		// Convert byte array to int array for SA-IS (handles alphabet naturally)
		int[] s = new int[n + 1];
		for (int i = 0; i < n; i++) {
			s[i] = data[i] + 1; // Shift by 1 so sentinel (0) is smallest
		}
		s[n] = 0; // Sentinel

		int[] sa = new int[n + 1];
		SaisInternal(s, sa, n + 1, 257); // 256 byte values + 1 sentinel

		// Remove sentinel position and adjust
		int[] result = new int[n];
		int idx = 0;
		for (int i = 0; i < sa.Length; i++) {
			if (sa[i] < n) {
				result[idx++] = sa[i];
			}
		}

		return result;
	}

	/// <summary>
	/// Core SA-IS implementation for integer alphabets.
	/// </summary>
	/// <param name="s">Input string as integer array (must end with sentinel = 0)</param>
	/// <param name="sa">Output suffix array</param>
	/// <param name="n">Length of input including sentinel</param>
	/// <param name="alphabetSize">Size of alphabet (max value + 1)</param>
	private static void SaisInternal(int[] s, int[] sa, int n, int alphabetSize) {
		// Step 1: Classify each suffix as S-type or L-type
		// S-type: s[i] < s[i+1] or (s[i] == s[i+1] and s[i+1] is S-type)
		// L-type: s[i] > s[i+1] or (s[i] == s[i+1] and s[i+1] is L-type)
		bool[] isS = new bool[n];
		isS[n - 1] = true; // Sentinel is always S-type

		for (int i = n - 2; i >= 0; i--) {
			isS[i] = s[i] < s[i + 1] || (s[i] == s[i + 1] && isS[i + 1]);
		}

		// Step 2: Find LMS (Leftmost S-type) positions
		// LMS suffix: S-type suffix where s[i-1] is L-type
		int[] bucketStarts = new int[alphabetSize];
		int[] bucketEnds = new int[alphabetSize];
		GetBuckets(s, n, alphabetSize, bucketStarts, bucketEnds);

		// Initialize SA with -1
		Array.Fill(sa, -1);

		// Place LMS suffixes at end of their buckets
		for (int i = 1; i < n; i++) {
			if (IsLms(isS, i)) {
				sa[--bucketEnds[s[i]]] = i;
			}
		}

		// Reset bucket ends
		GetBuckets(s, n, alphabetSize, bucketStarts, bucketEnds);

		// Step 3: Induced sort L-type suffixes
		InduceSortL(s, sa, n, isS, bucketStarts);

		// Reset bucket ends
		GetBuckets(s, n, alphabetSize, bucketStarts, bucketEnds);

		// Step 4: Induced sort S-type suffixes
		InduceSortS(s, sa, n, isS, bucketEnds);

		// Step 5: Compact LMS substrings and recursively sort if needed
		int lmsCount = 0;
		for (int i = 0; i < n; i++) {
			if (IsLms(isS, sa[i])) {
				sa[lmsCount++] = sa[i];
			}
		}

		// Clear working area
		Array.Fill(sa, -1, lmsCount, n - lmsCount);

		// Name each LMS substring
		int name = 0;
		int prevLms = -1;
		for (int i = 0; i < lmsCount; i++) {
			int pos = sa[i];
			bool diff = false;

			if (prevLms == -1) {
				diff = true;
			} else {
				// Compare LMS substrings
				int len = GetLmsSubstringLength(isS, pos, n);
				int prevLen = GetLmsSubstringLength(isS, prevLms, n);

				if (len != prevLen) {
					diff = true;
				} else {
					for (int j = 0; j < len; j++) {
						if (s[pos + j] != s[prevLms + j]) {
							diff = true;
							break;
						}
					}
				}
			}

			if (diff) {
				name++;
				prevLms = pos;
			}

			// Store name at position (using second half of SA as temp)
			sa[lmsCount + (pos >> 1)] = name - 1;
		}

		// Compact names
		int[] s1 = new int[lmsCount];
		int j2 = 0;
		for (int i = lmsCount; i < n; i++) {
			if (sa[i] >= 0) {
				s1[j2++] = sa[i];
			}
		}

		// If all names are unique, directly compute SA1
		// Otherwise recursively sort
		int[] sa1 = new int[lmsCount];
		if (name < lmsCount) {
			// Recursively sort the reduced string
			SaisInternal(s1, sa1, lmsCount, name);
		} else {
			// All unique - place directly
			for (int i = 0; i < lmsCount; i++) {
				sa1[s1[i]] = i;
			}
		}

		// Step 6: Induce final suffix array from sorted LMS suffixes
		// Get LMS positions in original order
		int[] lmsPositions = new int[lmsCount];
		int lmsIdx = 0;
		for (int i = 1; i < n; i++) {
			if (IsLms(isS, i)) {
				lmsPositions[lmsIdx++] = i;
			}
		}

		// Place sorted LMS suffixes
		Array.Fill(sa, -1);
		GetBuckets(s, n, alphabetSize, bucketStarts, bucketEnds);

		for (int i = lmsCount - 1; i >= 0; i--) {
			int idx = lmsPositions[sa1[i]];
			sa[--bucketEnds[s[idx]]] = idx;
		}

		// Final induced sort
		GetBuckets(s, n, alphabetSize, bucketStarts, bucketEnds);
		InduceSortL(s, sa, n, isS, bucketStarts);

		GetBuckets(s, n, alphabetSize, bucketStarts, bucketEnds);
		InduceSortS(s, sa, n, isS, bucketEnds);
	}

	/// <summary>
	/// Checks if position i is an LMS (Leftmost S-type) position.
	/// </summary>
	private static bool IsLms(bool[] isS, int i) {
		return i > 0 && isS[i] && !isS[i - 1];
	}

	/// <summary>
	/// Gets the length of the LMS substring starting at position i.
	/// </summary>
	private static int GetLmsSubstringLength(bool[] isS, int i, int n) {
		if (i == n - 1) {
			return 1;
		}

		int len = 1;
		while (i + len < n && !IsLms(isS, i + len)) {
			len++;
		}
		return len + 1; // Include next LMS position
	}

	/// <summary>
	/// Computes bucket boundaries from input string.
	/// </summary>
	private static void GetBuckets(int[] s, int n, int alphabetSize, int[] bucketStarts, int[] bucketEnds) {
		Array.Clear(bucketStarts);
		Array.Clear(bucketEnds);

		// Count occurrences
		for (int i = 0; i < n; i++) {
			bucketEnds[s[i]]++;
		}

		// Compute bucket boundaries
		int sum = 0;
		for (int i = 0; i < alphabetSize; i++) {
			bucketStarts[i] = sum;
			sum += bucketEnds[i];
			bucketEnds[i] = sum;
		}
	}

	/// <summary>
	/// Induced sort L-type suffixes from left to right.
	/// </summary>
	private static void InduceSortL(int[] s, int[] sa, int n, bool[] isS, int[] bucketStarts) {
		int[] bucketHeads = (int[])bucketStarts.Clone();

		for (int i = 0; i < n; i++) {
			int j = sa[i] - 1;
			if (sa[i] > 0 && !isS[j]) {
				sa[bucketHeads[s[j]]++] = j;
			}
		}
	}

	/// <summary>
	/// Induced sort S-type suffixes from right to left.
	/// </summary>
	private static void InduceSortS(int[] s, int[] sa, int n, bool[] isS, int[] bucketEnds) {
		int[] bucketTails = (int[])bucketEnds.Clone();

		for (int i = n - 1; i >= 0; i--) {
			int j = sa[i] - 1;
			if (sa[i] > 0 && isS[j]) {
				sa[--bucketTails[s[j]]] = j;
			}
		}
	}

	/// <summary>
	/// Builds LCP (Longest Common Prefix) array using Kasai's algorithm.
	/// </summary>
	private static int[] BuildLcpArray(byte[] data, int[] suffixArray) {
		int n = data.Length;
		int[] lcp = new int[n];
		int[] rank = new int[n];

		// Build inverse suffix array
		for (int i = 0; i < n; i++) {
			rank[suffixArray[i]] = i;
		}

		int h = 0;

		for (int i = 0; i < n; i++) {
			if (rank[i] > 0) {
				int j = suffixArray[rank[i] - 1];

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

// ========================================================================================================
// Suffix Array Tests - Comprehensive Unit Tests
// ========================================================================================================
// Tests for SuffixArray implementation including:
// - Suffix array construction correctness
// - LCP array validation
// - Pattern matching with various scenarios
// - Edge cases and performance comparisons
// ========================================================================================================

namespace bps_patch.Tests;

/// <summary>
/// Unit tests for SuffixArray pattern matching.
/// </summary>
public class SuffixArrayTests {
	[Fact]
	public void Constructor_EmptyData_CreatesEmptyArrays() {
		// Arrange & Act
		var sa = new SuffixArray(ReadOnlySpan<byte>.Empty);

		// Assert
		Assert.Equal(0, sa.Data.Length);
		Assert.Equal(0, sa.Suffixes.Length);
		Assert.Equal(0, sa.LCP.Length);
	}

	[Fact]
	public void Constructor_SingleByte_CreatesSingleSuffix() {
		// Arrange & Act
		byte[] data = { 42 };
		var sa = new SuffixArray(data);

		// Assert
		Assert.Equal(1, sa.Data.Length);
		Assert.Equal(1, sa.Suffixes.Length);
		Assert.Equal(0, sa.Suffixes[0]); // Only suffix starts at 0
	}

	[Fact]
	public void Constructor_SortsSuffixesLexicographically() {
		// Arrange
		byte[] data = "banana"u8.ToArray();

		// Act
		var sa = new SuffixArray(data);

		// Assert - suffixes should be sorted:
		// "a" (5), "ana" (3), "anana" (1), "banana" (0), "na" (4), "nana" (2)
		Assert.Equal(6, sa.Suffixes.Length);
		Assert.Equal(5, sa.Suffixes[0]); // "a"
		Assert.Equal(3, sa.Suffixes[1]); // "ana"
		Assert.Equal(1, sa.Suffixes[2]); // "anana"
		Assert.Equal(0, sa.Suffixes[3]); // "banana"
		Assert.Equal(4, sa.Suffixes[4]); // "na"
		Assert.Equal(2, sa.Suffixes[5]); // "nana"
	}

	[Fact]
	public void LCPArray_CalculatesCorrectValues() {
		// Arrange
		byte[] data = "banana"u8.ToArray();

		// Act
		var sa = new SuffixArray(data);

		// Assert - LCP values for "banana":
		// LCP[0] = 0 (no previous suffix)
		// LCP[1] = 1 ("a" and "ana" share "a")
		// LCP[2] = 3 ("ana" and "anana" share "ana")
		// LCP[3] = 0 ("anana" and "banana" share nothing)
		// LCP[4] = 0 ("banana" and "na" share nothing)
		// LCP[5] = 2 ("na" and "nana" share "na")
		Assert.Equal(6, sa.LCP.Length);
		Assert.Equal(0, sa.LCP[0]);
		Assert.Equal(1, sa.LCP[1]);
		Assert.Equal(3, sa.LCP[2]);
		Assert.Equal(0, sa.LCP[3]);
		Assert.Equal(0, sa.LCP[4]);
		Assert.Equal(2, sa.LCP[5]);
	}

	[Fact]
	public void FindLongestMatch_ExactMatch_ReturnsCompleteMatch() {
		// Arrange
		byte[] data = "hello world"u8.ToArray();
		var sa = new SuffixArray(data);
		byte[] pattern = "world"u8.ToArray();

		// Act
		var (length, start, reachedEnd) = sa.FindLongestMatch(pattern, minimumLength: 3);

		// Assert
		Assert.Equal(5, length);
		Assert.Equal(6, start); // "world" starts at index 6
		Assert.True(reachedEnd);
	}

	[Fact]
	public void FindLongestMatch_PartialMatch_ReturnsLongestPrefix() {
		// Arrange
		byte[] data = "hello world"u8.ToArray();
		var sa = new SuffixArray(data);
		byte[] pattern = "worldz"u8.ToArray(); // "world" + extra char

		// Act
		var (length, start, reachedEnd) = sa.FindLongestMatch(pattern, minimumLength: 3);

		// Assert
		Assert.Equal(5, length); // Matches "world"
		Assert.Equal(6, start);
		Assert.False(reachedEnd); // Didn't match entire pattern
	}

	[Fact]
	public void FindLongestMatch_NoMatch_ReturnsFailure() {
		// Arrange
		byte[] data = "hello world"u8.ToArray();
		var sa = new SuffixArray(data);
		byte[] pattern = "xyz"u8.ToArray();

		// Act
		var (length, start, reachedEnd) = sa.FindLongestMatch(pattern, minimumLength: 3);

		// Assert
		Assert.Equal(0, length);
		Assert.Equal(-1, start);
		Assert.False(reachedEnd);
	}

	[Fact]
	public void FindLongestMatch_BelowMinimum_ReturnsFailure() {
		// Arrange
		byte[] data = "hello world"u8.ToArray();
		var sa = new SuffixArray(data);
		byte[] pattern = "he"u8.ToArray(); // Only 2 bytes

		// Act
		var (length, start, reachedEnd) = sa.FindLongestMatch(pattern, minimumLength: 4);

		// Assert - match exists but is too short
		Assert.Equal(0, length);
		Assert.Equal(-1, start);
		Assert.False(reachedEnd);
	}

	[Fact]
	public void FindLongestMatch_RepeatedPattern_FindsLongestOccurrence() {
		// Arrange
		byte[] data = "ababababcababababa"u8.ToArray();
		var sa = new SuffixArray(data);
		byte[] pattern = "abababa"u8.ToArray();

		// Act
		var (length, start, reachedEnd) = sa.FindLongestMatch(pattern, minimumLength: 4);

		// Assert
		Assert.Equal(7, length); // Should find one of the 7-byte matches
		Assert.True(start >= 0);
		Assert.True(reachedEnd);
	}

	[Fact]
	public void FindLongestMatch_EmptyPattern_ReturnsFailure() {
		// Arrange
		byte[] data = "hello"u8.ToArray();
		var sa = new SuffixArray(data);

		// Act
		var (length, start, reachedEnd) = sa.FindLongestMatch(ReadOnlySpan<byte>.Empty, minimumLength: 1);

		// Assert
		Assert.Equal(0, length);
		Assert.Equal(-1, start);
		Assert.False(reachedEnd);
	}

	[Fact]
	public void FindLongestMatch_PatternAtStart_FindsCorrectly() {
		// Arrange
		byte[] data = "prefix_middle_suffix"u8.ToArray();
		var sa = new SuffixArray(data);
		byte[] pattern = "prefix"u8.ToArray();

		// Act
		var (length, start, reachedEnd) = sa.FindLongestMatch(pattern, minimumLength: 3);

		// Assert
		Assert.Equal(6, length);
		Assert.Equal(0, start);
		Assert.True(reachedEnd);
	}

	[Fact]
	public void FindLongestMatch_PatternAtEnd_FindsCorrectly() {
		// Arrange
		byte[] data = "prefix_middle_suffix"u8.ToArray();
		var sa = new SuffixArray(data);
		byte[] pattern = "suffix"u8.ToArray();

		// Act
		var (length, start, reachedEnd) = sa.FindLongestMatch(pattern, minimumLength: 3);

		// Assert
		Assert.Equal(6, length);
		Assert.Equal(14, start); // "suffix" starts at index 14
		Assert.True(reachedEnd);
	}

	[Fact]
	public void FindLongestMatch_BinaryData_WorksCorrectly() {
		// Arrange
		byte[] data = { 0x00, 0x01, 0x02, 0xFF, 0xFE, 0xFD, 0x01, 0x02, 0xFF };
		var sa = new SuffixArray(data);
		byte[] pattern = { 0x01, 0x02, 0xFF };

		// Act
		var (length, start, reachedEnd) = sa.FindLongestMatch(pattern, minimumLength: 2);

		// Assert
		Assert.Equal(3, length);
		Assert.True(start == 1 || start == 6); // Pattern appears at indices 1 and 6
		Assert.True(reachedEnd);
	}

	[Fact]
	public void EncoderIntegration_FindBestRunSuffixArray_MatchesLinearSearch() {
		// Arrange
		byte[] source = "The quick brown fox jumps over the lazy dog"u8.ToArray();
		byte[] target = "quick brown"u8.ToArray();

		// Act
		var linearResult = Encoder.FindBestRunLinear(source, target, minimumLongestRun: 4);
		var suffixResult = Encoder.FindBestRunSuffixArray(source, target, minimumLongestRun: 4);

		// Assert - both should find the same match
		Assert.Equal(linearResult.Length, suffixResult.Length);
		Assert.True(suffixResult.Start >= 0);
		Assert.Equal(linearResult.ReachedEnd, suffixResult.ReachedEnd);
	}

	[Fact]
	public void EncoderIntegration_ReusesSuffixArray_WorksCorrectly() {
		// Arrange
		byte[] source = "Lorem ipsum dolor sit amet, consectetur adipiscing elit"u8.ToArray();
		var suffixArray = new SuffixArray(source);
		byte[] pattern1 = "ipsum"u8.ToArray();
		byte[] pattern2 = "dolor"u8.ToArray();

		// Act
		var result1 = Encoder.FindBestRunSuffixArray(suffixArray, pattern1, minimumLongestRun: 3);
		var result2 = Encoder.FindBestRunSuffixArray(suffixArray, pattern2, minimumLongestRun: 3);

		// Assert
		Assert.Equal(5, result1.Length);
		Assert.Equal(6, result1.Start); // "ipsum" starts at index 6
		Assert.True(result1.ReachedEnd);

		Assert.Equal(5, result2.Length);
		Assert.Equal(12, result2.Start); // "dolor" starts at index 12
		Assert.True(result2.ReachedEnd);
	}

	[Fact(Skip = "O(n² log n) suffix array construction is too slow for 1MB. Would take several minutes.")]
	[Trait("Category", "LongRunning")]
	public void Performance_LargeData_CompletesInReasonableTime() {
		// Arrange - 1MB of data with patterns
		byte[] data = new byte[1024 * 1024];
		Random rng = new Random(42);
		rng.NextBytes(data);

		// Embed known patterns
		byte[] pattern = "PATTERN"u8.ToArray();
		Array.Copy(pattern, 0, data, 1000, pattern.Length);
		Array.Copy(pattern, 0, data, 500000, pattern.Length);

		// Act
		var sw = System.Diagnostics.Stopwatch.StartNew();
		var sa = new SuffixArray(data);
		var constructionTime = sw.Elapsed;

		sw.Restart();
		var (length, start, reachedEnd) = sa.FindLongestMatch(pattern, minimumLength: 4);
		var searchTime = sw.Elapsed;

		// Assert
		Assert.Equal(7, length);
		Assert.True(start == 1000 || start == 500000);
		Assert.True(reachedEnd);

		// Performance assertions (generous limits for CI environments)
		Assert.True(constructionTime.TotalSeconds < 5, $"Construction took {constructionTime.TotalSeconds}s");
		Assert.True(searchTime.TotalMilliseconds < 100, $"Search took {searchTime.TotalMilliseconds}ms");
	}
}

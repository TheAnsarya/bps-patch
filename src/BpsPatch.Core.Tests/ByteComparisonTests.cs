// ========================================================================================================
// Byte Comparison Tests
// ========================================================================================================
// Tests for SIMD-optimized byte comparison functionality.
// Timeout: 5 seconds per test (unit tests).
// ========================================================================================================

using BpsPatch.Core;

namespace BpsPatch.Core.Tests;

public class ByteComparisonTests {
	private const int TestTimeout = TestConfiguration.UnitTestTimeout;

	[Fact]
	public void CountMatching_IdenticalArrays_ReturnsFullLength() {
		byte[] data = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

		var (length, reachedEnd) = ByteComparison.CountMatching(data, data);

		Assert.Equal(10, length);
		Assert.True(reachedEnd);
	}

	[Fact]
	public void CountMatching_CompletelyDifferent_ReturnsZero() {
		byte[] a = [1, 2, 3, 4];
		byte[] b = [5, 6, 7, 8];

		var (length, reachedEnd) = ByteComparison.CountMatching(a, b);

		Assert.Equal(0, length);
		Assert.False(reachedEnd);
	}

	[Fact]
	public void CountMatching_PartialMatch_ReturnsMatchLength() {
		byte[] a = [1, 2, 3, 4, 5];
		byte[] b = [1, 2, 3, 9, 9];

		var (length, reachedEnd) = ByteComparison.CountMatching(a, b);

		Assert.Equal(3, length);
		Assert.False(reachedEnd);
	}

	[Fact]
	public void CountMatching_EmptyArrays_ReturnsZero() {
		var (length, reachedEnd) = ByteComparison.CountMatching([], []);

		Assert.Equal(0, length);
		Assert.False(reachedEnd);
	}

	[Fact]
	public void CountMatching_LargeArrays_UsesSimd() {
		// Create arrays larger than SIMD vector size
		byte[] data = new byte[1024];
		new Random(42).NextBytes(data);

		var (length, reachedEnd) = ByteComparison.CountMatching(data, data);

		Assert.Equal(1024, length);
		Assert.True(reachedEnd);
	}

	[Fact]
	public void CountMatching_DifferentLengths_ReturnsMinLength() {
		byte[] a = [1, 2, 3, 4, 5, 6, 7, 8];
		byte[] b = [1, 2, 3];

		var (length, reachedEnd) = ByteComparison.CountMatching(a, b);

		Assert.Equal(3, length);
		Assert.True(reachedEnd); // Reached end of shorter array
	}

	[Fact]
	public void CountMatchingScalar_MatchesSIMD() {
		byte[] a = new byte[256];
		byte[] b = new byte[256];
		new Random(42).NextBytes(a);
		Array.Copy(a, b, 200); // First 200 bytes match

		var simdResult = ByteComparison.CountMatching(a, b);
		var scalarResult = ByteComparison.CountMatchingScalar(a, b);

		Assert.Equal(scalarResult, simdResult);
	}

	[Fact]
	public void AreEqual_IdenticalArrays_ReturnsTrue() {
		byte[] data = [1, 2, 3, 4, 5];

		Assert.True(ByteComparison.AreEqual(data, data));
	}

	[Fact]
	public void AreEqual_DifferentArrays_ReturnsFalse() {
		byte[] a = [1, 2, 3];
		byte[] b = [1, 2, 4];

		Assert.False(ByteComparison.AreEqual(a, b));
	}

	[Fact]
	public void FindFirstDifference_IdenticalArrays_ReturnsMinusOne() {
		byte[] data = [1, 2, 3, 4, 5];

		Assert.Equal(-1, ByteComparison.FindFirstDifference(data, data));
	}

	[Fact]
	public void FindFirstDifference_DifferentArrays_ReturnsPosition() {
		byte[] a = [1, 2, 3, 4, 5];
		byte[] b = [1, 2, 9, 4, 5];

		Assert.Equal(2, ByteComparison.FindFirstDifference(a, b));
	}
}

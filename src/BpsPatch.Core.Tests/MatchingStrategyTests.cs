// ========================================================================================================
// Pattern Matching Strategy Tests
// ========================================================================================================
// Tests for all pattern matching strategies (Linear, Rabin-Karp, Suffix Array).
// ========================================================================================================

using BpsPatch.Core;
using static BpsPatch.Core.Tests.TestConfiguration;

namespace BpsPatch.Core.Tests;

public class MatchingStrategyTests {
	private const int TestTimeout = UnitTestTimeout;
	private static readonly byte[] Source = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 1, 2, 3, 4, 5];
	private static readonly byte[] Pattern = [1, 2, 3, 4];

	[Theory]
	[InlineData(typeof(LinearMatchingStrategy))]
	[InlineData(typeof(RabinKarpMatchingStrategy))]
	[InlineData(typeof(SuffixArrayMatchingStrategy))]
	public void AllStrategies_FindMatch_AtStart(Type strategyType) {
		var strategy = (IMatchingStrategy)Activator.CreateInstance(strategyType)!;
		strategy.Prepare(Source);

		var (length, start, _) = strategy.FindBestMatch(Source, Pattern, minimumLength: 4);

		Assert.True(length >= 4);
		Assert.True(start >= 0);
	}

	[Theory]
	[InlineData(typeof(LinearMatchingStrategy))]
	[InlineData(typeof(RabinKarpMatchingStrategy))]
	[InlineData(typeof(SuffixArrayMatchingStrategy))]
	public void AllStrategies_NoMatch_ReturnsNegativeStart(Type strategyType) {
		var strategy = (IMatchingStrategy)Activator.CreateInstance(strategyType)!;
		byte[] source = [1, 2, 3, 4, 5];
		byte[] pattern = [9, 9, 9, 9];
		strategy.Prepare(source);

		var (length, start, _) = strategy.FindBestMatch(source, pattern, minimumLength: 4);

		Assert.Equal(0, length);
		Assert.Equal(-1, start);
	}

	[Theory]
	[InlineData(typeof(LinearMatchingStrategy))]
	[InlineData(typeof(RabinKarpMatchingStrategy))]
	[InlineData(typeof(SuffixArrayMatchingStrategy))]
	public void AllStrategies_EmptyPattern_ReturnsNoMatch(Type strategyType) {
		var strategy = (IMatchingStrategy)Activator.CreateInstance(strategyType)!;
		strategy.Prepare(Source);

		var (length, start, _) = strategy.FindBestMatch(Source, [], minimumLength: 4);

		Assert.Equal(0, length);
		Assert.Equal(-1, start);
	}

	[Fact]
	public void MatchingStrategyFactory_Auto_SelectsBasedOnSize() {
		var small = MatchingStrategyFactory.Create(MatchingAlgorithm.Auto, 1000);
		var medium = MatchingStrategyFactory.Create(MatchingAlgorithm.Auto, 500_000);
		var large = MatchingStrategyFactory.Create(MatchingAlgorithm.Auto, 2_000_000);

		Assert.IsType<LinearMatchingStrategy>(small);
		Assert.IsType<RabinKarpMatchingStrategy>(medium);
		Assert.IsType<SuffixArrayMatchingStrategy>(large);
	}

	[Fact]
	public void MatchingStrategyFactory_Explicit_ReturnsRequestedType() {
		Assert.IsType<LinearMatchingStrategy>(
			MatchingStrategyFactory.Create(MatchingAlgorithm.Linear));
		Assert.IsType<RabinKarpMatchingStrategy>(
			MatchingStrategyFactory.Create(MatchingAlgorithm.RabinKarp));
		Assert.IsType<SuffixArrayMatchingStrategy>(
			MatchingStrategyFactory.Create(MatchingAlgorithm.SuffixArray));
	}
}

public class LinearMatchingStrategyTests {
	private const int TestTimeout = TestConfiguration.UnitTestTimeout;

	[Fact]
	public void FindBestMatch_FindsLongestMatch() {
		var strategy = new LinearMatchingStrategy();
		byte[] source = [1, 2, 3, 1, 2, 3, 4, 5];
		byte[] pattern = [1, 2, 3, 4, 5];

		var (length, start, reachedEnd) = strategy.FindBestMatch(source, pattern, minimumLength: 4);

		Assert.Equal(5, length);
		Assert.Equal(3, start); // Longer match at position 3
		Assert.True(reachedEnd);
	}

	[Fact]
	public void FindBestMatch_RespectsMinimumLength() {
		var strategy = new LinearMatchingStrategy();
		byte[] source = [1, 2, 3, 4, 5];
		byte[] pattern = [1, 2, 9]; // Only 2 bytes match

		var (length, start, _) = strategy.FindBestMatch(source, pattern, minimumLength: 4);

		Assert.Equal(0, length); // Below minimum
		Assert.Equal(-1, start);
	}
}

public class RabinKarpMatchingStrategyTests {
	private const int TestTimeout = TestConfiguration.UnitTestTimeout;

	[Fact]
	public void FindBestMatch_HandlesHashCollisions() {
		var strategy = new RabinKarpMatchingStrategy();
		byte[] source = new byte[1000];
		byte[] pattern = [1, 2, 3, 4];

		// Fill with data that might cause collisions
		new Random(42).NextBytes(source);
		Array.Copy(pattern, 0, source, 500, 4);

		var (length, start, _) = strategy.FindBestMatch(source, pattern, minimumLength: 4);

		Assert.Equal(4, length);
		Assert.Equal(500, start);
	}
}

public class SuffixArrayMatchingStrategyTests {
	private const int TestTimeout = TestConfiguration.UnitTestTimeout;

	[Fact]
	public void Prepare_BuildsSuffixArray() {
		var strategy = new SuffixArrayMatchingStrategy();
		byte[] source = [3, 1, 4, 1, 5, 9, 2, 6];

		strategy.Prepare(source);

		// After Prepare, queries should work
		var (length, start, _) = strategy.FindBestMatch(source, [1, 4, 1], minimumLength: 3);

		Assert.Equal(3, length);
		Assert.True(start >= 0);
	}

	[Fact]
	public void FindBestMatch_WithoutPrepare_StillWorks() {
		var strategy = new SuffixArrayMatchingStrategy();
		byte[] source = [1, 2, 3, 4, 5];
		byte[] pattern = [2, 3, 4];

		// Should work without explicit Prepare (builds on-the-fly)
		var (length, start, _) = strategy.FindBestMatch(source, pattern, minimumLength: 3);

		Assert.Equal(3, length);
		Assert.Equal(1, start);
	}
}

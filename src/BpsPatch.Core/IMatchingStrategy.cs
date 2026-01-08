// ========================================================================================================
// Pattern Matching Strategy Interface
// ========================================================================================================
// Defines the contract for pattern matching algorithms used in BPS encoding.
// Enables swapping algorithms based on file size or user preference.
//
// Implementations:
// - LinearMatchingStrategy: O(n²) - best for small files
// - RabinKarpMatchingStrategy: O(n) average - good for medium files
// - SuffixArrayMatchingStrategy: O(log n) query - best for large files with reuse
// ========================================================================================================

namespace BpsPatch.Core;

/// <summary>
/// Defines the contract for pattern matching strategies used in BPS encoding.
/// </summary>
/// <remarks>
/// <para>
/// Pattern matching is the core operation in BPS encoding - finding the best
/// location in source/target data where a given pattern exists. Different
/// algorithms offer different trade-offs:
/// </para>
/// <list type="table">
/// <item>
/// <term>Linear</term>
/// <description>Simple O(n²), no setup cost, best for small files</description>
/// </item>
/// <item>
/// <term>Rabin-Karp</term>
/// <description>Rolling hash O(n) average, good general purpose</description>
/// </item>
/// <item>
/// <term>Suffix Array</term>
/// <description>O(log n) queries after O(n²) setup, best for repeated queries</description>
/// </item>
/// </list>
/// </remarks>
public interface IMatchingStrategy {
	/// <summary>
	/// Gets the name of this matching strategy.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Finds the best matching run in the search data for the given pattern.
	/// </summary>
	/// <param name="searchData">Data to search within (source or target).</param>
	/// <param name="pattern">Pattern to find.</param>
	/// <param name="minimumLength">Minimum match length to consider (default 4).</param>
	/// <returns>
	/// Tuple containing:
	/// <list type="bullet">
	/// <item><description>Length: Number of matching bytes (0 if no match found)</description></item>
	/// <item><description>Start: Starting position in searchData (-1 if no match)</description></item>
	/// <item><description>ReachedEnd: True if entire pattern was matched</description></item>
	/// </list>
	/// </returns>
	(int Length, int Start, bool ReachedEnd) FindBestMatch(
		ReadOnlySpan<byte> searchData,
		ReadOnlySpan<byte> pattern,
		int minimumLength = 4);

	/// <summary>
	/// Prepares the strategy for a specific source data set.
	/// Called once before encoding begins, allows preprocessing.
	/// </summary>
	/// <param name="sourceData">Source file data to preprocess.</param>
	/// <remarks>
	/// Suffix array strategies use this to build their index.
	/// Other strategies may no-op this method.
	/// </remarks>
	void Prepare(ReadOnlySpan<byte> sourceData);
}

/// <summary>
/// Specifies which pattern matching algorithm to use.
/// </summary>
public enum MatchingAlgorithm {
	/// <summary>
	/// Automatically select algorithm based on file size.
	/// </summary>
	Auto,

	/// <summary>
	/// Linear search - O(n²) worst case, best for small files.
	/// </summary>
	Linear,

	/// <summary>
	/// Rabin-Karp rolling hash - O(n) average case.
	/// </summary>
	RabinKarp,

	/// <summary>
	/// Suffix array - O(log n) queries after O(n²) preprocessing.
	/// </summary>
	SuffixArray
}

/// <summary>
/// Factory for creating matching strategy instances.
/// </summary>
public static class MatchingStrategyFactory {
	/// <summary>
	/// Size threshold for switching from Linear to Rabin-Karp (64 KB).
	/// </summary>
	public const int LinearThreshold = 65_536;

	/// <summary>
	/// Size threshold for switching from Rabin-Karp to Suffix Array (1 MB).
	/// </summary>
	public const int RabinKarpThreshold = 1_048_576;

	/// <summary>
	/// Creates a matching strategy instance based on the specified algorithm.
	/// </summary>
	/// <param name="algorithm">Algorithm to use.</param>
	/// <param name="sourceSize">Size of source data (used for Auto selection).</param>
	/// <returns>Matching strategy instance.</returns>
	public static IMatchingStrategy Create(MatchingAlgorithm algorithm, long sourceSize = 0) {
		return algorithm switch {
			MatchingAlgorithm.Auto => CreateAuto(sourceSize),
			MatchingAlgorithm.Linear => new LinearMatchingStrategy(),
			MatchingAlgorithm.RabinKarp => new RabinKarpMatchingStrategy(),
			MatchingAlgorithm.SuffixArray => new SuffixArrayMatchingStrategy(),
			_ => new LinearMatchingStrategy()
		};
	}

	/// <summary>
	/// Creates the optimal matching strategy based on source file size.
	/// </summary>
	/// <param name="sourceSize">Size of source data in bytes.</param>
	/// <returns>Optimal matching strategy for the given size.</returns>
	private static IMatchingStrategy CreateAuto(long sourceSize) {
		if (sourceSize < LinearThreshold) {
			return new LinearMatchingStrategy();
		} else if (sourceSize < RabinKarpThreshold) {
			return new RabinKarpMatchingStrategy();
		} else {
			return new SuffixArrayMatchingStrategy();
		}
	}
}

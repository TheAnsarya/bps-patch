// ========================================================================================================
// SIMD-Optimized Byte Comparison
// ========================================================================================================
// High-performance byte sequence comparison using SIMD (Vector<byte>).
// Falls back to scalar comparison when SIMD is unavailable or for small sequences.
//
// References:
// - System.Numerics.Vector: https://learn.microsoft.com/en-us/dotnet/api/system.numerics.vector-1
// - SIMD in .NET: https://learn.microsoft.com/en-us/dotnet/standard/simd
// ========================================================================================================

namespace BpsPatch.Core;

/// <summary>
/// Provides SIMD-optimized byte sequence comparison.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="Vector{T}"/> for bulk comparison when hardware acceleration is available.
/// Provides 4-8× speedup for long matching runs (SSE: 16 bytes/op, AVX: 32 bytes/op).
/// </para>
/// </remarks>
public static class ByteComparison
{
    /// <summary>
    /// Counts consecutive matching bytes between two sequences.
    /// Uses SIMD optimization when available.
    /// </summary>
    /// <param name="source">First byte sequence.</param>
    /// <param name="target">Second byte sequence.</param>
    /// <returns>
    /// Tuple containing:
    /// <list type="bullet">
    /// <item><description>Length: Number of consecutive matching bytes</description></item>
    /// <item><description>ReachedEnd: True if entire target was matched</description></item>
    /// </list>
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (int Length, bool ReachedEnd) CountMatching(
        ReadOnlySpan<byte> source,
        ReadOnlySpan<byte> target)
    {
        if (source.IsEmpty || target.IsEmpty)
        {
            return (0, false);
        }

        int maxLength = Math.Min(source.Length, target.Length);
        int length = 0;

        // SIMD optimization: Use Vector<byte> for bulk comparison
        if (Vector.IsHardwareAccelerated && maxLength >= Vector<byte>.Count)
        {
            int vectorLength = Vector<byte>.Count;
            int maxVectorIndex = maxLength - vectorLength;

            while (length <= maxVectorIndex)
            {
                var sourceVec = new Vector<byte>(source.Slice(length, vectorLength));
                var targetVec = new Vector<byte>(target.Slice(length, vectorLength));

                if (!Vector.EqualsAll(sourceVec, targetVec))
                {
                    break; // Mismatch in this chunk
                }

                length += vectorLength;
            }
        }

        // Scalar comparison for remaining bytes
        while (length < maxLength && source[length] == target[length])
        {
            length++;
        }

        return (length, length == target.Length);
    }

    /// <summary>
    /// Scalar (non-SIMD) byte comparison for benchmarking.
    /// </summary>
    /// <param name="source">First byte sequence.</param>
    /// <param name="target">Second byte sequence.</param>
    /// <returns>Tuple of (match length, reached end flag).</returns>
    public static (int Length, bool ReachedEnd) CountMatchingScalar(
        ReadOnlySpan<byte> source,
        ReadOnlySpan<byte> target)
    {
        if (source.IsEmpty || target.IsEmpty)
        {
            return (0, false);
        }

        int maxLength = Math.Min(source.Length, target.Length);
        int length = 0;

        while (length < maxLength && source[length] == target[length])
        {
            length++;
        }

        return (length, length == target.Length);
    }

    /// <summary>
    /// Compares two byte sequences for equality.
    /// Uses SIMD optimization when available.
    /// </summary>
    /// <param name="a">First sequence.</param>
    /// <param name="b">Second sequence.</param>
    /// <returns>True if sequences are equal.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool AreEqual(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        return a.SequenceEqual(b);
    }

    /// <summary>
    /// Finds the first position where two sequences differ.
    /// Uses SIMD optimization when available.
    /// </summary>
    /// <param name="a">First sequence.</param>
    /// <param name="b">Second sequence.</param>
    /// <returns>Index of first differing byte, or -1 if equal (up to shorter length).</returns>
    public static int FindFirstDifference(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
    {
        var (length, _) = CountMatching(a, b);
        int maxLength = Math.Min(a.Length, b.Length);

        return length < maxLength ? length : -1;
    }
}

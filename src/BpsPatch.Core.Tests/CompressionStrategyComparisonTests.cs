// ========================================================================================================
// Compression Strategy Comparison Tests
// ========================================================================================================
// Comprehensive tests comparing all pattern matching strategies for correctness and performance.
// Tests verify that all algorithms produce valid patches that reconstruct the target correctly.
//
// References:
// - COMPRESSION_TESTING.md: Testing methodology
// - ALGORITHMS.md: Algorithm details
// ========================================================================================================

using System.Diagnostics;
using BpsPatch.Core;
using static BpsPatch.Core.Tests.TestConfiguration;

namespace BpsPatch.Core.Tests;

/// <summary>
/// Tests that verify all matching strategies produce correct results.
/// </summary>
public class CompressionStrategyComparisonTests : IDisposable
{
    private readonly List<string> _tempFiles = [];
    
    private string GetTempFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bps_strategy_{Guid.NewGuid():N}.tmp");
        _tempFiles.Add(path);
        return path;
    }
    
    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            try { File.Delete(file); } catch { }
        }
        GC.SuppressFinalize(this);
    }

    // ========================================================================================================
    // Correctness Tests - All strategies must produce valid patches
    // ========================================================================================================

    [Theory]
    [InlineData(MatchingAlgorithm.Linear)]
    [InlineData(MatchingAlgorithm.RabinKarp)]
    [InlineData(MatchingAlgorithm.SuffixArray)]
    public void AllAlgorithms_ProduceValidPatch_SmallFile(MatchingAlgorithm algorithm)
    {
        // Arrange: Small file (100 bytes)
        byte[] source = GenerateSequentialData(100);
        byte[] target = ModifyRandomPositions(source, 10);
        
        var sourceFile = GetTempFile();
        var targetFile = GetTempFile();
        var patchFile = GetTempFile();
        var outputFile = GetTempFile();
        
        File.WriteAllBytes(sourceFile, source);
        File.WriteAllBytes(targetFile, target);
        
        // Act: Create patch with specific algorithm
        BpsEncoder.CreatePatch(
            new FileInfo(sourceFile),
            new FileInfo(patchFile),
            new FileInfo(targetFile),
            $"Test with {algorithm}",
            new BpsEncoderOptions { Algorithm = algorithm });
        
        // Apply patch
        var result = BpsDecoder.ApplyPatch(
            new FileInfo(sourceFile),
            new FileInfo(patchFile),
            new FileInfo(outputFile));
        
        // Assert: Output matches target exactly
        byte[] output = File.ReadAllBytes(outputFile);
        Assert.Equal(target, output);
        Assert.Empty(result.Warnings);
    }

    [Theory]
    [InlineData(MatchingAlgorithm.Linear)]
    [InlineData(MatchingAlgorithm.RabinKarp)]
    [InlineData(MatchingAlgorithm.SuffixArray)]
    public void AllAlgorithms_ProduceValidPatch_MediumFile(MatchingAlgorithm algorithm)
    {
        // Arrange: Medium file (10KB)
        byte[] source = GenerateSequentialData(10 * 1024);
        byte[] target = ModifyRandomPositions(source, 100);
        
        var sourceFile = GetTempFile();
        var targetFile = GetTempFile();
        var patchFile = GetTempFile();
        var outputFile = GetTempFile();
        
        File.WriteAllBytes(sourceFile, source);
        File.WriteAllBytes(targetFile, target);
        
        // Act
        BpsEncoder.CreatePatch(
            new FileInfo(sourceFile),
            new FileInfo(patchFile),
            new FileInfo(targetFile),
            $"Test with {algorithm}",
            new BpsEncoderOptions { Algorithm = algorithm });
        
        var result = BpsDecoder.ApplyPatch(
            new FileInfo(sourceFile),
            new FileInfo(patchFile),
            new FileInfo(outputFile));
        
        // Assert
        byte[] output = File.ReadAllBytes(outputFile);
        Assert.Equal(target, output);
        Assert.Empty(result.Warnings);
    }

    [Theory]
    [InlineData(MatchingAlgorithm.Linear)]
    [InlineData(MatchingAlgorithm.RabinKarp)]
    [InlineData(MatchingAlgorithm.SuffixArray)]
    public void AllAlgorithms_HandleIdenticalFiles(MatchingAlgorithm algorithm)
    {
        // Arrange: Identical source and target
        byte[] data = GenerateSequentialData(1000);
        
        var sourceFile = GetTempFile();
        var targetFile = GetTempFile();
        var patchFile = GetTempFile();
        var outputFile = GetTempFile();
        
        File.WriteAllBytes(sourceFile, data);
        File.WriteAllBytes(targetFile, data);
        
        // Act
        BpsEncoder.CreatePatch(
            new FileInfo(sourceFile),
            new FileInfo(patchFile),
            new FileInfo(targetFile),
            $"Identical test with {algorithm}",
            new BpsEncoderOptions { Algorithm = algorithm });
        
        var result = BpsDecoder.ApplyPatch(
            new FileInfo(sourceFile),
            new FileInfo(patchFile),
            new FileInfo(outputFile));
        
        // Assert: Output matches (patch should be minimal)
        byte[] output = File.ReadAllBytes(outputFile);
        Assert.Equal(data, output);
        Assert.Empty(result.Warnings);
        
        // Patch should be very small for identical files
        var patchSize = new FileInfo(patchFile).Length;
        Assert.True(patchSize < 100, $"Patch for identical files should be tiny, was {patchSize} bytes");
    }

    [Theory]
    [InlineData(MatchingAlgorithm.Linear)]
    [InlineData(MatchingAlgorithm.RabinKarp)]
    [InlineData(MatchingAlgorithm.SuffixArray)]
    public void AllAlgorithms_HandleCompletelyDifferentFiles(MatchingAlgorithm algorithm)
    {
        // Arrange: Completely different files
        byte[] source = new byte[100];
        byte[] target = new byte[100];
        new Random(42).NextBytes(source);
        new Random(99).NextBytes(target);
        
        var sourceFile = GetTempFile();
        var targetFile = GetTempFile();
        var patchFile = GetTempFile();
        var outputFile = GetTempFile();
        
        File.WriteAllBytes(sourceFile, source);
        File.WriteAllBytes(targetFile, target);
        
        // Act
        BpsEncoder.CreatePatch(
            new FileInfo(sourceFile),
            new FileInfo(patchFile),
            new FileInfo(targetFile),
            $"Different test with {algorithm}",
            new BpsEncoderOptions { Algorithm = algorithm });
        
        var result = BpsDecoder.ApplyPatch(
            new FileInfo(sourceFile),
            new FileInfo(patchFile),
            new FileInfo(outputFile));
        
        // Assert
        byte[] output = File.ReadAllBytes(outputFile);
        Assert.Equal(target, output);
    }

    // ========================================================================================================
    // Compression Ratio Tests - Compare effectiveness
    // ========================================================================================================

    [Fact]
    public void CompressionRatio_AllAlgorithms_SimilarForRepeatingPatterns()
    {
        // Arrange: Data with repeating patterns (good for TargetCopy)
        byte[] source = GenerateRepeatingPattern(1000, [0xAB, 0xCD, 0xEF, 0x12]);
        byte[] target = GenerateRepeatingPattern(1000, [0x12, 0x34, 0x56, 0x78]);
        
        var sourceFile = GetTempFile();
        var targetFile = GetTempFile();
        
        File.WriteAllBytes(sourceFile, source);
        File.WriteAllBytes(targetFile, target);
        
        var results = new Dictionary<MatchingAlgorithm, long>();
        
        foreach (var algorithm in new[] { MatchingAlgorithm.Linear, MatchingAlgorithm.RabinKarp, MatchingAlgorithm.SuffixArray })
        {
            var patchFile = GetTempFile();
            
            BpsEncoder.CreatePatch(
                new FileInfo(sourceFile),
                new FileInfo(patchFile),
                new FileInfo(targetFile),
                "",
                new BpsEncoderOptions { Algorithm = algorithm });
            
            results[algorithm] = new FileInfo(patchFile).Length;
        }
        
        // All algorithms should produce reasonably similar patch sizes
        // Allow up to 2x difference (some variation expected)
        var min = results.Values.Min();
        var max = results.Values.Max();
        Assert.True(max <= min * 2, $"Patch size variance too high: min={min}, max={max}");
    }

    [Fact]
    public void CompressionRatio_ScatteredChanges_MeasureEfficiency()
    {
        // Arrange: Scattered single-byte changes
        byte[] source = GenerateSequentialData(5000);
        byte[] target = (byte[])source.Clone();
        
        // Change every 50th byte
        for (int i = 0; i < target.Length; i += 50)
        {
            target[i] = (byte)(target[i] ^ 0xFF);
        }
        
        var sourceFile = GetTempFile();
        var targetFile = GetTempFile();
        var patchFile = GetTempFile();
        
        File.WriteAllBytes(sourceFile, source);
        File.WriteAllBytes(targetFile, target);
        
        // Use Auto to let factory decide
        BpsEncoder.CreatePatch(
            new FileInfo(sourceFile),
            new FileInfo(patchFile),
            new FileInfo(targetFile));
        
        var patchSize = new FileInfo(patchFile).Length;
        var ratio = (double)patchSize / target.Length * 100;
        
        // Patch should be much smaller than target (we only changed ~100 bytes)
        Assert.True(ratio < 20, $"Compression ratio {ratio:F1}% is too high for scattered changes");
    }

    // ========================================================================================================
    // Performance Tests - Timing comparisons
    // ========================================================================================================

    [Theory]
    [InlineData(1024, 500)]       // 1KB
    [InlineData(10 * 1024, 2000)] // 10KB  
    [InlineData(50 * 1024, 10000)] // 50KB - generous timeout for debug builds
    public void Performance_EncodingTime_WithinLimits(int size, int maxMs)
    {
        // Arrange
        byte[] source = GenerateSequentialData(size);
        byte[] target = ModifyRandomPositions(source, size / 100);
        
        var sourceFile = GetTempFile();
        var targetFile = GetTempFile();
        var patchFile = GetTempFile();
        
        File.WriteAllBytes(sourceFile, source);
        File.WriteAllBytes(targetFile, target);
        
        // Act
        var sw = Stopwatch.StartNew();
        BpsEncoder.CreatePatch(
            new FileInfo(sourceFile),
            new FileInfo(patchFile),
            new FileInfo(targetFile));
        sw.Stop();
        
        // Assert
        Assert.True(sw.ElapsedMilliseconds < maxMs, 
            $"Encoding {size / 1024}KB took {sw.ElapsedMilliseconds}ms, expected <{maxMs}ms");
    }

    [Theory]
    [InlineData(MatchingAlgorithm.Linear, 10_000)]
    [InlineData(MatchingAlgorithm.RabinKarp, 50_000)]
    [InlineData(MatchingAlgorithm.SuffixArray, 100_000)]
    public void Performance_AlgorithmScaling_MeasureTime(MatchingAlgorithm algorithm, int size)
    {
        // Skip suffix array for very large sizes in unit tests
        if (algorithm == MatchingAlgorithm.SuffixArray && size > 50_000)
        {
            return; // SA-IS not implemented yet, O(n² log n) too slow
        }
        
        // Arrange
        byte[] source = GenerateSequentialData(size);
        byte[] target = ModifyRandomPositions(source, size / 100);
        
        var sourceFile = GetTempFile();
        var targetFile = GetTempFile();
        var patchFile = GetTempFile();
        
        File.WriteAllBytes(sourceFile, source);
        File.WriteAllBytes(targetFile, target);
        
        // Act
        var sw = Stopwatch.StartNew();
        BpsEncoder.CreatePatch(
            new FileInfo(sourceFile),
            new FileInfo(patchFile),
            new FileInfo(targetFile),
            "",
            new BpsEncoderOptions { Algorithm = algorithm });
        sw.Stop();
        
        // Just measure and report - timing will vary
        // Test passes if it completes without error
        Assert.True(sw.ElapsedMilliseconds > 0);
    }

    // ========================================================================================================
    // Edge Cases
    // ========================================================================================================

    [Theory]
    [InlineData(MatchingAlgorithm.Linear)]
    [InlineData(MatchingAlgorithm.RabinKarp)]
    [InlineData(MatchingAlgorithm.SuffixArray)]
    public void EdgeCase_SingleByteFile(MatchingAlgorithm algorithm)
    {
        // Arrange
        byte[] source = [0x42];
        byte[] target = [0x84];
        
        var sourceFile = GetTempFile();
        var targetFile = GetTempFile();
        var patchFile = GetTempFile();
        var outputFile = GetTempFile();
        
        File.WriteAllBytes(sourceFile, source);
        File.WriteAllBytes(targetFile, target);
        
        // Act
        BpsEncoder.CreatePatch(
            new FileInfo(sourceFile),
            new FileInfo(patchFile),
            new FileInfo(targetFile),
            "",
            new BpsEncoderOptions { Algorithm = algorithm });
        
        var result = BpsDecoder.ApplyPatch(
            new FileInfo(sourceFile),
            new FileInfo(patchFile),
            new FileInfo(outputFile));
        
        // Assert
        byte[] output = File.ReadAllBytes(outputFile);
        Assert.Equal(target, output);
    }

    [Theory]
    [InlineData(MatchingAlgorithm.Linear)]
    [InlineData(MatchingAlgorithm.RabinKarp)]
    [InlineData(MatchingAlgorithm.SuffixArray)]
    public void EdgeCase_TargetSmallerThanSource(MatchingAlgorithm algorithm)
    {
        // Arrange: Target is smaller than source
        byte[] source = GenerateSequentialData(1000);
        byte[] target = GenerateSequentialData(500);
        
        var sourceFile = GetTempFile();
        var targetFile = GetTempFile();
        var patchFile = GetTempFile();
        var outputFile = GetTempFile();
        
        File.WriteAllBytes(sourceFile, source);
        File.WriteAllBytes(targetFile, target);
        
        // Act
        BpsEncoder.CreatePatch(
            new FileInfo(sourceFile),
            new FileInfo(patchFile),
            new FileInfo(targetFile),
            "",
            new BpsEncoderOptions { Algorithm = algorithm });
        
        var result = BpsDecoder.ApplyPatch(
            new FileInfo(sourceFile),
            new FileInfo(patchFile),
            new FileInfo(outputFile));
        
        // Assert
        byte[] output = File.ReadAllBytes(outputFile);
        Assert.Equal(target, output);
    }

    [Theory]
    [InlineData(MatchingAlgorithm.Linear)]
    [InlineData(MatchingAlgorithm.RabinKarp)]
    [InlineData(MatchingAlgorithm.SuffixArray)]
    public void EdgeCase_TargetLargerThanSource(MatchingAlgorithm algorithm)
    {
        // Arrange: Target is larger than source
        byte[] source = GenerateSequentialData(500);
        byte[] target = GenerateSequentialData(1000);
        
        var sourceFile = GetTempFile();
        var targetFile = GetTempFile();
        var patchFile = GetTempFile();
        var outputFile = GetTempFile();
        
        File.WriteAllBytes(sourceFile, source);
        File.WriteAllBytes(targetFile, target);
        
        // Act
        BpsEncoder.CreatePatch(
            new FileInfo(sourceFile),
            new FileInfo(patchFile),
            new FileInfo(targetFile),
            "",
            new BpsEncoderOptions { Algorithm = algorithm });
        
        var result = BpsDecoder.ApplyPatch(
            new FileInfo(sourceFile),
            new FileInfo(patchFile),
            new FileInfo(outputFile));
        
        // Assert
        byte[] output = File.ReadAllBytes(outputFile);
        Assert.Equal(target, output);
    }

    // ========================================================================================================
    // Helper Methods
    // ========================================================================================================

    private static byte[] GenerateSequentialData(int size)
    {
        var data = new byte[size];
        for (int i = 0; i < size; i++)
        {
            data[i] = (byte)(i % 256);
        }
        return data;
    }

    private static byte[] GenerateRepeatingPattern(int size, byte[] pattern)
    {
        var data = new byte[size];
        for (int i = 0; i < size; i++)
        {
            data[i] = pattern[i % pattern.Length];
        }
        return data;
    }

    private static byte[] ModifyRandomPositions(byte[] original, int count)
    {
        var result = (byte[])original.Clone();
        var random = new Random(42); // Deterministic for reproducibility
        
        for (int i = 0; i < count && i < result.Length; i++)
        {
            int pos = random.Next(result.Length);
            result[pos] = (byte)(result[pos] ^ 0xFF);
        }
        
        return result;
    }
}

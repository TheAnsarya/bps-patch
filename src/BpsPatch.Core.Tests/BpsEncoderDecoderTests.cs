// ========================================================================================================
// BPS Encoder/Decoder Integration Tests
// ========================================================================================================
// End-to-end tests for patch creation and application.
// Timeout: 30 seconds per test to prevent runaway computations.
// ========================================================================================================

using BpsPatch.Core;

namespace BpsPatch.Core.Tests;

public class BpsEncoderDecoderTests : IDisposable
{
    private const int TestTimeout = TestConfiguration.IntegrationTestTimeout;
    private readonly string _tempDir;
    private readonly List<string> _tempFiles = [];

    public BpsEncoderDecoderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"bps_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            try { File.Delete(file); } catch { }
        }
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    private FileInfo CreateTempFile(byte[] data)
    {
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid()}.bin");
        File.WriteAllBytes(path, data);
        _tempFiles.Add(path);
        return new FileInfo(path);
    }

    private FileInfo GetTempPath()
    {
        var path = Path.Combine(_tempDir, $"{Guid.NewGuid()}.bin");
        _tempFiles.Add(path);
        return new FileInfo(path);
    }

    [Fact]
    public void EncodeDecode_IdenticalFiles_RoundTrips()
    {
        byte[] data = "Hello, World! This is a test."u8.ToArray();
        var source = CreateTempFile(data);
        var target = CreateTempFile(data);
        var patch = GetTempPath();
        var output = GetTempPath();

        BpsEncoder.CreatePatch(source, patch, target, "Test");
        var result = BpsDecoder.ApplyPatch(source, patch, output);

        Assert.True(result.Success);
        Assert.Equal(data, File.ReadAllBytes(output.FullName));
    }

    [Fact]
    public void EncodeDecode_DifferentFiles_RoundTrips()
    {
        byte[] sourceData = "AAAAAAAAAA"u8.ToArray();
        byte[] targetData = "BBBBBBBBBB"u8.ToArray();
        var source = CreateTempFile(sourceData);
        var target = CreateTempFile(targetData);
        var patch = GetTempPath();
        var output = GetTempPath();

        BpsEncoder.CreatePatch(source, patch, target, "");
        var result = BpsDecoder.ApplyPatch(source, patch, output);

        Assert.True(result.Success);
        Assert.Equal(targetData, File.ReadAllBytes(output.FullName));
    }

    [Fact]
    public void EncodeDecode_SmallChange_ProducesSmallPatch()
    {
        byte[] sourceData = new byte[1000];
        byte[] targetData = new byte[1000];
        Array.Fill(sourceData, (byte)'A');
        Array.Copy(sourceData, targetData, 1000);
        targetData[500] = (byte)'B'; // Single change

        var source = CreateTempFile(sourceData);
        var target = CreateTempFile(targetData);
        var patch = GetTempPath();
        var output = GetTempPath();

        BpsEncoder.CreatePatch(source, patch, target, "");

        patch.Refresh();
        Assert.True(patch.Length < 100); // Should be much smaller than full file

        var result = BpsDecoder.ApplyPatch(source, patch, output);
        Assert.True(result.Success);
        Assert.Equal(targetData, File.ReadAllBytes(output.FullName));
    }

    [Fact]
    public void EncodeDecode_WithMetadata_PreservesMetadata()
    {
        byte[] data = [1, 2, 3, 4, 5];
        var source = CreateTempFile(data);
        var target = CreateTempFile(data);
        var patch = GetTempPath();
        var output = GetTempPath();

        BpsEncoder.CreatePatch(source, patch, target, "My Patch v1.0");
        var result = BpsDecoder.ApplyPatch(source, patch, output);

        Assert.Equal("My Patch v1.0", result.Metadata);
    }

    [Fact]
    public void EncodeDecode_EmptyFiles_Works()
    {
        var source = CreateTempFile([]);
        var target = CreateTempFile([]);
        var patch = GetTempPath();
        var output = GetTempPath();

        BpsEncoder.CreatePatch(source, patch, target, "");
        var result = BpsDecoder.ApplyPatch(source, patch, output);

        Assert.True(result.Success);
        Assert.Empty(File.ReadAllBytes(output.FullName));
    }

    [Fact]
    public void EncodeDecode_RepeatedPattern_UsesTargetCopy()
    {
        byte[] sourceData = [1, 2, 3, 4, 5];
        byte[] targetData = [1, 2, 3, 1, 2, 3, 1, 2, 3]; // Repeated pattern

        var source = CreateTempFile(sourceData);
        var target = CreateTempFile(targetData);
        var patch = GetTempPath();
        var output = GetTempPath();

        BpsEncoder.CreatePatch(source, patch, target, "");
        var result = BpsDecoder.ApplyPatch(source, patch, output);

        Assert.True(result.Success);
        Assert.Equal(targetData, File.ReadAllBytes(output.FullName));
    }

    [Theory]
    [InlineData(MatchingAlgorithm.Linear)]
    [InlineData(MatchingAlgorithm.RabinKarp)]
    [InlineData(MatchingAlgorithm.SuffixArray)]
    public void EncodeDecode_AllAlgorithms_ProduceValidPatches(MatchingAlgorithm algorithm)
    {
        byte[] sourceData = new byte[100];
        byte[] targetData = new byte[100];
        new Random(42).NextBytes(sourceData);
        new Random(43).NextBytes(targetData);

        var source = CreateTempFile(sourceData);
        var target = CreateTempFile(targetData);
        var patch = GetTempPath();
        var output = GetTempPath();

        var options = new BpsEncoderOptions { Algorithm = algorithm };
        BpsEncoder.CreatePatch(source, patch, target, "", options);
        var result = BpsDecoder.ApplyPatch(source, patch, output);

        Assert.True(result.Success);
        Assert.Equal(targetData, File.ReadAllBytes(output.FullName));
    }

    [Fact]
    public void Decoder_InvalidHeader_Throws()
    {
        byte[] invalidPatch = "NOTBPS"u8.ToArray();
        var source = CreateTempFile([1, 2, 3]);
        var patch = CreateTempFile(invalidPatch);
        var output = GetTempPath();

        Assert.Throws<BpsFormatException>(() =>
            BpsDecoder.ApplyPatch(source, patch, output));
    }

    [Fact]
    public void Decoder_TruncatedPatch_Throws()
    {
        byte[] truncated = "BPS1"u8.ToArray();
        var source = CreateTempFile([1, 2, 3]);
        var patch = CreateTempFile(truncated);
        var output = GetTempPath();

        Assert.Throws<BpsFormatException>(() =>
            BpsDecoder.ApplyPatch(source, patch, output));
    }

    [Fact]
    public void ReadPatchInfo_ReturnsCorrectInfo()
    {
        byte[] sourceData = new byte[1000];
        byte[] targetData = new byte[2000];
        var source = CreateTempFile(sourceData);
        var target = CreateTempFile(targetData);
        var patch = GetTempPath();

        BpsEncoder.CreatePatch(source, patch, target, "Test Metadata");

        var info = BpsDecoder.ReadPatchInfo(patch);

        Assert.Equal(1000, info.SourceSize);
        Assert.Equal(2000, info.TargetSize);
        Assert.Equal("Test Metadata", info.Metadata);
    }

    [Fact]
    public void Crc32Calculator_ValidatePatch_ReturnsTrueForValidPatch()
    {
        byte[] data = [1, 2, 3, 4, 5];
        var source = CreateTempFile(data);
        var target = CreateTempFile(data);
        var patch = GetTempPath();

        BpsEncoder.CreatePatch(source, patch, target, "");

        Assert.True(Crc32Calculator.ValidatePatch(patch));
    }

    [Fact]
    public void EncodeDecode_WithLazyMatching_ProducesValidPatch()
    {
        // Create data with a pattern where lazy matching could help
        // A small match followed by a larger match at the next position
        byte[] sourceData = new byte[500];
        byte[] targetData = new byte[500];

        // Fill with pattern
        for (int i = 0; i < sourceData.Length; i++)
        {
            sourceData[i] = (byte)(i % 256);
        }

        // Copy source to target with modifications
        Array.Copy(sourceData, targetData, sourceData.Length);

        // Make some changes that could benefit from lazy matching
        targetData[100] = 0xFF; // Single change before a longer match
        targetData[200] = 0xFF;
        targetData[300] = 0xFF;

        var source = CreateTempFile(sourceData);
        var target = CreateTempFile(targetData);
        var patchNormal = GetTempPath();
        var patchLazy = GetTempPath();
        var output = GetTempPath();

        // Create patch without lazy matching
        BpsEncoder.CreatePatch(source, patchNormal, target, "", new BpsEncoderOptions { UseLazyMatching = false });

        // Create patch with lazy matching
        BpsEncoder.CreatePatch(source, patchLazy, target, "", new BpsEncoderOptions { UseLazyMatching = true });

        // Both should produce valid patches
        var result = BpsDecoder.ApplyPatch(source, patchLazy, output);
        Assert.True(result.Success);
        Assert.Equal(targetData, File.ReadAllBytes(output.FullName));
    }

    [Fact]
    public void EncodeDecode_LazyMatching_AllAlgorithms_ProducesValidPatch()
    {
        byte[] sourceData = new byte[256];
        byte[] targetData = new byte[256];
        new Random(42).NextBytes(sourceData);
        Array.Copy(sourceData, targetData, sourceData.Length);

        // Make scattered changes
        targetData[50] = 0xFF;
        targetData[100] = 0xFF;
        targetData[150] = 0xFF;
        targetData[200] = 0xFF;

        var source = CreateTempFile(sourceData);
        var target = CreateTempFile(targetData);

        foreach (var algorithm in new[] { MatchingAlgorithm.Linear, MatchingAlgorithm.RabinKarp, MatchingAlgorithm.SuffixArray })
        {
            var patch = GetTempPath();
            var output = GetTempPath();

            BpsEncoder.CreatePatch(source, patch, target, "", new BpsEncoderOptions
            {
                Algorithm = algorithm,
                UseLazyMatching = true
            });

            var result = BpsDecoder.ApplyPatch(source, patch, output);
            Assert.True(result.Success, $"Failed for algorithm {algorithm}");
            Assert.Equal(targetData, File.ReadAllBytes(output.FullName));
        }
    }
}

// ========================================================================================================
// CRC32 Calculator Tests
// ========================================================================================================
// Tests for CRC32 computation and validation.
// Timeout: 5 seconds per test (unit tests).
// ========================================================================================================

using BpsPatch.Core;

namespace BpsPatch.Core.Tests;

public class Crc32CalculatorTests : IDisposable
{
    private const int TestTimeout = TestConfiguration.UnitTestTimeout;
    private readonly string _tempDir;
    private readonly List<string> _tempFiles = [];

    public Crc32CalculatorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"crc32_test_{Guid.NewGuid()}");
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

    [Fact]
    public void ComputeFromSpan_EmptyData_ReturnsZero()
    {
        var crc = Crc32Calculator.Compute(ReadOnlySpan<byte>.Empty);
        Assert.Equal(0u, crc);
    }

    [Fact]
    public void ComputeFromSpan_KnownData_ReturnsExpectedCrc()
    {
        // Known CRC32 value for "123456789"
        byte[] data = "123456789"u8.ToArray();
        var crc = Crc32Calculator.Compute(data);

        // CRC32 of "123456789" is CBF43926
        Assert.Equal(0xCBF43926u, crc);
    }

    [Fact]
    public void ComputeFromFile_MatchesSpanComputation()
    {
        byte[] data = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        var file = CreateTempFile(data);

        var fileCrc = Crc32Calculator.Compute(file);
        var spanCrc = Crc32Calculator.Compute(data);

        Assert.Equal(spanCrc, fileCrc);
    }

    [Fact]
    public void ComputeFromStream_MatchesSpanComputation()
    {
        byte[] data = [100, 200, 50, 75, 125];

        uint streamCrc;
        using (var ms = new MemoryStream(data))
        {
            streamCrc = Crc32Calculator.Compute(ms);
        }

        var spanCrc = Crc32Calculator.Compute(data);
        Assert.Equal(spanCrc, streamCrc);
    }

    [Fact]
    public void ValidatePatch_ValidData_ReturnsTrue()
    {
        byte[] data = [1, 2, 3, 4];
        var crc = Crc32Calculator.Compute(data);

        // Append CRC in little-endian format
        byte[] dataWithCrc = new byte[data.Length + 4];
        Array.Copy(data, dataWithCrc, data.Length);
        BitConverter.GetBytes(crc).CopyTo(dataWithCrc, data.Length);

        // Write to file and validate
        var file = CreateTempFile(dataWithCrc);
        Assert.True(Crc32Calculator.ValidatePatch(file));
    }

    [Fact]
    public void ValidatePatch_CorruptedData_ReturnsFalse()
    {
        byte[] data = [1, 2, 3, 4];
        var crc = Crc32Calculator.Compute(data);

        byte[] dataWithCrc = new byte[data.Length + 4];
        Array.Copy(data, dataWithCrc, data.Length);
        BitConverter.GetBytes(crc).CopyTo(dataWithCrc, data.Length);

        // Corrupt one byte
        dataWithCrc[0] = 99;

        var file = CreateTempFile(dataWithCrc);
        Assert.False(Crc32Calculator.ValidatePatch(file));
    }

    [Theory]
    [InlineData(new byte[] { }, 0u)]
    [InlineData(new byte[] { 0 }, 0xD202EF8Du)]
    [InlineData(new byte[] { 0xFF }, 0xFF000000u)]
    public void ComputeFromSpan_VariousInputs_ReturnsExpectedValues(byte[] input, uint expected)
    {
        var result = Crc32Calculator.Compute(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Compute_LargeFile_DoesNotThrow()
    {
        // Test with 1MB file
        byte[] data = new byte[1024 * 1024];
        new Random(42).NextBytes(data);
        var file = CreateTempFile(data);

        var crc = Crc32Calculator.Compute(file);

        // Just verify it completes without throwing
        Assert.True(crc != 0 || data.All(b => b == 0));
    }

    [Fact]
    public void Compute_StreamPosition_NotReset()
    {
        byte[] data = [1, 2, 3, 4, 5];
        using var ms = new MemoryStream(data);
        ms.Position = 2; // Start from middle

        var crc = Crc32Calculator.Compute(ms);

        // Should compute CRC of remaining bytes [3, 4, 5]
        var expectedCrc = Crc32Calculator.Compute(data.AsSpan(2));
        Assert.Equal(expectedCrc, crc);
    }
}

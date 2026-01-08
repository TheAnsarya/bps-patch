// ========================================================================================================
// Variable-Length Integer Tests
// ========================================================================================================
// Comprehensive tests for BPS variable-length integer encoding/decoding.
// Timeout: 5 seconds per test (unit tests).
// ========================================================================================================

using BpsPatch.Core;

namespace BpsPatch.Core.Tests;

public class VariableLengthIntTests
{
    private const int TestTimeout = TestConfiguration.UnitTestTimeout;

    [Theory]
    [InlineData(0UL, new byte[] { 0x80 })]
    [InlineData(1UL, new byte[] { 0x81 })]
    [InlineData(127UL, new byte[] { 0xFF })]
    [InlineData(128UL, new byte[] { 0x00, 0x80 })]
    [InlineData(255UL, new byte[] { 0x7F, 0x80 })]
    [InlineData(300UL, new byte[] { 0x2C, 0x81 })]
    public void Encode_ProducesCorrectBytes(ulong input, byte[] expected)
    {
        var result = VariableLengthInt.Encode(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0UL)]
    [InlineData(1UL)]
    [InlineData(127UL)]
    [InlineData(128UL)]
    [InlineData(255UL)]
    [InlineData(1000UL)]
    [InlineData(10000UL)]
    [InlineData(100000UL)]
    [InlineData(ulong.MaxValue / 2)]
    public void EncodeDecodeRoundTrip_PreservesValue(ulong input)
    {
        var encoded = VariableLengthInt.Encode(input);
        var decoded = VariableLengthInt.Decode(encoded, out int bytesRead);

        Assert.Equal(input, decoded);
        Assert.Equal(encoded.Length, bytesRead);
    }

    [Fact]
    public void Encode_ToSpan_WritesCorrectLength()
    {
        Span<byte> buffer = stackalloc byte[10];
        int length = VariableLengthInt.Encode(300UL, buffer);

        Assert.Equal(2, length);
        Assert.Equal(0x2C, buffer[0]);
        Assert.Equal(0x81, buffer[1]);
    }

    [Fact]
    public void Decode_FromStream_ReadsCorrectly()
    {
        byte[] data = [0x2C, 0x81];
        using var stream = new MemoryStream(data);

        var result = VariableLengthInt.Decode(stream);

        Assert.Equal(300UL, result);
    }

    [Fact]
    public void GetEncodedLength_ReturnsCorrectLength()
    {
        Assert.Equal(1, VariableLengthInt.GetEncodedLength(0));
        Assert.Equal(1, VariableLengthInt.GetEncodedLength(127));
        Assert.Equal(2, VariableLengthInt.GetEncodedLength(128));
        Assert.Equal(2, VariableLengthInt.GetEncodedLength(300));
    }

    [Fact]
    public void Decode_ThrowsOnUnexpectedEnd()
    {
        byte[] data = [0x00]; // Continuation byte with no following byte
        using var stream = new MemoryStream(data);

        Assert.Throws<BpsFormatException>(() => VariableLengthInt.Decode(stream));
    }
}

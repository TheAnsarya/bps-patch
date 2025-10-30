using Xunit;

namespace bps_patch.Tests;

/// <summary>
/// Tests for SIMD optimizations to ensure correctness vs scalar implementation.
/// </summary>
public class SimdTests {
	[Fact]
	public void CheckRun_SIMDandScalar_ProduceIdenticalResults_EmptyData() {
		byte[] empty = [];
		byte[] data = [1, 2, 3, 4, 5];

		var simdResult = Encoder.CheckRun(empty, data);
		var scalarResult = Encoder.CheckRunScalar(empty, data);

		Assert.Equal(scalarResult.Length, simdResult.Length);
		Assert.Equal(scalarResult.ReachedEnd, simdResult.ReachedEnd);
	}

	[Fact]
	public void CheckRun_SIMDandScalar_ProduceIdenticalResults_IdenticalData() {
		byte[] data = new byte[1024];
		for (int i = 0; i < data.Length; i++) {
			data[i] = (byte)(i % 256);
		}

		var simdResult = Encoder.CheckRun(data, data);
		var scalarResult = Encoder.CheckRunScalar(data, data);

		Assert.Equal(scalarResult.Length, simdResult.Length);
		Assert.Equal(scalarResult.ReachedEnd, simdResult.ReachedEnd);
		Assert.Equal(data.Length, simdResult.Length);
		Assert.True(simdResult.ReachedEnd);
	}

	[Fact]
	public void CheckRun_SIMDandScalar_ProduceIdenticalResults_MismatchAtStart() {
		byte[] source = new byte[1024];
		byte[] target = new byte[1024];
		for (int i = 0; i < 1024; i++) {
			source[i] = (byte)(i % 256);
			target[i] = (byte)(i % 256);
		}
		target[0] = 0xFF; // Mismatch at position 0

		var simdResult = Encoder.CheckRun(source, target);
		var scalarResult = Encoder.CheckRunScalar(source, target);

		Assert.Equal(scalarResult.Length, simdResult.Length);
		Assert.Equal(scalarResult.ReachedEnd, simdResult.ReachedEnd);
		Assert.Equal(0, simdResult.Length);
		Assert.False(simdResult.ReachedEnd);
	}

	[Fact]
	public void CheckRun_SIMDandScalar_ProduceIdenticalResults_MismatchInMiddle() {
		byte[] source = new byte[1024];
		byte[] target = new byte[1024];
		for (int i = 0; i < 1024; i++) {
			source[i] = (byte)(i % 256);
			target[i] = (byte)(i % 256);
		}
		target[512] = 0xFF; // Mismatch at position 512

		var simdResult = Encoder.CheckRun(source, target);
		var scalarResult = Encoder.CheckRunScalar(source, target);

		Assert.Equal(scalarResult.Length, simdResult.Length);
		Assert.Equal(scalarResult.ReachedEnd, simdResult.ReachedEnd);
		Assert.Equal(512, simdResult.Length);
		Assert.False(simdResult.ReachedEnd);
	}

	[Fact]
	public void CheckRun_SIMDandScalar_ProduceIdenticalResults_MismatchAtEnd() {
		byte[] source = new byte[1024];
		byte[] target = new byte[1024];
		for (int i = 0; i < 1024; i++) {
			source[i] = (byte)(i % 256);
			target[i] = (byte)(i % 256);
		}
		target[1023] ^= 1; // Flip bit to create mismatch (1023 % 256 = 255, so becomes 254)

		var simdResult = Encoder.CheckRun(source, target);
		var scalarResult = Encoder.CheckRunScalar(source, target);

		Assert.Equal(scalarResult.Length, simdResult.Length);
		Assert.Equal(scalarResult.ReachedEnd, simdResult.ReachedEnd);
		Assert.Equal(1023, simdResult.Length);
		Assert.False(simdResult.ReachedEnd);
	}

	[Fact]
	public void CheckRun_SIMDandScalar_ProduceIdenticalResults_LargeFile() {
		// Test with 1MB of data
		byte[] source = new byte[1024 * 1024];
		byte[] target = new byte[1024 * 1024];
		var random = new Random(42);
		random.NextBytes(source);
		Array.Copy(source, target, source.Length);
		target[500_000] = (byte)(target[500_000] ^ 0xFF); // Mismatch at 500KB

		var simdResult = Encoder.CheckRun(source, target);
		var scalarResult = Encoder.CheckRunScalar(source, target);

		Assert.Equal(scalarResult.Length, simdResult.Length);
		Assert.Equal(scalarResult.ReachedEnd, simdResult.ReachedEnd);
		Assert.Equal(500_000, simdResult.Length);
		Assert.False(simdResult.ReachedEnd);
	}

	[Fact]
	public void CheckRun_SIMDandScalar_ProduceIdenticalResults_VectorBoundaries() {
		// Test with data sizes that cross Vector<byte>.Count boundaries
		for (int size = 1; size <= 128; size++) {
			byte[] data = new byte[size];
			for (int i = 0; i < size; i++) {
				data[i] = (byte)(i % 256);
			}

			var simdResult = Encoder.CheckRun(data, data);
			var scalarResult = Encoder.CheckRunScalar(data, data);

			Assert.Equal(scalarResult.Length, simdResult.Length);
			Assert.Equal(scalarResult.ReachedEnd, simdResult.ReachedEnd);
		}
	}

	[Theory]
	[InlineData(16)]   // Typical Vector<byte>.Count on x86
	[InlineData(32)]   // Typical Vector<byte>.Count on AVX2
	[InlineData(64)]   // Future AVX-512
	[InlineData(100)]  // Non-aligned size
	[InlineData(1000)] // Larger non-aligned size
	public void CheckRun_SIMDandScalar_ProduceIdenticalResults_VariousSizes(int size) {
		byte[] source = new byte[size];
		byte[] target = new byte[size];
		var random = new Random(42);
		random.NextBytes(source);
		Array.Copy(source, target, size);

		var simdResult = Encoder.CheckRun(source, target);
		var scalarResult = Encoder.CheckRunScalar(source, target);

		Assert.Equal(scalarResult.Length, simdResult.Length);
		Assert.Equal(scalarResult.ReachedEnd, simdResult.ReachedEnd);
		Assert.Equal(size, simdResult.Length);
		Assert.True(simdResult.ReachedEnd);
	}
}

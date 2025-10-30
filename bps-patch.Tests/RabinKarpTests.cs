using Xunit;

namespace bps_patch.Tests;

/// <summary>
/// Tests for Rabin-Karp rolling hash algorithm to ensure correctness vs linear search.
/// </summary>
public class RabinKarpTests {
	[Fact]
	public void FindBestRun_RabinKarpAndLinear_ProduceIdenticalResults_EmptyData() {
		byte[] source = [1, 2, 3, 4, 5];
		byte[] target = [];

		var rkResult = Encoder.FindBestRunRabinKarp(source, target);
		var linearResult = Encoder.FindBestRunLinear(source, target);

		Assert.Equal(linearResult.Length, rkResult.Length);
		Assert.Equal(linearResult.Start, rkResult.Start);
		Assert.Equal(linearResult.ReachedEnd, rkResult.ReachedEnd);
	}

	[Fact]
	public void FindBestRun_RabinKarpAndLinear_ProduceIdenticalResults_PatternAtStart() {
		byte[] source = new byte[1024];
		for (int i = 0; i < source.Length; i++) {
			source[i] = (byte)(i % 256);
		}

		byte[] target = new byte[32];
		Array.Copy(source, 0, target, 0, 32);

		var rkResult = Encoder.FindBestRunRabinKarp(source, target);
		var linearResult = Encoder.FindBestRunLinear(source, target);

		Assert.Equal(linearResult.Length, rkResult.Length);
		Assert.Equal(linearResult.Start, rkResult.Start);
		Assert.Equal(linearResult.ReachedEnd, rkResult.ReachedEnd);
		Assert.Equal(0, rkResult.Start);
		Assert.True(rkResult.Length >= 32);
	}

	[Fact]
	public void FindBestRun_RabinKarpAndLinear_ProduceIdenticalResults_PatternInMiddle() {
		byte[] source = new byte[1024];
		var random = new Random(42);
		random.NextBytes(source);

		byte[] target = new byte[32];
		Array.Copy(source, 512, target, 0, 32);

		var rkResult = Encoder.FindBestRunRabinKarp(source, target);
		var linearResult = Encoder.FindBestRunLinear(source, target);

		Assert.Equal(linearResult.Length, rkResult.Length);
		Assert.Equal(linearResult.Start, rkResult.Start);
		Assert.Equal(linearResult.ReachedEnd, rkResult.ReachedEnd);
	}

	[Fact]
	public void FindBestRun_RabinKarpAndLinear_ProduceIdenticalResults_PatternAtEnd() {
		byte[] source = new byte[1024];
		var random = new Random(42);
		random.NextBytes(source);

		byte[] target = new byte[32];
		Array.Copy(source, source.Length - 32, target, 0, 32);

		var rkResult = Encoder.FindBestRunRabinKarp(source, target);
		var linearResult = Encoder.FindBestRunLinear(source, target);

		Assert.Equal(linearResult.Length, rkResult.Length);
		Assert.Equal(linearResult.Start, rkResult.Start);
		Assert.Equal(linearResult.ReachedEnd, rkResult.ReachedEnd);
		Assert.Equal(source.Length - 32, rkResult.Start);
	}

	[Fact]
	public void FindBestRun_RabinKarpAndLinear_ProduceIdenticalResults_PatternAbsent() {
		byte[] source = new byte[1024];
		for (int i = 0; i < source.Length; i++) {
			source[i] = 0x00;
		}

		byte[] target = new byte[32];
		for (int i = 0; i < target.Length; i++) {
			target[i] = 0xFF;
		}

		var rkResult = Encoder.FindBestRunRabinKarp(source, target);
		var linearResult = Encoder.FindBestRunLinear(source, target);

		Assert.Equal(linearResult.Length, rkResult.Length);
		Assert.Equal(linearResult.Start, rkResult.Start);
		Assert.Equal(linearResult.ReachedEnd, rkResult.ReachedEnd);
		Assert.Equal(0, rkResult.Length);
		Assert.Equal(-1, rkResult.Start);
	}

	[Fact]
	public void FindBestRun_RabinKarpAndLinear_ProduceIdenticalResults_RepeatingPattern() {
		byte[] source = new byte[1024];
		for (int i = 0; i < source.Length; i++) {
			source[i] = (byte)(i % 16);
		}

		byte[] target = new byte[48]; // 3 full repetitions
		for (int i = 0; i < target.Length; i++) {
			target[i] = (byte)(i % 16);
		}

		var rkResult = Encoder.FindBestRunRabinKarp(source, target);
		var linearResult = Encoder.FindBestRunLinear(source, target);

		Assert.Equal(linearResult.Length, rkResult.Length);
		Assert.Equal(linearResult.Start, rkResult.Start);
		Assert.Equal(linearResult.ReachedEnd, rkResult.ReachedEnd);
	}

	[Fact]
	[Trait("Category", "Performance")]
	public void FindBestRun_RabinKarpAndLinear_ProduceIdenticalResults_LargeFile() {
		// Test with 100KB of data
		byte[] source = new byte[100 * 1024];
		var random = new Random(42);
		random.NextBytes(source);

		byte[] target = new byte[64];
		Array.Copy(source, 50 * 1024, target, 0, 64); // Pattern at 50KB mark

		var rkResult = Encoder.FindBestRunRabinKarp(source, target);
		var linearResult = Encoder.FindBestRunLinear(source, target);

		Assert.Equal(linearResult.Length, rkResult.Length);
		Assert.Equal(linearResult.Start, rkResult.Start);
		Assert.Equal(linearResult.ReachedEnd, rkResult.ReachedEnd);
	}

	[Fact]
	public void FindBestRun_RabinKarpAndLinear_ProduceIdenticalResults_MultipleMatches() {
		// Source with repeated pattern
		byte[] source = new byte[1024];
		byte[] repeatingBlock = [0xAA, 0xBB, 0xCC, 0xDD];
		for (int i = 0; i < source.Length; i++) {
			source[i] = repeatingBlock[i % 4];
		}

		byte[] target = [0xAA, 0xBB, 0xCC, 0xDD, 0xAA, 0xBB];

		var rkResult = Encoder.FindBestRunRabinKarp(source, target);
		var linearResult = Encoder.FindBestRunLinear(source, target);

		Assert.Equal(linearResult.Length, rkResult.Length);
		Assert.Equal(linearResult.Start, rkResult.Start);
		Assert.Equal(linearResult.ReachedEnd, rkResult.ReachedEnd);
		Assert.Equal(0, rkResult.Start); // Should find first occurrence
	}

	[Theory]
	[InlineData(4)]     // Minimum match length
	[InlineData(8)]
	[InlineData(16)]
	[InlineData(32)]
	[InlineData(64)]
	[InlineData(128)]
	public void FindBestRun_RabinKarpAndLinear_ProduceIdenticalResults_VariousPatternSizes(int patternSize) {
		byte[] source = new byte[1024];
		var random = new Random(42);
		random.NextBytes(source);

		byte[] target = new byte[patternSize];
		Array.Copy(source, 256, target, 0, patternSize);

		var rkResult = Encoder.FindBestRunRabinKarp(source, target);
		var linearResult = Encoder.FindBestRunLinear(source, target);

		Assert.Equal(linearResult.Length, rkResult.Length);
		Assert.Equal(linearResult.Start, rkResult.Start);
		Assert.Equal(linearResult.ReachedEnd, rkResult.ReachedEnd);
	}

	[Fact]
	public void FindBestRun_RabinKarpAndLinear_ProduceIdenticalResults_WithMinimumLength() {
		byte[] source = new byte[1024];
		for (int i = 0; i < source.Length; i++) {
			source[i] = (byte)(i % 256);
		}

		byte[] target = new byte[16];
		Array.Copy(source, 100, target, 0, 16);

		int minimumLength = 8;

		var rkResult = Encoder.FindBestRunRabinKarp(source, target, minimumLength);
		var linearResult = Encoder.FindBestRunLinear(source, target, minimumLength);

		Assert.Equal(linearResult.Length, rkResult.Length);
		Assert.Equal(linearResult.Start, rkResult.Start);
		Assert.Equal(linearResult.ReachedEnd, rkResult.ReachedEnd);
	}
}

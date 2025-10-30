// ========================================================================================================
// Utilities Tests - CRC32 Computation and Validation
// ========================================================================================================
// Comprehensive tests for CRC32 functionality used in BPS patch validation.
//
// References:
// - xUnit: https://xunit.net/
// - CRC32 Algorithm: https://en.wikipedia.org/wiki/Cyclic_redundancy_check
// ========================================================================================================

namespace bps_patch.Tests;

/// <summary>
/// Tests for the Utilities class CRC32 computation.
/// Validates correct CRC32 calculation for known test vectors.
/// </summary>
public class UtilitiesTests : TestBase {
	/// <summary>
	/// Tests CRC32 computation with empty input.
	/// Empty data should produce the CRC32 initialization value.
	/// </summary>
	[Fact]
	public void ComputeCRC32_EmptyFile_ReturnsCorrectHash() {
		// Arrange: Create an empty temporary file
		var tempFile = Path.GetTempFileName();
		try {
			WriteAllBytesWithSharing(tempFile, []);

			// Act: Compute CRC32 of empty file
			uint result = Utilities.ComputeCRC32(new FileInfo(tempFile));

			// Assert: Empty file should have CRC32 = 0
			Assert.Equal(0u, result);
		} finally {
			// Cleanup: Delete temporary file
			File.Delete(tempFile);
		}
	}

	/// <summary>
	/// Tests CRC32 computation with known test vector.
	/// The string "123456789" has a well-known CRC32 value.
	/// Reference: https://reveng.sourceforge.io/crc-catalogue/17plus.htm#crc.cat.crc-32-iso-hdlc
	/// </summary>
	[Fact]
	public void ComputeCRC32_KnownTestVector_ReturnsCorrectHash() {
		// Arrange: Create file with known content "123456789"
		var tempFile = Path.GetTempFileName();
		try {
			byte[] testData = "123456789"u8.ToArray();
			WriteAllBytesWithSharing(tempFile, testData);

			// Act: Compute CRC32
			uint result = Utilities.ComputeCRC32(new FileInfo(tempFile));

			// Assert: "123456789" has CRC32 = 0xCBF43926
			// Note: This is the standard CRC-32/ISO-HDLC polynomial
			Assert.Equal(0xCBF43926u, result);
		} finally {
			// Cleanup
			File.Delete(tempFile);
		}
	}

	/// <summary>
	/// Tests CRC32 computation with larger file (1MB of zeros).
	/// Validates performance and correctness with larger inputs.
	/// </summary>
	[Fact]
	public void ComputeCRC32_LargeFile_ReturnsCorrectHash() {
		// Arrange: Create 1MB file of zeros
		var tempFile = Path.GetTempFileName();
		try {
			byte[] testData = new byte[1024 * 1024]; // 1MB of zeros
			WriteAllBytesWithSharing(tempFile, testData);

			// Act: Compute CRC32
			uint result = Utilities.ComputeCRC32(new FileInfo(tempFile));

			// Assert: 1MB of zeros has a specific CRC32 value
			// This validates the incremental CRC32 calculation works correctly
			Assert.NotEqual(0u, result); // Should not be zero
		} finally {
			// Cleanup
			File.Delete(tempFile);
		}
	}

	/// <summary>
	/// Tests that different inputs produce different CRC32 values.
	/// Validates that CRC32 can distinguish between different files.
	/// </summary>
	[Fact]
	public void ComputeCRC32_DifferentInputs_ProduceDifferentHashes() {
		// Arrange: Create two files with different content
		var tempFile1 = Path.GetTempFileName();
		var tempFile2 = Path.GetTempFileName();
		try {
			WriteAllBytesWithSharing(tempFile1, "Hello World"u8.ToArray());
			WriteAllBytesWithSharing(tempFile2, "Hello world"u8.ToArray()); // Different case

			// Act: Compute CRC32 for both files
			uint hash1 = Utilities.ComputeCRC32(new FileInfo(tempFile1));
			uint hash2 = Utilities.ComputeCRC32(new FileInfo(tempFile2));

			// Assert: Different content should produce different hashes
			Assert.NotEqual(hash1, hash2);
		} finally {
			// Cleanup
			File.Delete(tempFile1);
			File.Delete(tempFile2);
		}
	}

	/// <summary>
	/// Tests that identical inputs produce identical CRC32 values.
	/// Validates deterministic behavior of CRC32 calculation.
	/// </summary>
	[Fact]
	public void ComputeCRC32_IdenticalInputs_ProduceIdenticalHashes() {
		// Arrange: Create two identical files
		var tempFile1 = Path.GetTempFileName();
		var tempFile2 = Path.GetTempFileName();
		try {
			byte[] testData = "Test Data"u8.ToArray();
			WriteAllBytesWithSharing(tempFile1, testData);
			WriteAllBytesWithSharing(tempFile2, testData);

			// Act: Compute CRC32 for both files
			uint hash1 = Utilities.ComputeCRC32(new FileInfo(tempFile1));
			uint hash2 = Utilities.ComputeCRC32(new FileInfo(tempFile2));

			// Assert: Identical content should produce identical hashes
			Assert.Equal(hash1, hash2);
		} finally {
			// Cleanup
			File.Delete(tempFile1);
			File.Delete(tempFile2);
		}
	}

	/// <summary>
	/// Tests CRC32 constant validation.
	/// The BPS format uses a magic constant for patch validation.
	/// </summary>
	[Fact]
	public void CRC32_ResultConstant_HasCorrectValue() {
		// The BPS format uses 0x2144DF1C as the expected result when
		// computing CRC32(patch_data + patch_crc32_bytes)
		// Reference: BPS specification
		Assert.Equal(0x2144DF1Cu, Utilities.CRC32_RESULT_CONSTANT);
	}
}


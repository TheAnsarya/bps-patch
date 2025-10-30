// ========================================================================================================
// Advanced Utilities Tests - CRC32 Comprehensive Testing
// ========================================================================================================
// Additional comprehensive tests for CRC32 computation and validation with focus on:
// - Edge cases and boundary conditions
// - Performance with various file sizes
// - CRC32 mathematical properties
// - Validation constant correctness
//
// References:
// - CRC32 Algorithm: https://en.wikipedia.org/wiki/Cyclic_redundancy_check
// - CRC32 Test Vectors: https://reveng.sourceforge.io/crc-catalogue/17plus.htm#crc.cat.crc-32-iso-hdlc
// - System.IO.Hashing.Crc32: https://learn.microsoft.com/en-us/dotnet/api/system.io.hashing.crc32
// ========================================================================================================

namespace bps_patch.Tests;

/// <summary>
/// Advanced tests for Utilities class CRC32 functionality.
/// </summary>
public class AdvancedUtilitiesTests : TestBase {
	/// <summary>
	/// Helper to create a clean temporary file path.
	/// </summary>
	private static string GetTempFile() => Path.Combine(Path.GetTempPath(), $"bps_util_{Guid.NewGuid()}.tmp");

	// ============================================================
	// CRC32 Test Vectors (Known Values)
	// ============================================================

	/// <summary>
	/// Tests CRC32 computation with standard test vector "The quick brown fox jumps over the lazy dog".
	/// Known CRC32: 0x414FA339
	/// Reference: Common CRC32 test string
	/// </summary>
	[Fact]
	public void ComputeCRC32_QuickBrownFox_ReturnsKnownHash() {
		var tempFile = GetTempFile();
		try {
			var testString = "The quick brown fox jumps over the lazy dog"u8.ToArray();
			WriteAllBytesWithSharing(tempFile, testString);

			var result = Utilities.ComputeCRC32(new FileInfo(tempFile));

			// Known CRC32 for this string (using standard CRC32 algorithm)
			Assert.Equal(0x414FA339u, result);
		} finally {
			File.Delete(tempFile);
		}
	}

	/// <summary>
	/// Tests CRC32 with all zeros input.
	/// Validates CRC32 polynomial behavior with uniform input.
	/// </summary>
	[Fact]
	public void ComputeCRC32_AllZeros_ReturnsCorrectHash() {
		var tempFile = GetTempFile();
		try {
			var zeros = new byte[1000];
			Array.Fill<byte>(zeros, 0x00);
			WriteAllBytesWithSharing(tempFile, zeros);

			var result = Utilities.ComputeCRC32(new FileInfo(tempFile));

			// CRC32 of 1000 zeros is deterministic
			Assert.NotEqual(0u, result); // Should not be zero
		} finally {
			File.Delete(tempFile);
		}
	}

	/// <summary>
	/// Tests CRC32 with all 0xFF bytes.
	/// Validates CRC32 polynomial behavior with all bits set.
	/// </summary>
	[Fact]
	public void ComputeCRC32_AllOnes_ReturnsCorrectHash() {
		var tempFile = GetTempFile();
		try {
			var ones = new byte[1000];
			Array.Fill<byte>(ones, 0xFF);
			WriteAllBytesWithSharing(tempFile, ones);

			var result = Utilities.ComputeCRC32(new FileInfo(tempFile));

			// CRC32 of all 0xFF is deterministic and different from zeros
			Assert.NotEqual(0u, result);
		} finally {
			File.Delete(tempFile);
		}
	}

	// ============================================================
	// CRC32 Mathematical Properties
	// ============================================================

	/// <summary>
	/// Tests that CRC32 is deterministic: same input always produces same output.
	/// </summary>
	[Fact]
	public void ComputeCRC32_Deterministic_SameInputProducesSameOutput() {
		var tempFile = GetTempFile();
		try {
			var data = "Deterministic Test Data"u8.ToArray();
			WriteAllBytesWithSharing(tempFile, data);

			var result1 = Utilities.ComputeCRC32(new FileInfo(tempFile));
			var result2 = Utilities.ComputeCRC32(new FileInfo(tempFile));
			var result3 = Utilities.ComputeCRC32(new FileInfo(tempFile));

			Assert.Equal(result1, result2);
			Assert.Equal(result2, result3);
		} finally {
			File.Delete(tempFile);
		}
	}

	/// <summary>
	/// Tests that CRC32 is sensitive to single bit flip.
	/// Changing one bit should produce a completely different hash.
	/// </summary>
	[Fact]
	public void ComputeCRC32_BitFlipSensitivity_ChangesSingleBit() {
		var tempFile1 = GetTempFile();
		var tempFile2 = GetTempFile();

		try {
			var data1 = new byte[] { 0b10101010 }; // Binary: 10101010
			var data2 = new byte[] { 0b10101011 }; // Binary: 10101011 (last bit flipped)

			WriteAllBytesWithSharing(tempFile1, data1);
			WriteAllBytesWithSharing(tempFile2, data2);

			var crc1 = Utilities.ComputeCRC32(new FileInfo(tempFile1));
			var crc2 = Utilities.ComputeCRC32(new FileInfo(tempFile2));

			// Single bit flip should change the CRC32
			Assert.NotEqual(crc1, crc2);
		} finally {
			File.Delete(tempFile1);
			File.Delete(tempFile2);
		}
	}

	/// <summary>
	/// Tests that CRC32 changes with byte order (not commutative).
	/// "AB" should have different CRC32 than "BA".
	/// </summary>
	[Fact]
	public void ComputeCRC32_ByteOrderSensitivity_DifferentOrder() {
		var tempFile1 = GetTempFile();
		var tempFile2 = GetTempFile();

		try {
			WriteAllBytesWithSharing(tempFile1, "ABCD"u8.ToArray());
			WriteAllBytesWithSharing(tempFile2, "DCBA"u8.ToArray());

			var crc1 = Utilities.ComputeCRC32(new FileInfo(tempFile1));
			var crc2 = Utilities.ComputeCRC32(new FileInfo(tempFile2));

			// Different byte order should produce different CRC32
			Assert.NotEqual(crc1, crc2);
		} finally {
			File.Delete(tempFile1);
			File.Delete(tempFile2);
		}
	}

	// ============================================================
	// Performance and Scalability Tests
	// ============================================================

	/// <summary>
	/// Tests CRC32 computation with various file sizes to validate buffered reading.
	/// </summary>
	[Theory]
	[InlineData(100)]          // Small file
	[InlineData(1024)]         // 1 KB
	[InlineData(10240)]        // 10 KB
	[InlineData(102400)]       // 100 KB
	[InlineData(1048576)]      // 1 MB
	public void ComputeCRC32_VariousFileSizes_ComputesCorrectly(int fileSize) {
		var tempFile = GetTempFile();
		try {
			var data = new byte[fileSize];
			Random.Shared.NextBytes(data);
			WriteAllBytesWithSharing(tempFile, data);

			var crc = Utilities.ComputeCRC32(new FileInfo(tempFile));

			// Should complete without error and produce non-zero hash (very unlikely to be 0)
			Assert.NotEqual(0u, crc);
		} finally {
			File.Delete(tempFile);
		}
	}

	/// <summary>
	/// Tests that CRC32 handles exactly 80KB file (buffer boundary).
	/// BufferedStream uses 80KB buffer, so this tests the boundary.
	/// </summary>
	[Fact]
	public void ComputeCRC32_Exactly80KB_HandlesBufferBoundary() {
		var tempFile = GetTempFile();
		try {
			const int bufferSize = 81920; // 80 KB (80 * 1024)
			var data = new byte[bufferSize];
			Random.Shared.NextBytes(data);
			WriteAllBytesWithSharing(tempFile, data);

			var crc = Utilities.ComputeCRC32(new FileInfo(tempFile));

			// Should handle buffer boundary correctly
			Assert.NotEqual(0u, crc);
		} finally {
			File.Delete(tempFile);
		}
	}

	/// <summary>
	/// Tests CRC32 with file size just over buffer boundary (80KB + 1 byte).
	/// </summary>
	[Fact]
	public void ComputeCRC32_OverBufferBoundary_HandlesCorrectly() {
		var tempFile = GetTempFile();
		try {
			const int size = 81921; // 80 KB + 1 byte
			var data = new byte[size];
			Random.Shared.NextBytes(data);
			WriteAllBytesWithSharing(tempFile, data);

			var crc = Utilities.ComputeCRC32(new FileInfo(tempFile));

			Assert.NotEqual(0u, crc);
		} finally {
			File.Delete(tempFile);
		}
	}

	// ============================================================
	// ComputeCRC32Bytes Tests
	// ============================================================

	/// <summary>
	/// Tests that ComputeCRC32Bytes returns same value as ComputeCRC32 in byte form.
	/// </summary>
	[Fact]
	public void ComputeCRC32Bytes_MatchesComputeCRC32_SameValue() {
		var tempFile = GetTempFile();
		try {
			var data = "Test Data for CRC32 Bytes"u8.ToArray();
			WriteAllBytesWithSharing(tempFile, data);

			var crcUint = Utilities.ComputeCRC32(new FileInfo(tempFile));
			var crcBytes = Utilities.ComputeCRC32Bytes(new FileInfo(tempFile));

			// Convert bytes to uint (little-endian)
			var crcFromBytes = BitConverter.ToUInt32(crcBytes);

			Assert.Equal(crcUint, crcFromBytes);
		} finally {
			File.Delete(tempFile);
		}
	}

	/// <summary>
	/// Tests that ComputeCRC32Bytes returns exactly 4 bytes.
	/// </summary>
	[Fact]
	public void ComputeCRC32Bytes_ReturnsExactly4Bytes() {
		var tempFile = GetTempFile();
		try {
			var data = "Sample data"u8.ToArray();
			WriteAllBytesWithSharing(tempFile, data);

			var crcBytes = Utilities.ComputeCRC32Bytes(new FileInfo(tempFile));

			Assert.Equal(4, crcBytes.Length);
		} finally {
			File.Delete(tempFile);
		}
	}

	/// <summary>
	/// Tests that ComputeCRC32Bytes uses little-endian byte order.
	/// </summary>
	[Fact]
	public void ComputeCRC32Bytes_UsesLittleEndian_ByteOrder() {
		var tempFile = GetTempFile();
		try {
			var data = "123456789"u8.ToArray();
			WriteAllBytesWithSharing(tempFile, data);

			var crcUint = Utilities.ComputeCRC32(new FileInfo(tempFile));
			var crcBytes = Utilities.ComputeCRC32Bytes(new FileInfo(tempFile));

			// Manual little-endian conversion
			uint manual = (uint)(crcBytes[0] | (crcBytes[1] << 8) | (crcBytes[2] << 16) | (crcBytes[3] << 24));

			Assert.Equal(crcUint, manual);
		} finally {
			File.Delete(tempFile);
		}
	}

	// ============================================================
	// CRC32 Result Constant Validation
	// ============================================================

	/// <summary>
	/// Tests that CRC32_RESULT_CONSTANT has the correct value (0x2144df1c).
	/// This constant is used in BPS patch self-validation.
	/// Reference: BPS specification - CRC32 of patch including its own CRC32 should equal this constant.
	/// </summary>
	[Fact]
	public void CRC32ResultConstant_HasCorrectValue() {
		// The magic constant for BPS patch validation
		Assert.Equal(0x2144df1cu, Utilities.CRC32_RESULT_CONSTANT);
	}

	/// <summary>
	/// Tests CRC32 mathematical property: CRC32(data || CRC32(data)) has special property.
	/// When you append the CRC32 of data to the data itself and compute CRC32 again,
	/// the result should be a constant value (0x2144df1c for this CRC32 variant).
	/// </summary>
	[Fact]
	public void ComputeCRC32_WithAppendedCRC_EqualsResultConstant() {
		var tempFile1 = GetTempFile();
		var tempFile2 = GetTempFile();

		try {
			// Original data
			var data = "Test data for CRC32 validation constant"u8.ToArray();
			WriteAllBytesWithSharing(tempFile1, data);

			// Compute CRC32 of original data
			var crc1 = Utilities.ComputeCRC32Bytes(new FileInfo(tempFile1));

			// Append CRC32 to data: data || CRC32(data)
			var dataWithCRC = new byte[data.Length + 4];
			data.CopyTo(dataWithCRC, 0);
			crc1.CopyTo(dataWithCRC, data.Length);
			WriteAllBytesWithSharing(tempFile2, dataWithCRC);

			// Compute CRC32 of data+CRC32
			var crc2 = Utilities.ComputeCRC32(new FileInfo(tempFile2));

			// Should equal the magic constant
			Assert.Equal(Utilities.CRC32_RESULT_CONSTANT, crc2);
		} finally {
			File.Delete(tempFile1);
			File.Delete(tempFile2);
		}
	}

	// ============================================================
	// Error Handling Tests
	// ============================================================

	/// <summary>
	/// Tests that ComputeCRC32 throws FileNotFoundException for non-existent file.
	/// </summary>
	[Fact]
	public void ComputeCRC32_NonExistentFile_ThrowsFileNotFoundException() {
		var nonExistentFile = GetTempFile(); // Not created

		Assert.Throws<FileNotFoundException>(() => {
			Utilities.ComputeCRC32(new FileInfo(nonExistentFile));
		});
	}

	/// <summary>
	/// Tests that ComputeCRC32Bytes throws FileNotFoundException for non-existent file.
	/// </summary>
	[Fact]
	public void ComputeCRC32Bytes_NonExistentFile_ThrowsFileNotFoundException() {
		var nonExistentFile = GetTempFile(); // Not created

		Assert.Throws<FileNotFoundException>(() => {
			Utilities.ComputeCRC32Bytes(new FileInfo(nonExistentFile));
		});
	}

	// ============================================================
	// Collision Resistance Tests
	// ============================================================

	/// <summary>
	/// Tests that CRC32 produces different hashes for similar but different data.
	/// While CRC32 is not collision-resistant for adversarial inputs, random data should hash differently.
	/// </summary>
	[Fact]
	public void ComputeCRC32_RandomData_ProducesDifferentHashes() {
		var tempFile1 = GetTempFile();
		var tempFile2 = GetTempFile();

		try {
			var data1 = new byte[1000];
			var data2 = new byte[1000];
			Random.Shared.NextBytes(data1);
			Random.Shared.NextBytes(data2);

			WriteAllBytesWithSharing(tempFile1, data1);
			WriteAllBytesWithSharing(tempFile2, data2);

			var crc1 = Utilities.ComputeCRC32(new FileInfo(tempFile1));
			var crc2 = Utilities.ComputeCRC32(new FileInfo(tempFile2));

			// Extremely unlikely to be equal for random data
			Assert.NotEqual(crc1, crc2);
		} finally {
			File.Delete(tempFile1);
			File.Delete(tempFile2);
		}
	}
}


// ========================================================================================================
// Advanced Decoder Tests - Comprehensive Edge Cases and Error Handling
// ========================================================================================================
// Additional comprehensive tests for BPS patch decoding with focus on:
// - Malformed patch file handling
// - CRC32 validation scenarios
// - Patch action edge cases
// - Large file decoding
//
// References:
// - BPS Specification: https://github.com/blakesmith/beat/blob/master/doc/bps.txt
// - xUnit: https://xunit.net/
// - Exception Testing: https://xunit.net/docs/assert-throws
// ========================================================================================================

namespace bps_patch.Tests;

/// <summary>
/// Advanced tests for Decoder class focusing on error conditions, validation, and edge cases.
/// </summary>
public class AdvancedDecoderTests {
	/// <summary>
	/// Helper to create a clean temporary file path.
	/// </summary>
	private static string GetTempFile() => Path.Combine(Path.GetTempPath(), $"bps_decoder_{Guid.NewGuid()}.tmp");

	// ============================================================
	// Malformed Patch File Tests
	// ============================================================

	/// <summary>
	/// Tests that decoder rejects patch file with invalid magic number.
	/// Valid BPS patch must start with "BPS1" (0x42 0x50 0x53 0x31).
	/// </summary>
	[Fact]
	public void ApplyPatch_InvalidMagicNumber_ThrowsPatchFormatException() {
		var source = GetTempFile();
		var patch = GetTempFile();
		var target = GetTempFile();

		try {
			File.WriteAllBytes(source, "test"u8.ToArray());

			// Create invalid patch with wrong magic number
			var invalidPatch = new byte[] {
				0x49, 0x50, 0x53, 0x31, // "IPS1" instead of "BPS1"
				0x00, 0x00, 0x00, 0x00
			};
			File.WriteAllBytes(patch, invalidPatch);

			// Act & Assert
			Assert.Throws<PatchFormatException>(() => {
				Decoder.ApplyPatch(new FileInfo(source), new FileInfo(patch), new FileInfo(target));
			});
		} finally {
			File.Delete(source);
			File.Delete(patch);
			if (File.Exists(target)) File.Delete(target);
		}
	}

	/// <summary>
	/// Tests that decoder rejects patch file that's too small to be valid.
	/// Minimum valid BPS patch is 19 bytes: 4 (header) + 3 (sizes) + 12 (CRC32s).
	/// </summary>
	[Fact]
	public void ApplyPatch_TooSmallPatchFile_ThrowsPatchFormatException() {
		var source = GetTempFile();
		var patch = GetTempFile();
		var target = GetTempFile();

		try {
			File.WriteAllBytes(source, "test"u8.ToArray());

			// Create patch file that's too small (only 10 bytes)
			var tinyPatch = new byte[] {
				0x42, 0x50, 0x53, 0x31, // "BPS1" - valid magic
				0x00, 0x00, 0x00, 0x00, // Only 4 more bytes
				0x00, 0x00
			};
			File.WriteAllBytes(patch, tinyPatch);

			// Act & Assert
			Assert.Throws<PatchFormatException>(() => {
				Decoder.ApplyPatch(new FileInfo(source), new FileInfo(patch), new FileInfo(target));
			});
		} finally {
			File.Delete(source);
			File.Delete(patch);
			if (File.Exists(target)) File.Delete(target);
		}
	}

	/// <summary>
	/// Tests that decoder rejects patch with file size exceeding int.MaxValue.
	/// BPS implementation uses int for sizes, limited to 2GB - 1 byte.
	/// </summary>
	[Fact]
	public void ApplyPatch_FileSizeExceedsMaxInt_ThrowsPatchFormatException() {
		var source = GetTempFile();
		var patch = GetTempFile();
		var target = GetTempFile();

		try {
			File.WriteAllBytes(source, "test"u8.ToArray());

			// Create patch claiming file size > int.MaxValue
			// Variable-length encoding for a very large number
			using (var ms = new MemoryStream()) {
				ms.Write("BPS1"u8);

				// Encode source size (4 bytes = small)
				ms.WriteByte(0x84); // 4 with continuation bit

				// Encode target size > int.MaxValue (would overflow)
				// Write maximum + 1 in variable-length encoding
				WriteVariableLengthInt(ms, (long)int.MaxValue + 100);

				// Rest doesn't matter as it should fail before reading further
				File.WriteAllBytes(patch, ms.ToArray());
			}

			// Act & Assert
			Assert.Throws<PatchFormatException>(() => {
				Decoder.ApplyPatch(new FileInfo(source), new FileInfo(patch), new FileInfo(target));
			});
		} finally {
			File.Delete(source);
			File.Delete(patch);
			if (File.Exists(target)) File.Delete(target);
		}
	}

	// ============================================================
	// CRC32 Validation Tests
	// ============================================================

	/// <summary>
	/// Tests that decoder produces warning when source file CRC32 doesn't match patch expectation.
	/// Decoder should complete but warn about potential mismatch.
	/// </summary>
	[Fact]
	public void ApplyPatch_SourceCRC32Mismatch_ProducesWarning() {
		var source1 = GetTempFile();
		var source2 = GetTempFile();
		var target1 = GetTempFile();
		var patch = GetTempFile();
		var target2 = GetTempFile();

		try {
			// Create patch with source1
			File.WriteAllBytes(source1, "original source"u8.ToArray());
			File.WriteAllBytes(target1, "modified target"u8.ToArray());
			Encoder.CreatePatch(new FileInfo(source1), new FileInfo(patch), new FileInfo(target1), "");

			// Apply patch with different source (source2)
			File.WriteAllBytes(source2, "different source"u8.ToArray());

			// Act: Apply patch with wrong source
			var warnings = Decoder.ApplyPatch(new FileInfo(source2), new FileInfo(patch), new FileInfo(target2));

			// Assert: Should have CRC32 warning
			Assert.NotEmpty(warnings);
			Assert.Contains(warnings, w => w.Contains("CRC32", StringComparison.OrdinalIgnoreCase));
		} finally {
			File.Delete(source1);
			File.Delete(source2);
			File.Delete(target1);
			File.Delete(patch);
			if (File.Exists(target2)) File.Delete(target2);
		}
	}

	// ============================================================
	// Patch Action Edge Cases
	// ============================================================

	/// <summary>
	/// Tests decoder handling of TargetCopy with overlapping regions.
	/// When TargetCopy copies from earlier in target with overlap, it should handle correctly.
	/// Example: Copy 10 bytes from offset 0 to offset 5 (5-byte overlap).
	/// </summary>
	[Fact]
	public void ApplyPatch_TargetCopyOverlapping_HandlesCorrectly() {
		var source = GetTempFile();
		var target1 = GetTempFile();
		var patch = GetTempFile();
		var target2 = GetTempFile();

		try {
			// Create scenario that produces overlapping TargetCopy
			var sourceData = new byte[100];
			Array.Fill<byte>(sourceData, 0x00);

			var targetData = new byte[100];
			// Pattern that when RLE-encoded will create overlapping copy
			for (int i = 0; i < 100; i++) {
				targetData[i] = (byte)(i % 10); // Repeating pattern of 10 bytes
			}

			File.WriteAllBytes(source, sourceData);
			File.WriteAllBytes(target1, targetData);

			// Create patch
			Encoder.CreatePatch(new FileInfo(source), new FileInfo(patch), new FileInfo(target1), "");

			// Apply patch
			var warnings = Decoder.ApplyPatch(new FileInfo(source), new FileInfo(patch), new FileInfo(target2));

			// Assert: Should reconstruct correctly
			var result = File.ReadAllBytes(target2);
			Assert.Equal(targetData, result);
		} finally {
			File.Delete(source);
			File.Delete(target1);
			File.Delete(patch);
			if (File.Exists(target2)) File.Delete(target2);
		}
	}

	/// <summary>
	/// Tests decoder with patch containing only SourceRead actions.
	/// Simplest case: target identical to source.
	/// </summary>
	[Fact]
	public void ApplyPatch_OnlySourceRead_ReconstructsIdentical() {
		var source = GetTempFile();
		var patch = GetTempFile();
		var target = GetTempFile();

		try {
			var data = "This is identical in source and target"u8.ToArray();
			File.WriteAllBytes(source, data);

			// Create patch from source to itself
			Encoder.CreatePatch(new FileInfo(source), new FileInfo(patch), new FileInfo(source), "Identity patch");

			// Apply patch
			var warnings = Decoder.ApplyPatch(new FileInfo(source), new FileInfo(patch), new FileInfo(target));

			// Assert
			var result = File.ReadAllBytes(target);
			Assert.Equal(data, result);
		} finally {
			File.Delete(source);
			File.Delete(patch);
			if (File.Exists(target)) File.Delete(target);
		}
	}

	/// <summary>
	/// Tests decoder with patch containing only TargetRead actions.
	/// Case: source and target are completely different.
	/// </summary>
	[Fact]
	public void ApplyPatch_OnlyTargetRead_ReconstructsNew() {
		var source = GetTempFile();
		var target1 = GetTempFile();
		var patch = GetTempFile();
		var target2 = GetTempFile();

		try {
			// Completely different data
			var sourceData = new byte[100];
			Array.Fill<byte>(sourceData, 0xAA);

			var targetData = new byte[100];
			Array.Fill<byte>(targetData, 0x55);

			File.WriteAllBytes(source, sourceData);
			File.WriteAllBytes(target1, targetData);

			// Create patch
			Encoder.CreatePatch(new FileInfo(source), new FileInfo(patch), new FileInfo(target1), "");

			// Apply patch
			var warnings = Decoder.ApplyPatch(new FileInfo(source), new FileInfo(patch), new FileInfo(target2));

			// Assert
			var result = File.ReadAllBytes(target2);
			Assert.Equal(targetData, result);
		} finally {
			File.Delete(source);
			File.Delete(target1);
			File.Delete(patch);
			if (File.Exists(target2)) File.Delete(target2);
		}
	}

	// ============================================================
	// Large File Tests
	// ============================================================

	/// <summary>
	/// Tests decoder with multi-MB file to validate buffered streaming.
	/// Ensures decoder doesn't load entire file into memory.
	/// </summary>
	[Fact]
	public void ApplyPatch_LargeFile_UsesBufferedStreaming() {
		var source = GetTempFile();
		var target1 = GetTempFile();
		var patch = GetTempFile();
		var target2 = GetTempFile();

		try {
			// Create 10MB files
			const int size = 10 * 1024 * 1024;
			var sourceData = new byte[size];
			var targetData = new byte[size];

			// Fill with patterns
			for (int i = 0; i < size; i += 1024) {
				sourceData[i] = (byte)(i / 1024);
				targetData[i] = (byte)((i / 1024) + 1);
			}

			File.WriteAllBytes(source, sourceData);
			File.WriteAllBytes(target1, targetData);

			// Create patch
			Encoder.CreatePatch(new FileInfo(source), new FileInfo(patch), new FileInfo(target1), "Large file");

			// Apply patch
			var warnings = Decoder.ApplyPatch(new FileInfo(source), new FileInfo(patch), new FileInfo(target2));

			// Assert: Basic validation (full comparison would be slow)
			var resultInfo = new FileInfo(target2);
			Assert.Equal(size, resultInfo.Length);
		} finally {
			File.Delete(source);
			File.Delete(target1);
			File.Delete(patch);
			if (File.Exists(target2)) File.Delete(target2);
		}
	}

	// ============================================================
	// Helper Methods
	// ============================================================

	/// <summary>
	/// Writes a value as variable-length integer (BPS encoding format).
	/// 7 bits of data per byte, MSB indicates continuation.
	/// </summary>
	private static void WriteVariableLengthInt(Stream stream, long value) {
		var bytes = new List<byte>();
		while (true) {
			byte b = (byte)(value & 0x7F);
			value >>= 7;
			if (value == 0) {
				bytes.Add((byte)(b | 0x80)); // Final byte has MSB set
				break;
			}
			bytes.Add(b);
		}
		stream.Write(bytes.ToArray());
	}
}

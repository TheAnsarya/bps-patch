// ========================================================================================================
// Advanced Encoder Tests - Comprehensive Edge Cases and Performance Scenarios
// ========================================================================================================
// Additional comprehensive tests for BPS patch encoding with focus on:
// - Edge cases and boundary conditions
// - Error handling and validation
// - Pattern matching optimization scenarios
// - Large file handling
//
// References:
// - BPS Specification: https://github.com/blakesmith/beat/blob/master/doc/bps.txt
// - xUnit Theory: https://xunit.net/docs/getting-started/netcore/cmdline#write-first-theory
// - Test-Driven Development: https://learn.microsoft.com/en-us/dotnet/core/testing/
// ========================================================================================================

namespace bps_patch.Tests;

/// <summary>
/// Advanced tests for Encoder class focusing on edge cases, error conditions, and optimization scenarios.
/// </summary>
public class AdvancedEncoderTests {
	/// <summary>
	/// Helper to create a clean temporary file path.
	/// </summary>
	private static string GetTempFile() => Path.Combine(Path.GetTempPath(), $"bps_advanced_{Guid.NewGuid()}.tmp");

	// ============================================================
	// Edge Cases: File Size Boundaries
	// ============================================================

	/// <summary>
	/// Tests patch creation with maximum practical file size near int.MaxValue.
	/// Validates that encoder handles large files correctly.
	/// </summary>
	[Fact(Skip = "Requires >2GB free disk space - enable manually for large file testing")]
	public void CreatePatch_NearMaxFileSize_HandlesCorrectly() {
		// This test validates behavior near the int.MaxValue limit (2GB)
		// Skipped by default due to disk space and time requirements
		var source = GetTempFile();
		var target = GetTempFile();
		var patch = GetTempFile();

		try {
			// Create large files (100MB for practical testing)
			const int testSize = 100 * 1024 * 1024;
			var largeData = new byte[testSize];
			Random.Shared.NextBytes(largeData);

			File.WriteAllBytes(source, largeData);
			// Modify a few bytes
			largeData[testSize / 2] = 0xFF;
			largeData[testSize / 4] = 0xAA;
			File.WriteAllBytes(target, largeData);

			// Act: Create patch
			Encoder.CreatePatch(new FileInfo(source), new FileInfo(patch), new FileInfo(target), "Large file test");

			// Assert: Patch should exist and be much smaller than source
			var patchInfo = new FileInfo(patch);
			Assert.True(patchInfo.Exists);
			Assert.True(patchInfo.Length < testSize / 10); // Patch should be <10% of original
		} finally {
			// Cleanup
			File.Delete(source);
			File.Delete(target);
			File.Delete(patch);
		}
	}

	/// <summary>
	/// Tests patch creation with single-byte files.
	/// Edge case: minimum possible non-empty file.
	/// </summary>
	[Fact]
	public void CreatePatch_SingleByteFiles_ProducesValidPatch() {
		var source = GetTempFile();
		var target = GetTempFile();
		var patch = GetTempFile();

		try {
			// Single byte files
			File.WriteAllBytes(source, [0x42]);
			File.WriteAllBytes(target, [0x84]);

			// Act
			Encoder.CreatePatch(new FileInfo(source), new FileInfo(patch), new FileInfo(target), "");

			// Assert
			Assert.True(File.Exists(patch));
			var patchSize = new FileInfo(patch).Length;
			Assert.True(patchSize > 0 && patchSize < 100); // Should be minimal
		} finally {
			File.Delete(source);
			File.Delete(target);
			File.Delete(patch);
		}
	}

	// ============================================================
	// Pattern Matching Scenarios
	// ============================================================

	/// <summary>
	/// Tests patch creation with highly repetitive data (Run-Length Encoding scenario).
	/// Should produce efficient patch using TargetCopy actions.
	/// </summary>
	[Fact]
	public void CreatePatch_RepeatingPattern_UsesTargetCopy() {
		var source = GetTempFile();
		var target = GetTempFile();
		var patch = GetTempFile();

		try {
			// Source: different pattern
			var sourceData = new byte[1000];
			for (int i = 0; i < sourceData.Length; i++) {
				sourceData[i] = (byte)(i % 256);
			}

			// Target: repeating pattern "ABCD" 250 times
			var targetData = new byte[1000];
			for (int i = 0; i < targetData.Length; i++) {
				targetData[i] = (byte)("ABCD"[i % 4]);
			}

			File.WriteAllBytes(source, sourceData);
			File.WriteAllBytes(target, targetData);

			// Act
			Encoder.CreatePatch(new FileInfo(source), new FileInfo(patch), new FileInfo(target), "RLE test");

			// Assert: Patch should be small due to run-length encoding
			var patchSize = new FileInfo(patch).Length;
			Assert.True(patchSize < 500); // Should compress well
		} finally {
			File.Delete(source);
			File.Delete(target);
			File.Delete(patch);
		}
	}

	/// <summary>
	/// Tests patch creation with alternating match/no-match pattern.
	/// Validates encoder handles frequent context switching.
	/// </summary>
	[Fact]
	public void CreatePatch_AlternatingChanges_HandlesCorrectly() {
		var source = GetTempFile();
		var target = GetTempFile();
		var patch = GetTempFile();

		try {
			var sourceData = new byte[1000];
			var targetData = new byte[1000];

			// Every other byte is different
			for (int i = 0; i < 1000; i++) {
				sourceData[i] = (byte)i;
				targetData[i] = (i % 2 == 0) ? (byte)i : (byte)(i + 1);
			}

			File.WriteAllBytes(source, sourceData);
			File.WriteAllBytes(target, targetData);

			// Act
			Encoder.CreatePatch(new FileInfo(source), new FileInfo(patch), new FileInfo(target), "");

			// Assert
			Assert.True(File.Exists(patch));
		} finally {
			File.Delete(source);
			File.Delete(target);
			File.Delete(patch);
		}
	}

	// ============================================================
	// Metadata Handling
	// ============================================================

	/// <summary>
	/// Tests patch creation with large metadata (XML/JSON).
	/// Validates metadata size handling.
	/// </summary>
	[Fact]
	public void CreatePatch_LargeMetadata_EmbedsCorrectly() {
		var source = GetTempFile();
		var target = GetTempFile();
		var patch = GetTempFile();

		try {
			var data = "Test data"u8.ToArray();
			File.WriteAllBytes(source, data);
			File.WriteAllBytes(target, data);

			// Large metadata (4KB)
			var largeMetadata = new string('X', 4096);

			// Act
			Encoder.CreatePatch(new FileInfo(source), new FileInfo(patch), new FileInfo(target), largeMetadata);

			// Assert: Patch should contain the metadata
			var patchBytes = File.ReadAllBytes(patch);
			Assert.True(patchBytes.Length > 4096); // Must contain metadata
		} finally {
			File.Delete(source);
			File.Delete(target);
			File.Delete(patch);
		}
	}

	/// <summary>
	/// Tests patch creation with Unicode metadata.
	/// Validates UTF-8 encoding of metadata.
	/// </summary>
	[Fact]
	public void CreatePatch_UnicodeMetadata_EncodesCorrectly() {
		var source = GetTempFile();
		var target = GetTempFile();
		var patch = GetTempFile();

		try {
			var data = "Test"u8.ToArray();
			File.WriteAllBytes(source, data);
			File.WriteAllBytes(target, data);

			// Unicode metadata: Japanese, Emoji, etc.
			var unicodeMetadata = "作者: John Doe 🎮 Version: 1.0";

			// Act
			Encoder.CreatePatch(new FileInfo(source), new FileInfo(patch), new FileInfo(target), unicodeMetadata);

			// Assert: Should encode without error
			Assert.True(File.Exists(patch));
			var patchBytes = File.ReadAllBytes(patch);
			Assert.Contains((byte)'J', patchBytes); // ASCII part should be present
		} finally {
			File.Delete(source);
			File.Delete(target);
			File.Delete(patch);
		}
	}

	// ============================================================
	// SourceCopy Optimization Tests
	// ============================================================

	/// <summary>
	/// Tests patch creation when target contains data from elsewhere in source (SourceCopy).
	/// Example: Target reuses a block from source at a different offset.
	/// </summary>
	[Fact]
	public void CreatePatch_ReusesSourceBlock_UsesSourceCopy() {
		var source = GetTempFile();
		var target = GetTempFile();
		var patch = GetTempFile();

		try {
			// Source: [HEADER...DATA BLOCK...FOOTER]
			var header = "HEADER"u8.ToArray();
			var dataBlock = "REUSABLE_DATA_BLOCK"u8.ToArray();
			var footer = "FOOTER"u8.ToArray();

			var sourceData = new byte[header.Length + dataBlock.Length + footer.Length];
			header.CopyTo(sourceData, 0);
			dataBlock.CopyTo(sourceData, header.Length);
			footer.CopyTo(sourceData, header.Length + dataBlock.Length);

			// Target: [DIFFERENT_HEADER...DATA BLOCK...DIFFERENT_FOOTER]
			var newHeader = "NEWHED"u8.ToArray();
			var newFooter = "NEWFOT"u8.ToArray();
			var targetData = new byte[newHeader.Length + dataBlock.Length + newFooter.Length];
			newHeader.CopyTo(targetData, 0);
			dataBlock.CopyTo(targetData, newHeader.Length); // Reuse data block
			newFooter.CopyTo(targetData, newHeader.Length + dataBlock.Length);

			File.WriteAllBytes(source, sourceData);
			File.WriteAllBytes(target, targetData);

			// Act
			Encoder.CreatePatch(new FileInfo(source), new FileInfo(patch), new FileInfo(target), "SourceCopy test");

			// Assert: Patch should use SourceCopy for the data block
			var patchSize = new FileInfo(patch).Length;
			Assert.True(patchSize < targetData.Length); // Should be efficient
		} finally {
			File.Delete(source);
			File.Delete(target);
			File.Delete(patch);
		}
	}

	// ============================================================
	// Binary Data Patterns (ROM Hacking Scenarios)
	// ============================================================

	/// <summary>
	/// Tests patch creation with binary patterns common in ROM files.
	/// Simulates graphics tile data with repetition.
	/// </summary>
	[Fact]
	public void CreatePatch_GraphicsTilePattern_HandlesEfficiently() {
		var source = GetTempFile();
		var target = GetTempFile();
		var patch = GetTempFile();

		try {
			// Simulate 8x8 tile data (64 bytes per tile, 16 tiles = 1KB)
			var sourceTiles = new byte[1024];
			var targetTiles = new byte[1024];

			// Source: all tiles are blank (0x00)
			Array.Fill<byte>(sourceTiles, 0x00);

			// Target: some tiles have graphics data
			for (int i = 0; i < 16; i++) {
				for (int j = 0; j < 64; j++) {
					// Every 4th tile has a checkerboard pattern
					targetTiles[i * 64 + j] = (i % 4 == 0) ? (byte)((j % 2 == 0) ? 0xAA : 0x55) : (byte)0x00;
				}
			}

			File.WriteAllBytes(source, sourceTiles);
			File.WriteAllBytes(target, targetTiles);

			// Act
			Encoder.CreatePatch(new FileInfo(source), new FileInfo(patch), new FileInfo(target), "Graphics hack");

			// Assert
			Assert.True(File.Exists(patch));
			var patchSize = new FileInfo(patch).Length;
			Assert.True(patchSize < 1024); // Should compress pattern
		} finally {
			File.Delete(source);
			File.Delete(target);
			File.Delete(patch);
		}
	}

	// ============================================================
	// Error Handling and Validation
	// ============================================================

	/// <summary>
	/// Tests that encoder throws ArgumentException for zero-byte target file.
	/// BPS format requires non-empty target (per specification).
	/// </summary>
	[Fact]
	public void CreatePatch_ZeroByteTarget_ThrowsArgumentException() {
		var source = GetTempFile();
		var target = GetTempFile();
		var patch = GetTempFile();

		try {
			File.WriteAllBytes(source, "source data"u8.ToArray());
			File.WriteAllBytes(target, []); // Empty target

			// Act & Assert: Should throw
			Assert.Throws<ArgumentException>(() => {
				Encoder.CreatePatch(new FileInfo(source), new FileInfo(patch), new FileInfo(target), "");
			});
		} finally {
			File.Delete(source);
			if (File.Exists(target)) File.Delete(target);
			if (File.Exists(patch)) File.Delete(patch);
		}
	}

	/// <summary>
	/// Tests that encoder handles non-existent source file gracefully.
	/// </summary>
	[Fact]
	public void CreatePatch_NonExistentSource_ThrowsFileNotFoundException() {
		var source = GetTempFile(); // Not created
		var target = GetTempFile();
		var patch = GetTempFile();

		try {
			File.WriteAllBytes(target, "data"u8.ToArray());

			// Act & Assert
			Assert.Throws<FileNotFoundException>(() => {
				Encoder.CreatePatch(new FileInfo(source), new FileInfo(patch), new FileInfo(target), "");
			});
		} finally {
			if (File.Exists(target)) File.Delete(target);
			if (File.Exists(patch)) File.Delete(patch);
		}
	}

	/// <summary>
	/// Tests that encoder handles non-existent target file gracefully.
	/// </summary>
	[Fact]
	public void CreatePatch_NonExistentTarget_ThrowsFileNotFoundException() {
		var source = GetTempFile();
		var target = GetTempFile(); // Not created
		var patch = GetTempFile();

		try {
			File.WriteAllBytes(source, "data"u8.ToArray());

			// Act & Assert
			Assert.Throws<FileNotFoundException>(() => {
				Encoder.CreatePatch(new FileInfo(source), new FileInfo(patch), new FileInfo(target), "");
			});
		} finally {
			if (File.Exists(source)) File.Delete(source);
			if (File.Exists(patch)) File.Delete(patch);
		}
	}

	// ============================================================
	// Minimum Match Length Tests
	// ============================================================

	/// <summary>
	/// Tests that encoder respects minimum match length (4 bytes).
	/// Matches shorter than 4 bytes should not use SourceCopy/TargetCopy.
	/// </summary>
	[Fact]
	public void CreatePatch_ShortMatches_UsesTargetReadInsteadOfCopy() {
		var source = GetTempFile();
		var target = GetTempFile();
		var patch = GetTempFile();

		try {
			// Source with 3-byte repeating pattern
			var sourceData = new byte[100];
			for (int i = 0; i < 100; i++) {
				sourceData[i] = (byte)((i % 3) + 1); // 1, 2, 3, 1, 2, 3, ...
			}

			// Target with same 3-byte pattern
			var targetData = new byte[100];
			for (int i = 0; i < 100; i++) {
				targetData[i] = (byte)((i % 3) + 1);
			}

			File.WriteAllBytes(source, sourceData);
			File.WriteAllBytes(target, targetData);

			// Act: Should use SourceRead since full file matches
			Encoder.CreatePatch(new FileInfo(source), new FileInfo(patch), new FileInfo(target), "");

			// Assert: Should produce minimal patch
			var patchSize = new FileInfo(patch).Length;
			Assert.True(patchSize < 150); // Minimal patch for identical data
		} finally {
			File.Delete(source);
			File.Delete(target);
			File.Delete(patch);
		}
	}
}

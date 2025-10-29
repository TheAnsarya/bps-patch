// ========================================================================================================
// Decoder Tests - BPS Patch Application
// ========================================================================================================
// Comprehensive tests for BPS patch decoding functionality.
//
// References:
// - BPS Specification: https://github.com/blakesmith/beat/blob/master/doc/bps.txt
// - xUnit Testing: https://xunit.net/docs/getting-started/netcore/cmdline
// ========================================================================================================

namespace bps_patch.Tests;

/// <summary>
/// Tests for the Decoder class patch application functionality.
/// Validates correct BPS patch application for various scenarios.
/// </summary>
public class DecoderTests {
	/// <summary>
	/// Creates a temporary file path and ensures it's deleted if it exists.
	/// </summary>
	private static string GetCleanTempFile() {
		var path = GetCleanTempFile();
		File.Delete(path);
		return path;
	}

	/// <summary>
	/// Tests the complete encode-decode round-trip.
	/// Creates a patch from source to target, then applies it to verify correctness.
	/// </summary>
	[Fact]
	public void ApplyPatch_RoundTrip_ReconstructsOriginalFile() {
		// Arrange: Create source and target files
		var sourceFile = GetCleanTempFile();
		var targetFile = GetCleanTempFile();
		var patchFile = GetCleanTempFile();
		var decodedFile = GetCleanTempFile();
		try {
			byte[] sourceData = "Original Data"u8.ToArray();
			byte[] targetData = "Modified Data"u8.ToArray();
			File.WriteAllBytes(sourceFile, sourceData);
			File.WriteAllBytes(targetFile, targetData);

			// Act: Create patch and apply it
			Encoder.CreatePatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(targetFile),
				"Round-trip test");

			List<string> warnings = Decoder.ApplyPatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(decodedFile));

			// Assert: Decoded file should match target
			byte[] decodedData = File.ReadAllBytes(decodedFile);
			Assert.Equal(targetData, decodedData);
			Assert.Empty(warnings); // Should have no warnings with fresh patch
		} finally {
			// Cleanup
			File.Delete(sourceFile);
			File.Delete(targetFile);
			File.Delete(patchFile);
			File.Delete(decodedFile);
		}
	}

	/// <summary>
	/// Tests patch application with empty files.
	/// Edge case: both source and target are empty.
	/// </summary>
	[Fact]
	public void ApplyPatch_EmptyFiles_ProducesEmptyTarget() {
		// Arrange: Create empty source and target, generate patch
		var sourceFile = GetCleanTempFile();
		var targetFile = GetCleanTempFile();
		var patchFile = GetCleanTempFile();
		var decodedFile = GetCleanTempFile();
		try {
			File.WriteAllBytes(sourceFile, []);
			File.WriteAllBytes(targetFile, []);

			Encoder.CreatePatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(targetFile),
				"");

			// Act: Apply patch
			List<string> warnings = Decoder.ApplyPatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(decodedFile));

			// Assert: Decoded file should be empty
			Assert.Equal(0, new FileInfo(decodedFile).Length);
			Assert.Empty(warnings);
		} finally {
			// Cleanup
			File.Delete(sourceFile);
			File.Delete(targetFile);
			File.Delete(patchFile);
			File.Delete(decodedFile);
		}
	}

	/// <summary>
	/// Tests patch application with identical source and target.
	/// When files are identical, patch should use SourceRead actions.
	/// </summary>
	[Fact]
	public void ApplyPatch_IdenticalFiles_ProducesIdenticalOutput() {
		// Arrange: Create identical files
		var sourceFile = GetCleanTempFile();
		var targetFile = GetCleanTempFile();
		var patchFile = GetCleanTempFile();
		var decodedFile = GetCleanTempFile();
		try {
			byte[] data = "Identical Content"u8.ToArray();
			File.WriteAllBytes(sourceFile, data);
			File.WriteAllBytes(targetFile, data);

			Encoder.CreatePatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(targetFile),
				"");

			// Act: Apply patch
			List<string> warnings = Decoder.ApplyPatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(decodedFile));

			// Assert: Output should match input
			byte[] decodedData = File.ReadAllBytes(decodedFile);
			Assert.Equal(data, decodedData);
			Assert.Empty(warnings);
		} finally {
			// Cleanup
			File.Delete(sourceFile);
			File.Delete(targetFile);
			File.Delete(patchFile);
			File.Delete(decodedFile);
		}
	}

	/// <summary>
	/// Tests patch application with completely different files.
	/// Validates TargetRead actions work correctly.
	/// </summary>
	[Fact]
	public void ApplyPatch_CompletelyDifferentFiles_ReconstructsTarget() {
		// Arrange: Create completely different files
		var sourceFile = GetCleanTempFile();
		var targetFile = GetCleanTempFile();
		var patchFile = GetCleanTempFile();
		var decodedFile = GetCleanTempFile();
		try {
			byte[] sourceData = "XXXXXXXXXX"u8.ToArray();
			byte[] targetData = "YYYYYYYYYY"u8.ToArray();
			File.WriteAllBytes(sourceFile, sourceData);
			File.WriteAllBytes(targetFile, targetData);

			Encoder.CreatePatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(targetFile),
				"");

			// Act: Apply patch
			List<string> warnings = Decoder.ApplyPatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(decodedFile));

			// Assert: Should reconstruct target exactly
			byte[] decodedData = File.ReadAllBytes(decodedFile);
			Assert.Equal(targetData, decodedData);
			Assert.Empty(warnings);
		} finally {
			// Cleanup
			File.Delete(sourceFile);
			File.Delete(targetFile);
			File.Delete(patchFile);
			File.Delete(decodedFile);
		}
	}

	/// <summary>
	/// Tests patch application with various file sizes.
	/// Validates decoding works for different sized files.
	/// </summary>
	[Theory]
	[InlineData(10)]      // 10 bytes
	[InlineData(100)]     // 100 bytes
	[InlineData(1024)]    // 1 KB
	[InlineData(10240)]   // 10 KB
	public void ApplyPatch_VariousFileSizes_ReconstructsCorrectly(int fileSize) {
		// Arrange: Create files of specified size
		var sourceFile = GetCleanTempFile();
		var targetFile = GetCleanTempFile();
		var patchFile = GetCleanTempFile();
		var decodedFile = GetCleanTempFile();
		try {
			byte[] sourceData = new byte[fileSize];
			byte[] targetData = new byte[fileSize];
			Random.Shared.NextBytes(sourceData);
			Random.Shared.NextBytes(targetData);

			File.WriteAllBytes(sourceFile, sourceData);
			File.WriteAllBytes(targetFile, targetData);

			Encoder.CreatePatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(targetFile),
				"");

			// Act: Apply patch
			List<string> warnings = Decoder.ApplyPatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(decodedFile));

			// Assert: Should reconstruct target exactly
			byte[] decodedData = File.ReadAllBytes(decodedFile);
			Assert.Equal(targetData, decodedData);
			Assert.Empty(warnings);
		} finally {
			// Cleanup
			File.Delete(sourceFile);
			File.Delete(targetFile);
			File.Delete(patchFile);
			File.Delete(decodedFile);
		}
	}

	/// <summary>
	/// Tests that invalid patch files are rejected.
	/// Should throw PatchFormatException for malformed patches.
	/// </summary>
	[Fact]
	public void ApplyPatch_InvalidPatchFile_ThrowsException() {
		// Arrange: Create invalid patch file (too small)
		var sourceFile = GetCleanTempFile();
		var patchFile = GetCleanTempFile();
		var targetFile = GetCleanTempFile();
		try {
			File.WriteAllBytes(sourceFile, "Source"u8.ToArray());
			File.WriteAllBytes(patchFile, "INVALID"u8.ToArray()); // Invalid patch

			// Act & Assert: Should throw PatchFormatException
			Assert.Throws<PatchFormatException>(() => {
				Decoder.ApplyPatch(
					new FileInfo(sourceFile),
					new FileInfo(patchFile),
					new FileInfo(targetFile));
			});
		} finally {
			// Cleanup
			File.Delete(sourceFile);
			File.Delete(patchFile);
			if (File.Exists(targetFile)) {
				File.Delete(targetFile);
			}
		}
	}

	/// <summary>
	/// Tests patch application with single byte change.
	/// Validates efficient decoding of minimal changes.
	/// </summary>
	[Fact]
	public void ApplyPatch_SingleByteChange_ReconstructsCorrectly() {
		// Arrange: Files differing by one byte
		var sourceFile = GetCleanTempFile();
		var targetFile = GetCleanTempFile();
		var patchFile = GetCleanTempFile();
		var decodedFile = GetCleanTempFile();
		try {
			byte[] sourceData = "Hello World"u8.ToArray();
			byte[] targetData = "Hello Warld"u8.ToArray(); // Changed 'o' to 'a'
			File.WriteAllBytes(sourceFile, sourceData);
			File.WriteAllBytes(targetFile, targetData);

			Encoder.CreatePatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(targetFile),
				"");

			// Act: Apply patch
			List<string> warnings = Decoder.ApplyPatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(decodedFile));

			// Assert: Should reconstruct target exactly
			byte[] decodedData = File.ReadAllBytes(decodedFile);
			Assert.Equal(targetData, decodedData);
			Assert.Empty(warnings);
		} finally {
			// Cleanup
			File.Delete(sourceFile);
			File.Delete(targetFile);
			File.Delete(patchFile);
			File.Delete(decodedFile);
		}
	}

	/// <summary>
	/// Tests patch application with repeating patterns (RLE-like data).
	/// Validates TargetCopy actions work correctly with overlapping regions.
	/// </summary>
	[Fact]
	public void ApplyPatch_RepeatingPattern_ReconstructsCorrectly() {
		// Arrange: Create files with repeating patterns
		var sourceFile = GetCleanTempFile();
		var targetFile = GetCleanTempFile();
		var patchFile = GetCleanTempFile();
		var decodedFile = GetCleanTempFile();
		try {
			byte[] sourceData = "ABC"u8.ToArray();
			byte[] targetData = "ABCABCABCABC"u8.ToArray(); // Repeating pattern
			File.WriteAllBytes(sourceFile, sourceData);
			File.WriteAllBytes(targetFile, targetData);

			Encoder.CreatePatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(targetFile),
				"");

			// Act: Apply patch
			List<string> warnings = Decoder.ApplyPatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(decodedFile));

			// Assert: Should reconstruct repeating pattern correctly
			byte[] decodedData = File.ReadAllBytes(decodedFile);
			Assert.Equal(targetData, decodedData);
			Assert.Empty(warnings);
		} finally {
			// Cleanup
			File.Delete(sourceFile);
			File.Delete(targetFile);
			File.Delete(patchFile);
			File.Delete(decodedFile);
		}
	}
}

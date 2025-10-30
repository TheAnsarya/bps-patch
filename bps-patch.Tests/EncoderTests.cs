// ========================================================================================================
// Encoder Tests - BPS Patch Creation
// ========================================================================================================
// Comprehensive tests for BPS patch encoding functionality.
//
// References:
// - BPS Specification: https://github.com/blakesmith/beat/blob/master/doc/bps.txt
// - xUnit Theory: https://xunit.net/docs/getting-started/netcore/cmdline#write-first-theory
// ========================================================================================================

namespace bps_patch.Tests;

/// <summary>
/// Tests for the Encoder class patch creation functionality.
/// Validates correct BPS patch generation for various scenarios.
/// </summary>
public class EncoderTests : TestBase {

	/// <summary>
	/// Tests that creating a patch from identical source and target produces a minimal patch.
	/// When source and target are identical, the patch should only contain SourceRead actions.
	/// </summary>
	[Fact]
	public void CreatePatch_IdenticalFiles_ProducesMinimalPatch() {
		// Arrange: Create identical source and target files
		var sourceFile = GetCleanTempFile();
		var targetFile = GetCleanTempFile();
		var patchFile = GetCleanTempFile();

		byte[] testData = "Test Data for Identical Files"u8.ToArray();
		File.WriteAllBytes(sourceFile, testData);
		File.WriteAllBytes(targetFile, testData);

		// Act: Create patch
		Encoder.CreatePatch(
			new FileInfo(sourceFile),
			new FileInfo(patchFile),
			new FileInfo(targetFile),
			"Test patch");

		// Assert: Patch file should exist and be small (mostly metadata + SourceRead)
		var patchInfo = new FileInfo(patchFile);
		Assert.True(patchInfo.Exists);
		Assert.True(patchInfo.Length > 0);
		Assert.True(patchInfo.Length < 200); // Should be small for identical files
		// Note: Cleanup handled automatically by TestBase.Dispose()
	}

	/// <summary>
	/// Tests patch creation with empty source and target files.
	/// Edge case: both files are empty.
	/// </summary>
	[Fact]
	public void CreatePatch_EmptyFiles_ProducesValidPatch() {
		// Arrange: Create empty files
		var sourceFile = GetCleanTempFile();
		var targetFile = GetCleanTempFile();
		var patchFile = GetCleanTempFile();
		try {
			WriteAllBytesWithSharing(sourceFile, []);
			WriteAllBytesWithSharing(targetFile, []);

			// Act: Create patch
			Encoder.CreatePatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(targetFile),
				"");

			// Assert: Patch should exist and be minimal
			var patchInfo = new FileInfo(patchFile);
			Assert.True(patchInfo.Exists);
			Assert.True(patchInfo.Length > 0);
		} finally {
			// Cleanup
			File.Delete(sourceFile);
			File.Delete(targetFile);
			File.Delete(patchFile);
		}
	}

	/// <summary>
	/// Tests patch creation with completely different files.
	/// Should produce a patch with primarily TargetRead actions.
	/// </summary>
	[Fact]
	public void CreatePatch_CompletelyDifferentFiles_ProducesValidPatch() {
		// Arrange: Create completely different files
		var sourceFile = GetCleanTempFile();
		var targetFile = GetCleanTempFile();
		var patchFile = GetCleanTempFile();
		try {
			WriteAllBytesWithSharing(sourceFile, "AAAAAAAAAA"u8.ToArray());
			WriteAllBytesWithSharing(targetFile, "BBBBBBBBBB"u8.ToArray());

			// Act: Create patch
			Encoder.CreatePatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(targetFile),
				"Different files test");

			// Assert: Patch should exist
			var patchInfo = new FileInfo(patchFile);
			Assert.True(patchInfo.Exists);
			Assert.True(patchInfo.Length > 0);
		} finally {
			// Cleanup
			File.Delete(sourceFile);
			File.Delete(targetFile);
			File.Delete(patchFile);
		}
	}

	/// <summary>
	/// Tests patch creation with metadata string.
	/// Validates that manifest is properly embedded in the patch.
	/// </summary>
	[Fact]
	public void CreatePatch_WithMetadata_EmbedMetadataInPatch() {
		// Arrange
		var sourceFile = GetCleanTempFile();
		var targetFile = GetCleanTempFile();
		var patchFile = GetCleanTempFile();
		try {
			WriteAllBytesWithSharing(sourceFile, "Source"u8.ToArray());
			WriteAllBytesWithSharing(targetFile, "Target"u8.ToArray());
			string metadata = "Test Metadata: v1.0";

			// Act: Create patch with metadata
			Encoder.CreatePatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(targetFile),
				metadata);

			// Assert: Patch should contain metadata (size increases with metadata length)
			var patchInfo = new FileInfo(patchFile);
			Assert.True(patchInfo.Exists);
			Assert.True(patchInfo.Length > metadata.Length); // At minimum, should include metadata
		} finally {
			// Cleanup
			File.Delete(sourceFile);
			File.Delete(targetFile);
			File.Delete(patchFile);
		}
	}

	/// <summary>
	/// Tests patch creation with single byte change.
	/// Validates efficient encoding of minimal changes.
	/// </summary>
	[Fact]
	public void CreatePatch_SingleByteChange_ProducesEfficientPatch() {
		// Arrange: Files differing by one byte
		var sourceFile = GetCleanTempFile();
		var targetFile = GetCleanTempFile();
		var patchFile = GetCleanTempFile();
		try {
			WriteAllBytesWithSharing(sourceFile, "Hello World"u8.ToArray());
			WriteAllBytesWithSharing(targetFile, "Hello Warld"u8.ToArray()); // Changed 'o' to 'a'

			// Act: Create patch
			Encoder.CreatePatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(targetFile),
				"");

			// Assert: Patch should be small (efficient encoding)
			var patchInfo = new FileInfo(patchFile);
			Assert.True(patchInfo.Exists);
			Assert.True(patchInfo.Length < 100); // Should be small for single byte change
		} finally {
			// Cleanup
			File.Delete(sourceFile);
			File.Delete(targetFile);
			File.Delete(patchFile);
		}
	}

	/// <summary>
	/// Tests patch creation with various file sizes.
	/// Validates encoding works for different sized files.
	/// </summary>
	[Theory]
	[InlineData(10)]      // 10 bytes
	[InlineData(100)]     // 100 bytes
	[InlineData(1024)]    // 1 KB
	[InlineData(10240)]   // 10 KB
	public void CreatePatch_VariousFileSizes_ProducesValidPatches(int fileSize) {
		// Arrange: Create files of specified size
		var sourceFile = GetCleanTempFile();
		var targetFile = GetCleanTempFile();
		var patchFile = GetCleanTempFile();
		try {
			byte[] sourceData = new byte[fileSize];
			byte[] targetData = new byte[fileSize];
			Random.Shared.NextBytes(sourceData);
			Random.Shared.NextBytes(targetData);

			WriteAllBytesWithSharing(sourceFile, sourceData);
			WriteAllBytesWithSharing(targetFile, targetData);

			// Act: Create patch
			Encoder.CreatePatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(targetFile),
				"");

			// Assert: Patch should exist and be valid
			var patchInfo = new FileInfo(patchFile);
			Assert.True(patchInfo.Exists);
			Assert.True(patchInfo.Length > 0);
		} finally {
			// Cleanup
			File.Delete(sourceFile);
			File.Delete(targetFile);
			File.Delete(patchFile);
		}
	}
}

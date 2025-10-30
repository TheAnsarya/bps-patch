// ========================================================================================================
// Integration Tests - Real-World BPS Patch Scenarios
// ========================================================================================================
// Comprehensive integration tests simulating real ROM hacking scenarios.
// Tests full encode-decode cycles with realistic binary data patterns.
//
// References:
// - BPS Specification: https://github.com/blakesmith/beat/blob/master/doc/bps.txt
// - ROM Hacking: https://www.romhacking.net/
// ========================================================================================================

namespace bps_patch.Tests;

/// <summary>
/// Integration tests for BPS patching with realistic scenarios.
/// Simulates actual ROM hacking use cases like translation patches, bug fixes, and enhancements.
/// </summary>
public class IntegrationTests : TestBase {
	/// <summary>
	/// Tests a realistic ROM translation scenario: replacing ASCII text with UTF-8 text.
	/// Simulates patching a game's dialogue strings.
	/// </summary>
	[Fact]
	public void RealWorld_TranslationPatch_ReplacesTextCorrectly() {
		// Arrange: Simulate a ROM file with ASCII dialogue
		var sourceFile = GetCleanTempFile();
		var targetFile = GetCleanTempFile();
		var patchFile = GetCleanTempFile();
		var outputFile = GetCleanTempFile();

		try {
			// Original ROM: "Hello, World! Welcome to the game."
			byte[] originalRom = new byte[1024];
			byte[] dialogue = "Hello, World! Welcome to the game."u8.ToArray();
			Array.Copy(dialogue, 0, originalRom, 100, dialogue.Length);
			WriteAllBytesWithSharing(sourceFile, originalRom);

			// Translated ROM: "こんにちは！ゲームへようこそ。"
			byte[] translatedRom = new byte[1024];
			byte[] translatedDialogue = "こんにちは！ゲームへようこそ。"u8.ToArray();
			Array.Copy(translatedDialogue, 0, translatedRom, 100, translatedDialogue.Length);
			WriteAllBytesWithSharing(targetFile, translatedRom);

			// Act: Create patch
			Encoder.CreatePatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(targetFile),
				"Japanese Translation v1.0");

			// Apply patch
			var warnings = Decoder.ApplyPatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(outputFile));

			// Assert: Output matches translated ROM
			byte[] output = ReadAllBytesWithSharing(outputFile);
			byte[] expected = ReadAllBytesWithSharing(targetFile);
			Assert.Equal(expected, output);
			Assert.Empty(warnings);
		} finally {
			// Cleanup
			File.Delete(sourceFile);
			File.Delete(targetFile);
			File.Delete(patchFile);
			File.Delete(outputFile);
		}
	}

	/// <summary>
	/// Tests a bug fix patch scenario: correcting specific bytes at known addresses.
	/// Simulates fixing a game-breaking bug in ROM code.
	/// </summary>
	[Fact]
	public void RealWorld_BugFixPatch_FixesSpecificBytes() {
		// Arrange: ROM with a bug (wrong jump address)
		var sourceFile = GetCleanTempFile();
		var targetFile = GetCleanTempFile();
		var patchFile = GetCleanTempFile();
		var outputFile = GetCleanTempFile();

		try {
			// Original ROM: 32KB with a bug at offset 0x4A2C
			byte[] originalRom = new byte[32768];
			Random.Shared.NextBytes(originalRom);
			originalRom[0x4A2C] = 0xFF; // Wrong jump offset (causes crash)
			originalRom[0x4A2D] = 0xFF;
			WriteAllBytesWithSharing(sourceFile, originalRom);

			// Fixed ROM: Same data but corrected jump
			byte[] fixedRom = new byte[32768];
			Array.Copy(originalRom, fixedRom, originalRom.Length);
			fixedRom[0x4A2C] = 0x20; // Correct jump offset
			fixedRom[0x4A2D] = 0x30;
			WriteAllBytesWithSharing(targetFile, fixedRom);

			// Act: Create and apply patch
			Encoder.CreatePatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(targetFile),
				"Bug Fix: Corrects crash in level 3");

			var warnings = Decoder.ApplyPatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(outputFile));

			// Assert: Bug is fixed
			byte[] output = ReadAllBytesWithSharing(outputFile);
			Assert.Equal(0x20, output[0x4A2C]);
			Assert.Equal(0x30, output[0x4A2D]);
			Assert.Empty(warnings);

			// Verify patch file is small (only 2 bytes changed)
			var patchInfo = new FileInfo(patchFile);
			Assert.True(patchInfo.Length < 200); // Should be tiny for 2-byte change
		} finally {
			// Cleanup
			File.Delete(sourceFile);
			File.Delete(targetFile);
			File.Delete(patchFile);
			File.Delete(outputFile);
		}
	}

	/// <summary>
	/// Tests expansion patch: increasing ROM size for new content.
	/// Simulates adding new levels or features to a game.
	/// </summary>
	[Fact]
	public void RealWorld_ExpansionPatch_IncreasesRomSize() {
		// Arrange: 256KB ROM expanding to 512KB
		var sourceFile = GetCleanTempFile();
		var targetFile = GetCleanTempFile();
		var patchFile = GetCleanTempFile();
		var outputFile = GetCleanTempFile();

		try {
			// Original ROM: 256KB
			byte[] originalRom = new byte[256 * 1024];
			Random.Shared.NextBytes(originalRom);
			WriteAllBytesWithSharing(sourceFile, originalRom);

			// Expanded ROM: 512KB with new content
			byte[] expandedRom = new byte[512 * 1024];
			Array.Copy(originalRom, expandedRom, originalRom.Length);
			// New content in expanded area
			for (int i = originalRom.Length; i < expandedRom.Length; i++) {
				expandedRom[i] = (byte)(i % 256);
			}
			WriteAllBytesWithSharing(targetFile, expandedRom);

			// Act: Create and apply patch
			Encoder.CreatePatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(targetFile),
				"ROM Expansion: Adds 256KB for new levels");

			var warnings = Decoder.ApplyPatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(outputFile));

			// Assert: ROM expanded correctly
			byte[] output = ReadAllBytesWithSharing(outputFile);
			Assert.Equal(512 * 1024, output.Length);
			Assert.Equal(expandedRom, output);
			Assert.Empty(warnings);
		} finally {
			// Cleanup
			File.Delete(sourceFile);
			File.Delete(targetFile);
			File.Delete(patchFile);
			File.Delete(outputFile);
		}
	}

	/// <summary>
	/// Tests graphics hack: replacing tile data in ROM.
	/// Simulates changing character sprites or backgrounds.
	/// </summary>
	[Fact]
	public void RealWorld_GraphicsHack_ReplacesTileData() {
		// Arrange: ROM with original tile graphics
		var sourceFile = GetCleanTempFile();
		var targetFile = GetCleanTempFile();
		var patchFile = GetCleanTempFile();
		var outputFile = GetCleanTempFile();

		try {
			// Original ROM: 64KB with tile data at 0x8000
			byte[] originalRom = new byte[65536];
			Random.Shared.NextBytes(originalRom);

			// Original tiles: Simple pattern (8x8 tiles, 16 bytes each)
			for (int i = 0; i < 256; i++) {
				originalRom[0x8000 + i] = 0xAA; // Checkerboard pattern
			}
			WriteAllBytesWithSharing(sourceFile, originalRom);

			// Modified ROM: New tile graphics
			byte[] modifiedRom = new byte[65536];
			Array.Copy(originalRom, modifiedRom, originalRom.Length);

			// New tiles: Different pattern
			for (int i = 0; i < 256; i++) {
				modifiedRom[0x8000 + i] = 0x55; // Inverse checkerboard
			}
			WriteAllBytesWithSharing(targetFile, modifiedRom);

			// Act: Create and apply patch
			Encoder.CreatePatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(targetFile),
				"Graphics Pack: New character sprites");

			var warnings = Decoder.ApplyPatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(outputFile));

			// Assert: Graphics replaced
			byte[] output = ReadAllBytesWithSharing(outputFile);
			for (int i = 0; i < 256; i++) {
				Assert.Equal(0x55, output[0x8000 + i]);
			}
			Assert.Empty(warnings);
		} finally {
			// Cleanup
			File.Delete(sourceFile);
			File.Delete(targetFile);
			File.Delete(patchFile);
			File.Delete(outputFile);
		}
	}

	/// <summary>
	/// Tests music/sound hack: replacing audio data.
	/// Simulates changing background music or sound effects.
	/// </summary>
	[Fact]
	public void RealWorld_MusicHack_ReplacesAudioData() {
		// Arrange: ROM with original music data
		var sourceFile = GetCleanTempFile();
		var targetFile = GetCleanTempFile();
		var patchFile = GetCleanTempFile();
		var outputFile = GetCleanTempFile();

		try {
			// Original ROM: 128KB
			byte[] originalRom = new byte[131072];
			Random.Shared.NextBytes(originalRom);

			// Original music sequence at 0x10000 (4KB)
			for (int i = 0; i < 4096; i++) {
				originalRom[0x10000 + i] = (byte)((i / 16) % 128);
			}
			WriteAllBytesWithSharing(sourceFile, originalRom);

			// Modified ROM: New music sequence
			byte[] modifiedRom = new byte[131072];
			Array.Copy(originalRom, modifiedRom, originalRom.Length);

			// New music: Different pattern
			for (int i = 0; i < 4096; i++) {
				modifiedRom[0x10000 + i] = (byte)((i / 8) % 96);
			}
			WriteAllBytesWithSharing(targetFile, modifiedRom);

			// Act: Create and apply patch
			Encoder.CreatePatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(targetFile),
				"Music Replacement: New soundtrack");

			var warnings = Decoder.ApplyPatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(outputFile));

			// Assert: Music replaced
			byte[] output = ReadAllBytesWithSharing(outputFile);
			byte[] expected = ReadAllBytesWithSharing(targetFile);
			Assert.Equal(expected, output);
			Assert.Empty(warnings);
		} finally {
			// Cleanup
			File.Delete(sourceFile);
			File.Delete(targetFile);
			File.Delete(patchFile);
			File.Delete(outputFile);
		}
	}

	/// <summary>
	/// Tests difficulty hack: modifying enemy stats and level parameters.
	/// Simulates creating a "hard mode" or "kaizo" version.
	/// </summary>
	[Fact]
	public void RealWorld_DifficultyHack_ModifiesGameParameters() {
		// Arrange: ROM with original difficulty parameters
		var sourceFile = GetCleanTempFile();
		var targetFile = GetCleanTempFile();
		var patchFile = GetCleanTempFile();
		var outputFile = GetCleanTempFile();

		try {
			// Original ROM: 32KB
			byte[] originalRom = new byte[32768];
			Random.Shared.NextBytes(originalRom);

			// Enemy stats at various locations
			originalRom[0x2000] = 10; // Enemy 1 HP
			originalRom[0x2001] = 5;  // Enemy 1 Attack
			originalRom[0x2010] = 15; // Enemy 2 HP
			originalRom[0x2011] = 8;  // Enemy 2 Attack
			originalRom[0x3000] = 100; // Player starting HP
			WriteAllBytesWithSharing(sourceFile, originalRom);

			// Hard mode ROM: Increased difficulty
			byte[] hardRom = new byte[32768];
			Array.Copy(originalRom, hardRom, originalRom.Length);

			hardRom[0x2000] = 25; // Enemy 1 HP (2.5x)
			hardRom[0x2001] = 12; // Enemy 1 Attack (2.4x)
			hardRom[0x2010] = 40; // Enemy 2 HP (2.67x)
			hardRom[0x2011] = 20; // Enemy 2 Attack (2.5x)
			hardRom[0x3000] = 50; // Player starting HP (0.5x)
			WriteAllBytesWithSharing(targetFile, hardRom);

			// Act: Create and apply patch
			Encoder.CreatePatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(targetFile),
				"Hard Mode: Increased difficulty");

			var warnings = Decoder.ApplyPatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(outputFile));

			// Assert: Difficulty modified
			byte[] output = ReadAllBytesWithSharing(outputFile);
			Assert.Equal(25, output[0x2000]);
			Assert.Equal(12, output[0x2001]);
			Assert.Equal(40, output[0x2010]);
			Assert.Equal(20, output[0x2011]);
			Assert.Equal(50, output[0x3000]);
			Assert.Empty(warnings);

			// Verify patch is efficient
			var patchInfo = new FileInfo(patchFile);
			Assert.True(patchInfo.Length < 500); // Small patch for scattered changes
		} finally {
			// Cleanup
			File.Delete(sourceFile);
			File.Delete(targetFile);
			File.Delete(patchFile);
			File.Delete(outputFile);
		}
	}

	/// <summary>
	/// Tests complete ROM replacement: total conversion hack.
	/// Simulates a complete game overhaul where most data is different.
	/// </summary>
	[Fact]
	public void RealWorld_TotalConversion_ReplacesEntireRom() {
		// Arrange: Completely different ROMs (same size)
		var sourceFile = GetCleanTempFile();
		var targetFile = GetCleanTempFile();
		var patchFile = GetCleanTempFile();
		var outputFile = GetCleanTempFile();

		try {
			// Original ROM: 16KB of one pattern
			byte[] originalRom = new byte[16384];
			for (int i = 0; i < originalRom.Length; i++) {
				originalRom[i] = (byte)(i % 256);
			}
			WriteAllBytesWithSharing(sourceFile, originalRom);

			// Total conversion: Completely different data
			byte[] convertedRom = new byte[16384];
			Random.Shared.NextBytes(convertedRom);
			WriteAllBytesWithSharing(targetFile, convertedRom);

			// Act: Create and apply patch
			Encoder.CreatePatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(targetFile),
				"Total Conversion: Complete game overhaul");

			var warnings = Decoder.ApplyPatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(outputFile));

			// Assert: Complete replacement
			byte[] output = ReadAllBytesWithSharing(outputFile);
			Assert.Equal(convertedRom, output);
			Assert.Empty(warnings);

			// Patch will be large (most data is TargetRead)
			var patchInfo = new FileInfo(patchFile);
			Assert.True(patchInfo.Length > 1000);
		} finally {
			// Cleanup
			File.Delete(sourceFile);
			File.Delete(targetFile);
			File.Delete(patchFile);
			File.Delete(outputFile);
		}
	}

	/// <summary>
	/// Tests metadata preservation: ensuring patch metadata is readable.
	/// Validates that author info, version, and description are embedded correctly.
	/// </summary>
	[Fact]
	public void RealWorld_MetadataPreservation_StoresAuthorInfo() {
		// Arrange
		var sourceFile = GetCleanTempFile();
		var targetFile = GetCleanTempFile();
		var patchFile = GetCleanTempFile();

		try {
			// Create simple patch with detailed metadata
			byte[] source = "Original Game Data v1.0"u8.ToArray();
			byte[] target = "Modified Game Data v2.0"u8.ToArray();
			WriteAllBytesWithSharing(sourceFile, source);
			WriteAllBytesWithSharing(targetFile, target);

			string metadata = "ROM Hack Name: Super Game DX\n" +
			                 "Author: TheHacker\n" +
			                 "Version: 2.0\n" +
			                 "Release: 2025-10-29\n" +
			                 "Description: Enhanced graphics and gameplay";

			// Act: Create patch with metadata
			Encoder.CreatePatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(targetFile),
				metadata);

			// Assert: Patch file exists and contains data
			var patchInfo = new FileInfo(patchFile);
			Assert.True(patchInfo.Exists);
			Assert.True(patchInfo.Length > metadata.Length);

			// Read patch file to verify it's valid BPS
			byte[] patchData = ReadAllBytesWithSharing(patchFile);
			Assert.Equal((byte)'B', patchData[0]);
			Assert.Equal((byte)'P', patchData[1]);
			Assert.Equal((byte)'S', patchData[2]);
			Assert.Equal((byte)'1', patchData[3]);
		} finally {
			// Cleanup
			File.Delete(sourceFile);
			File.Delete(targetFile);
			File.Delete(patchFile);
		}
	}

	/// <summary>
	/// Tests multiple sequential patches: applying patches in series.
	/// Simulates updating through multiple patch versions (v1.0 -> v1.1 -> v1.2).
	/// </summary>
	[Fact]
	public void RealWorld_SequentialPatches_AppliesMultipleVersions() {
		// Arrange: Three ROM versions
		var v10File = GetCleanTempFile();
		var v11File = GetCleanTempFile();
		var v12File = GetCleanTempFile();
		var patch1File = GetCleanTempFile();
		var patch2File = GetCleanTempFile();
		var temp1File = GetCleanTempFile();
		var temp2File = GetCleanTempFile();

		try {
			// v1.0: Original ROM
			byte[] v10 = new byte[8192];
			Random.Shared.NextBytes(v10);
			v10[100] = 1; // Version marker
			WriteAllBytesWithSharing(v10File, v10);

			// v1.1: First update
			byte[] v11 = new byte[8192];
			Array.Copy(v10, v11, v10.Length);
			v11[100] = 2; // Version marker
			v11[500] = 0xFF; // Bug fix
			WriteAllBytesWithSharing(v11File, v11);

			// v1.2: Second update
			byte[] v12 = new byte[8192];
			Array.Copy(v11, v12, v11.Length);
			v12[100] = 3; // Version marker
			v12[1000] = 0xAA; // Feature addition
			WriteAllBytesWithSharing(v12File, v12);

			// Act: Create patches
			Encoder.CreatePatch(
				new FileInfo(v10File),
				new FileInfo(patch1File),
				new FileInfo(v11File),
				"Update v1.0 -> v1.1");

			Encoder.CreatePatch(
				new FileInfo(v11File),
				new FileInfo(patch2File),
				new FileInfo(v12File),
				"Update v1.1 -> v1.2");

			// Apply patches sequentially
			Decoder.ApplyPatch(
				new FileInfo(v10File),
				new FileInfo(patch1File),
				new FileInfo(temp1File));

			var warnings = Decoder.ApplyPatch(
				new FileInfo(temp1File),
				new FileInfo(patch2File),
				new FileInfo(temp2File));

			// Assert: Final version is correct
			byte[] output = ReadAllBytesWithSharing(temp2File);
			Assert.Equal(3, output[100]);
			Assert.Equal(0xFF, output[500]);
			Assert.Equal(0xAA, output[1000]);
			Assert.Empty(warnings);
		} finally {
			// Cleanup
			File.Delete(v10File);
			File.Delete(v11File);
			File.Delete(v12File);
			File.Delete(patch1File);
			File.Delete(patch2File);
			File.Delete(temp1File);
			File.Delete(temp2File);
		}
	}
}



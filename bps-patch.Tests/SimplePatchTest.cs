namespace bps_patch.Tests;

public class SimplePatchTest : TestBase {
	[Fact]
	public void SimplePatch_SingleChange_Works() {
		var sourceFile = GetCleanTempFile("source");
		var targetFile = GetCleanTempFile("target");
		var patchFile = GetCleanTempFile("patch");
		var outputFile = GetCleanTempFile("output");

		try {
			// Create source: 8KB with marker=1
			byte[] source = new byte[8192];
			Random.Shared.NextBytes(source);
			source[100] = 1;
			WriteAllBytesWithSharing(sourceFile, source);

			// Create target: Same as source but marker=2
			byte[] target = new byte[8192];
			Array.Copy(source, target, source.Length);
			target[100] = 2;
			WriteAllBytesWithSharing(targetFile, target);

			// Verify target file
			byte[] targetCheck = ReadAllBytesWithSharing(targetFile);
			Assert.Equal(2, targetCheck[100]);

			// Create patch
			Encoder.CreatePatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(targetFile),
				"Test");

			// Apply patch
			var warnings = Decoder.ApplyPatch(
				new FileInfo(sourceFile),
				new FileInfo(patchFile),
				new FileInfo(outputFile));

			// Verify output
			byte[] output = ReadAllBytesWithSharing(outputFile);
			Assert.Equal(target.Length, output.Length);
			Assert.Equal(2, output[100]);

			// Check if output matches target exactly
			bool matches = output.SequenceEqual(target);
			if (!matches) {
				int firstDiff = -1;
				for (int i = 0; i < output.Length; i++) {
					if (output[i] != target[i]) {
						firstDiff = i;
						break;
					}
				}
				throw new Exception($"Output doesn't match target. First diff at byte {firstDiff}: expected 0x{target[firstDiff]:X2}, got 0x{output[firstDiff]:X2}");
			}

			Assert.Empty(warnings);
		} finally {
			File.Delete(sourceFile);
			File.Delete(targetFile);
			File.Delete(patchFile);
			File.Delete(outputFile);
		}
	}
}

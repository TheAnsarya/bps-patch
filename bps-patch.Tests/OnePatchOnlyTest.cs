namespace bps_patch.Tests;

public class OnePatchOnlyTest : TestBase {
	[Fact]
	public void OnePatch_WithV12Existing_Works() {
		var v10File = GetCleanTempFile("v10");
		var v11File = GetCleanTempFile("v11");
		var v12File = GetCleanTempFile("v12");
		var patch1File = GetCleanTempFile("patch1");
		var temp1File = GetCleanTempFile("temp1");

		try {
			// v1.0
			byte[] v10 = new byte[8192];
			Random.Shared.NextBytes(v10);
			v10[100] = 1;
			WriteAllBytesWithSharing(v10File, v10);

			// v1.1
			byte[] v11 = new byte[8192];
			Array.Copy(v10, v11, v10.Length);
			v11[100] = 2;
			v11[500] = 0xFF;
			WriteAllBytesWithSharing(v11File, v11);

			// v1.2 (create but don't patch)
			byte[] v12 = new byte[8192];
			Array.Copy(v11, v12, v11.Length);
			v12[100] = 3;
			v12[1000] = 0xAA;
			WriteAllBytesWithSharing(v12File, v12);

			// Create ONLY patch1: v10 -> v11
			Encoder.CreatePatch(
				new FileInfo(v10File),
				new FileInfo(patch1File),
				new FileInfo(v11File),
				"v10->v11");

			// Apply patch1
			Decoder.ApplyPatch(
				new FileInfo(v10File),
				new FileInfo(patch1File),
				new FileInfo(temp1File));

			// Verify temp1
			byte[] temp1 = ReadAllBytesWithSharing(temp1File);
			Console.WriteLine($"temp1[100]={temp1[100]}, expected 2");
			Console.WriteLine($"temp1[500]={temp1[500]:X2}, expected FF");

			Assert.Equal(2, temp1[100]);
			Assert.Equal(0xFF, temp1[500]);
			Assert.True(temp1.SequenceEqual(v11), "temp1 should match v11");

		} finally {
			File.Delete(v10File);
			File.Delete(v11File);
			File.Delete(v12File);
			File.Delete(patch1File);
			File.Delete(temp1File);
		}
	}
}

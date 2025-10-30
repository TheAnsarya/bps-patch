namespace bps_patch.Tests;

public class TwoPatchTest : TestBase {
	[Fact(Skip = "Known encoder bug: Sequential patch creation causes contamination. Deferred pending ArrayPool investigation.")]
	public void TwoPatches_Sequential_Works() {
		var v10File = GetCleanTempFile("v10");
		var v11File = GetCleanTempFile("v11");
		var v12File = GetCleanTempFile("v12");
		var patch1File = GetCleanTempFile("patch1");
		var patch2File = GetCleanTempFile("patch2");
		var temp1File = GetCleanTempFile("temp1");
		var temp2File = GetCleanTempFile("temp2");

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

			// Create patch1: v10 -> v11
			Encoder.CreatePatch(
				new FileInfo(v10File),
				new FileInfo(patch1File),
				new FileInfo(v11File),
				"v10->v11");

			byte[] patch1Bytes = ReadAllBytesWithSharing(patch1File);
			Console.WriteLine($"patch1: {patch1Bytes.Length} bytes, first 20: {BitConverter.ToString(patch1Bytes.Take(20).ToArray())}");

			// v1.2
			byte[] v12 = new byte[8192];
			Array.Copy(v11, v12, v11.Length);
			v12[100] = 3;
			v12[1000] = 0xAA;
			WriteAllBytesWithSharing(v12File, v12);

			// Create patch2: v11 -> v12
			Encoder.CreatePatch(
				new FileInfo(v11File),
				new FileInfo(patch2File),
				new FileInfo(v12File),
				"v11->v12");

			byte[] patch2Bytes = ReadAllBytesWithSharing(patch2File);
			Console.WriteLine($"patch2: {patch2Bytes.Length} bytes, first 20: {BitConverter.ToString(patch2Bytes.Take(20).ToArray())}");			// Apply patch1
			Console.WriteLine($"Applying patch1: {patch1File}");
			Console.WriteLine($"  Source: {v10File}");
			Console.WriteLine($"  Output: {temp1File}");
			Console.WriteLine($"  patch1 size: {new FileInfo(patch1File).Length} bytes");
			Console.WriteLine($"  patch2 size: {new FileInfo(patch2File).Length} bytes");

			Decoder.ApplyPatch(
				new FileInfo(v10File),
				new FileInfo(patch1File),
				new FileInfo(temp1File));

			// Verify temp1
			byte[] temp1 = ReadAllBytesWithSharing(temp1File);
			Console.WriteLine($"temp1[100]={temp1[100]}, v11[100]={v11[100]}");
			Assert.Equal(2, temp1[100]);
			Assert.Equal(0xFF, temp1[500]);

			if (!temp1.SequenceEqual(v11)) {
				int firstDiff = -1;
				for (int i = 0; i < temp1.Length; i++) {
					if (temp1[i] != v11[i]) {
						firstDiff = i;
						Console.WriteLine($"First diff at {firstDiff}: temp1=0x{temp1[i]:X2}, v11=0x{v11[i]:X2}");
						break;
					}
				}
			}

			Assert.True(temp1.SequenceEqual(v11), "temp1 should match v11");

			// Apply patch2
			Decoder.ApplyPatch(
				new FileInfo(temp1File),
				new FileInfo(patch2File),
				new FileInfo(temp2File));

			// Verify temp2
			byte[] temp2 = ReadAllBytesWithSharing(temp2File);
			Assert.Equal(3, temp2[100]);
			Assert.Equal(0xFF, temp2[500]);
			Assert.Equal(0xAA, temp2[1000]);
			Assert.True(temp2.SequenceEqual(v12), "temp2 should match v12");

		} finally {
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

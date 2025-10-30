using System;
using System.IO;

namespace bps_patch;

public static class DebugPatch {
	public static void DebugSequentialPatch() {
		string tempDir = Path.Combine(Path.GetTempPath(), "bps-debug-" + Guid.NewGuid().ToString()[..8]);
		Directory.CreateDirectory(tempDir);

		string v10File = Path.Combine(tempDir, "v10.bin");
		string v11File = Path.Combine(tempDir, "v11.bin");
		string v12File = Path.Combine(tempDir, "v12.bin");
		string patch1File = Path.Combine(tempDir, "patch1.bps");
		string patch2File = Path.Combine(tempDir, "patch2.bps");
		string temp1File = Path.Combine(tempDir, "temp1.bin");

		try {
			// v1.0: Original ROM
			byte[] v10 = new byte[8192];
			Random.Shared.NextBytes(v10);
			v10[100] = 1; // Version marker
			File.WriteAllBytes(v10File, v10);
			Console.WriteLine($"Created v1.0: byte[100]={v10[100]}");

			// v1.1: First update
			byte[] v11 = new byte[8192];
			Array.Copy(v10, v11, v10.Length);
			v11[100] = 2; // Version marker
			v11[500] = 0xFF; // Bug fix
			File.WriteAllBytes(v11File, v11);
			Console.WriteLine($"Created v1.1: byte[100]={v11[100]}, byte[500]={v11[500]:X2}");

			// v1.2: Second update
			byte[] v12 = new byte[8192];
			Array.Copy(v11, v12, v11.Length);
			v12[100] = 3; // Version marker
			v12[1000] = 0xAA; // Feature addition
			File.WriteAllBytes(v12File, v12);
			Console.WriteLine($"Created v1.2: byte[100]={v12[100]}, byte[500]={v12[500]:X2}, byte[1000]={v12[1000]:X2}");

			// Verify files before patching
			byte[] v10Check = File.ReadAllBytes(v10File);
			byte[] v11Check = File.ReadAllBytes(v11File);
			byte[] v12Check = File.ReadAllBytes(v12File);
			Console.WriteLine($"\nPre-patch verification:");
			Console.WriteLine($"  v10: byte[100]={v10Check[100]} (expected 1)");
			Console.WriteLine($"  v11: byte[100]={v11Check[100]}, byte[500]={v11Check[500]:X2} (expected 2, FF)");
			Console.WriteLine($"  v12: byte[100]={v12Check[100]}, byte[500]={v12Check[500]:X2}, byte[1000]={v12Check[1000]:X2} (expected 3, FF, AA)");

			// Create patch1: v1.0 -> v1.1
			Console.WriteLine($"\nCreating patch1 (v1.0 -> v1.1)...");
			Encoder.CreatePatch(
				new FileInfo(v10File),
				new FileInfo(patch1File),
				new FileInfo(v11File),
				"Update v1.0 -> v1.1");

			var patch1Info = new FileInfo(patch1File);
			Console.WriteLine($"Patch1 created: {patch1Info.Length} bytes");

			// Read target file again to verify
			byte[] v11AfterPatchCreate = File.ReadAllBytes(v11File);
			Console.WriteLine($"v11 after patch creation: byte[100]={v11AfterPatchCreate[100]}, byte[500]={v11AfterPatchCreate[500]:X2}");

			// Apply patch1
			Console.WriteLine($"\nApplying patch1 to v1.0...");
			var warnings1 = Decoder.ApplyPatch(
				new FileInfo(v10File),
				new FileInfo(patch1File),
				new FileInfo(temp1File));

			Console.WriteLine($"Warnings: {warnings1.Count}");
			foreach (var warning in warnings1) {
				Console.WriteLine($"  - {warning}");
			}

			// Verify result
			byte[] temp1Data = File.ReadAllBytes(temp1File);
			byte[] v11Final = File.ReadAllBytes(v11File);

			Console.WriteLine($"\nResult verification:");
			Console.WriteLine($"  temp1: byte[100]={temp1Data[100]}, byte[500]={temp1Data[500]:X2}");
			Console.WriteLine($"  v11:   byte[100]={v11Final[100]}, byte[500]={v11Final[500]:X2}");

			if (temp1Data[100] != v11Final[100]) {
				Console.WriteLine($"\n*** ERROR: temp1[100]={temp1Data[100]} != v11[100]={v11Final[100]}");

				// Find all differences
				int diffCount = 0;
				for (int i = 0; i < temp1Data.Length && diffCount < 10; i++) {
					if (temp1Data[i] != v11Final[i]) {
						Console.WriteLine($"  Diff at [{i}]: temp1={temp1Data[i]:X2}, v11={v11Final[i]:X2}");
						diffCount++;
					}
				}
			} else {
				Console.WriteLine($"\n*** SUCCESS: temp1 matches v11!");
			}

		} finally {
			// Cleanup
			if (Directory.Exists(tempDir)) {
				Directory.Delete(tempDir, true);
			}
		}
	}
}

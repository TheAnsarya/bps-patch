using System;
using System.IO;

namespace bps_patch {
	class Program {
		static void Main(string[] args) {
			TestDecoder();
		}

		static void TestDecoder() {
			var source = new FileInfo(@"C:\working\patch\Final Fantasy II (U) (V1.1).smc");
			var patch = new FileInfo(@"C:\working\patch\from beat.bps");
			var target = new FileInfo(@"C:\working\patch\decode test.smc");

			Decoder.ApplyPatch(source, patch, target);
		}
	}
}

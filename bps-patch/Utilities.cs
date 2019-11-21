using Force.Crc32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace bps_patch {
	class Utilities {
		// CRC32 of data + 4 bytes of CRC32 at end always results in this number
		public const uint CRC32_RESULT_CONSTANT = 0x2144df1c;

		public static uint ComputeCRC32(FileInfo sourceFile) {
			using var source = sourceFile.OpenRead();

			var crc32 = new Crc32Algorithm();
			var hashBytes = crc32.ComputeHash(source);

			uint hash = hashBytes[0] + ((uint)hashBytes[1] << 8) + ((uint)hashBytes[2] << 16) + ((uint)hashBytes[3] << 24);

			return hash;
		}
		public static byte[] ComputeCRC32Bytes(FileInfo sourceFile) {
			using var source = sourceFile.OpenRead();

			var crc32 = new Crc32Algorithm();
			var hashBytes = crc32.ComputeHash(source);

			return hashBytes;
		}
	}
}

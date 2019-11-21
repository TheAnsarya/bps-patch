using Force.Crc32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace bps_patch {
	class Decoder {
		public const int MIN_PATCH_SIZE = 19;

		// Returns list of warning messages, if any
		public static List<string> ApplyPatch(FileInfo sourceFile, FileInfo patchFile, FileInfo targetFile) {
			// Check patch size
			if (patchFile.Length < MIN_PATCH_SIZE) {
				throw new PatchFormatException("beat size mismatch");
			}

			// Get streams for the source and patch
			using var source = sourceFile.OpenRead();
			using var patch = patchFile.OpenRead();
			
			// Verify patch header and version to make sure this is a BPS version 1 patch file
			if ((patch.ReadByte() != 'B') || (patch.ReadByte() != 'P') || (patch.ReadByte() != 'S')) {
				throw new PatchFormatException("beat header invalid");
			}
			if (patch.ReadByte() != '1') {
				throw new PatchFormatException("beat version mismatch");
			}

			// Local function to read patch data
			byte readPatch() {
				// Read the next patch byte
				int read = patch.ReadByte();

				// TODO: Is there a way to get rid of this check?
				if (read == -1) {
					throw new PatchFormatException("hit end of patch file unexpectedly");
				}

				return (byte)read;
			}

			// Local function to decode patch data
			ulong decodePatch() {
				ulong data = 0, shift = 1;

				while (true) {
					var x = readPatch();

					// TODO: document what this does
					data += (ulong)(x & 0x7f) * shift;
					if ((x & 0x80) != 0) {
						break;
					}

					shift <<= 7;
					data += shift;
				}
				return data;
			}

			if ((long)decodePatch() != sourceFile.Length) {
				throw new ArgumentException($"{nameof(sourceFile)} - source size mismatch");
			}

			// TODO: check against output
			uint targetSize = (uint)decodePatch();

			// Target is created in memory since reading from the file as it is written
			// can causes issues due to unflushed data
			var targetData = new byte[targetSize];
			using var target = new MemoryStream(targetData, true);


			// Fetch manifest from patch
			// TODO: make sure int is appropriate size (should be) as original is uint
			int metadataSize = (int)decodePatch();
			var metadata = new byte[metadataSize];
			patch.Read(metadata.AsSpan());
			var manifest = Encoding.UTF8.GetString(metadata);

			// The last 12 bytes of the patch are hashes
			var readUntil = patchFile.Length - 12;
			var patchPos = patch.Position;
			patch.Position = readUntil;
			uint sourceHash = readPatch() + ((uint)readPatch() << 8) + ((uint)readPatch() << 16) + ((uint)readPatch() << 24);
			uint targetHash = readPatch() + ((uint)readPatch() << 8) + ((uint)readPatch() << 16) + ((uint)readPatch() << 24);
			uint patchHash = readPatch() + ((uint)readPatch() << 8) + ((uint)readPatch() << 16) + ((uint)readPatch() << 24);
			patch.Position = patchPos;

			// sourceRelativeOffset was uint
			long sourceRelativeOffset = 0, targetRelativeOffset = 0;
			while (patch.Position < readUntil) {
				int length = (int)decodePatch();
				var mode = (PatchAction)(length & 3);
				length = (length >> 2) + 1;

				if (mode == PatchAction.SourceRead) {
					// Copy unchanged data from the source file
					source.Position = target.Position;
					while (length-- > 0) {
						var read = source.ReadByte();
						if (read == -1) {
							throw new Exception("hit end of source file unexpectedly");
						}
						target.WriteByte((byte)read);
					}
				} else if (mode == PatchAction.TargetRead) {
					// Copy from the patch file
					patch.Read(targetData.AsSpan().Slice((int)target.Position, length));
					target.Position += length;
				} else {
					int offset = (int)decodePatch();
					offset = ((offset & 1) != 0) ? -(offset >> 1) : (offset >> 1);
					if (mode == PatchAction.SourceCopy) {
						// Copy from another part of the source file
						sourceRelativeOffset += offset;
						source.Position = sourceRelativeOffset;
						source.Read(targetData.AsSpan().Slice((int)target.Position, (int)target.Position + length));
						target.Position += length;
						sourceRelativeOffset += length;
					} else {
						// Copy from another part of the target file
						targetRelativeOffset += offset;
						var targetReader = targetData.AsSpan().Slice((int)targetRelativeOffset, length);
						targetRelativeOffset += length;
						// Have to copy by single bytes as the source read may only be one byte behind the write
						for (int i = 0; i < length; i++){
							target.WriteByte(targetReader[i]);
						}
					}
				}
			}

			using var targetWriter = targetFile.OpenWrite();
			targetWriter.Write(targetData.AsSpan());
			targetWriter.Close();

			// Check possible problems
			var warnings = new List<string>();
			if (Utilities.ComputeCRC32(patchFile) != Utilities.CRC32_RESULT_CONSTANT) {
				warnings.Add($"{nameof(patchFile)} hash mismatch");
			}
			if (Utilities.ComputeCRC32(sourceFile) != sourceHash) {
				warnings.Add($"{nameof(sourceFile)} hash mismatch");
			}
			if (targetFile.Length != targetSize) {
				warnings.Add($"{nameof(targetFile)} size mismatch");
			}
			if (Utilities.ComputeCRC32(targetFile) != targetHash) {
				warnings.Add($"{nameof(targetFile)} hash mismatch");
			}

			return warnings;
		}
	} 
}

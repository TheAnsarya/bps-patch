using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace bps_patch {
	class Encoder {
		// Returns list of warning messages, if any
		public static List<string> CreatePatch(FileInfo sourceFile, FileInfo patchFile, FileInfo targetFile) {
			if (targetFile.Length == 0) {
				throw new ArgumentException($"{nameof(targetFile)} is zero bytes");
			}

			using var source = sourceFile.OpenRead();
			using var target = targetFile.OpenRead();
			using var targetReader = targetFile.OpenRead();
			using var patch = patchFile.OpenRead();

			// TODO: write the patch header and stuff

			long targetReadLength = 0;
			long targetReadStart = -1;

			while (target.Position < target.Length) {
				(PatchAction mode, long length, long start) = FindNextRun(source, target, targetReader);
				if (mode == PatchAction.TargetRead) {
					targetReadLength++;
					if (targetReadStart == -1) {
						targetReadStart = start;
					}
				} else {
					WriteTargetReadCommand();
					var command = EncodeNumber((ulong)(((targetReadLength - 1) << 2) + ((byte)mode)));
					patch.Write(command);
					var offset = start - target.Position;
					var negative = (offset < 0) ? 1 : 0;
					var offsetBytes = EncodeNumber((ulong)((offset << 1) + negative));
					target.Position += length;
					patch.Write(command);
				}
			}

			WriteTargetReadCommand();
			
			// TOSO: write hashes

			void WriteTargetReadCommand() {
				if (targetReadLength > 0) {
					var command = EncodeNumber((ulong)(((targetReadLength - 1) << 2) + ((byte)PatchAction.TargetRead)));
					patch.Write(command);
					target.Position = targetReadStart;

					while (targetReadLength > 0) {
						var targetByte = target.ReadByte();

						if (targetByte == -1) {
							throw new Exception($"{nameof(target)} is at end of file while copying run");
						}

						patch.WriteByte((byte)targetByte);
						targetReadLength--;
					}
				}
			}
		}

		public static byte[] EncodeNumber(ulong number) {
			var output = new List<byte>();
			while (true) {
				byte x = (byte)(number & 0x7f);
				number >>= 7;
				if (number == 0) {
					output.Add((byte)(0x80 | x));
					return output.ToArray();
				}
				output.Add(x);
				number--;
			}
		}

		public static (PatchAction Mode, long Length, long Start) FindNextRun(FileStream source, FileStream target, FileStream targetReader) {
			PatchAction mode = PatchAction.TargetRead;
			long longestRun = 3;
			long longestStart = -1;

			// Check For Source Read
			if (target.Position < source.Length) {
				(long length, bool reachedEnd) = CheckRun(source, target, target.Position);
				if (length > longestRun) {
					mode = PatchAction.SourceRead;
					longestRun = length;

					if (reachedEnd) {
						return (mode, longestRun, -1);
					}
				}
			}

			// Check for Source Copy
			{
				(long length, long start, bool reachedEnd) = FindBestRun(source, target, longestRun + 1);

				if (length > longestRun) {
					mode = PatchAction.SourceCopy;
					longestRun = length;
					longestStart = start;

					if (reachedEnd) {
						return (mode, longestRun, start);
					}
				}
			}

			// Check for Target Copy
			{
				(long length, long start, bool reachedEnd) = FindBestRun(targetReader, target, longestRun + 1, target.Position);

				if (length > longestRun) {
					mode = PatchAction.TargetCopy;
					longestRun = length;
					longestStart = start;

					if (reachedEnd) {
						return (mode, longestRun, start);
					}
				}
			}

			return (mode, longestRun, longestStart);
		}

		public static (long Length, long Start, bool ReachedEnd) FindBestRun(FileStream source, FileStream target, long minimumLongestRun = 4, long checkUntilMax = -1) {
			source.Position = 0;
			var checkUntil = (checkUntilMax == -1) ? (source.Length - minimumLongestRun) : Math.Min(checkUntilMax, (source.Length - minimumLongestRun));

			long currentStart = 0;
			long longestRun = 0;
			long longestStart = -1;

			// TODO: is checkUntil correct? check for off by one
			while (currentStart < checkUntil) {
				(long length, bool reachedEnd) = CheckRun(source, target, currentStart);
				if (length > longestRun) {
					longestRun = length;
					longestStart = currentStart;
					checkUntil = Math.Min(checkUntil, (source.Length - longestRun));
				}
				if (reachedEnd) {
					return (longestRun, longestStart, true);
				}

				currentStart++;
			}

			if (longestRun >= minimumLongestRun) {
				return (longestRun, longestStart, false);
			}

			// Failure to find a good run
			return (0, -1, false);
		}

		public static (long Length, bool ReachedEnd) CheckRun(FileStream source, FileStream target, long sourceStart) {
			if (sourceStart >= source.Length) {
				throw new ArgumentException($"{nameof(sourceStart)} is after end of {nameof(source)} data");
			}

			var originalTargetPosition = target.Position;
			source.Position = sourceStart;
			long length = 0;
			bool reachedEnd = false;

			int sourceByte = source.ReadByte();
			if (sourceByte == -1) {
				// Should never happen since we checked the length
				throw new Exception($"{nameof(source)} is at end of file");
			}

			int targetByte = target.ReadByte();
			if (targetByte == -1) {
				throw new Exception($"{nameof(target)} is at end of file - no data to find a run for");
			}

			if (targetByte == sourceByte) {
				do {
					length++;
					sourceByte = source.ReadByte();
					targetByte = target.ReadByte();
				} while ((sourceByte != -1) && (targetByte != -1) && (sourceByte == targetByte));
			}

			target.Position = originalTargetPosition;
			return (length, reachedEnd);
		}
	}
}

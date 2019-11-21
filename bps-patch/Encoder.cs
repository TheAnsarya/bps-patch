using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace bps_patch {
	class Encoder {
		// Returns list of warning messages, if any
		public static void CreatePatch(FileInfo sourceFile, FileInfo patchFile, FileInfo targetFile, string manifest) {
			if (targetFile.Length == 0) {
				throw new ArgumentException($"{nameof(targetFile)} is zero bytes");
			}
			if (targetFile.Length > int.MaxValue) {
				throw new ArgumentException($"{nameof(targetFile)} is larger than maximum size of {int.MaxValue} bytes");
			}
			if (sourceFile.Length > int.MaxValue) {
				throw new ArgumentException($"{nameof(sourceFile)} is larger than maximum size of {int.MaxValue} bytes");
			}

			// Read source and target into memory
			// WARNING: Large files obviously will use large amounts of memory
			var sourceData = new byte[sourceFile.Length];
			var source = new ReadOnlySpan<byte>(sourceData);
			using (var sourceStream = sourceFile.OpenRead()) {
				sourceStream.Read(sourceData, 0, sourceData.Length);
			}
			var targetData = new byte[targetFile.Length];
			var target = new ReadOnlySpan<byte>(targetData);
			using (var targetStream = targetFile.OpenRead()) {
				targetStream.Read(targetData, 0, sourceData.Length);
			}

			// Patch is directly written to file
			using var patch = patchFile.OpenWrite();

			// Write patch header
			patch.Write(Encoding.UTF8.GetBytes("BPS1"));
			patch.Write(EncodeNumber((ulong)sourceFile.Length));
			patch.Write(EncodeNumber((ulong)targetFile.Length));
			patch.Write(EncodeNumber((ulong)manifest.Length));
			// NOTE: Officially, manifest/metadata should be XML version 1.0 encoding UTF-8 data
			// but could be anything so this might read as garbage or error
			patch.Write(Encoding.UTF8.GetBytes(manifest));

			int targetReadLength = 0;
			int targetReadStart = -1;
			int targetPosition = 0;
			while (targetPosition < target.Length) {
				(PatchAction mode, int length, int start) = FindNextRun(source, target, targetPosition);
				if (mode == PatchAction.TargetRead) {
					targetReadLength++;
					if (targetReadStart == -1) {
						targetReadStart = start;
					}
				} else {
					WriteTargetReadCommand(target);
					var command = EncodeNumber((ulong)(((length - 1) << 2) + ((byte)mode)));
					patch.Write(command);

					// SourceCopy and TargetCopy have an offset
					if (mode != PatchAction.SourceRead) {
						var offset = start - targetPosition;
						var isNegative = offset < 0;
						var offsetValue = ((ulong)Math.Abs(offset) << 1) + (isNegative ? 1UL : 0);
						var offsetBytes = EncodeNumber(offsetValue);
						patch.Write(offsetBytes);
					}

					targetPosition += length;
				}
			}

			WriteTargetReadCommand(target);
			patch.Flush();

			// Write file hashes
			patch.Write(Utilities.ComputeCRC32Bytes(sourceFile));
			patch.Write(Utilities.ComputeCRC32Bytes(targetFile));
			patch.Write(Utilities.ComputeCRC32Bytes(patchFile));

			void WriteTargetReadCommand(ReadOnlySpan<byte> target) {
				if (targetReadLength > 0) {
					var command = EncodeNumber((ulong)(((targetReadLength - 1) << 2) + ((byte)PatchAction.TargetRead)));
					patch.Write(command);
					patch.Write(target.Slice((int)targetReadStart, (int)targetReadLength));
					targetPosition += targetReadLength;
					targetReadLength = 0;
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

		public static (PatchAction Mode, int Length, int Start) FindNextRun(ReadOnlySpan<byte> source, ReadOnlySpan<byte> target, int targetPosition) {
			PatchAction mode = PatchAction.TargetRead;
			int longestRun = 3;
			int longestStart = -1;

			// Check For Source Read
			if (targetPosition < source.Length) {
				(int length, bool reachedEnd) = CheckRun(source.Slice(targetPosition), target.Slice(targetPosition));
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
				(int length, int start, bool reachedEnd) = FindBestRun(source, target.Slice(targetPosition), longestRun + 1);

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
				(int length, int start, bool reachedEnd) = FindBestRun(target, target.Slice(targetPosition), longestRun + 1);

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

		public static (int Length, int Start, bool ReachedEnd) FindBestRun(ReadOnlySpan<byte> source, ReadOnlySpan<byte> target, int minimumLongestRun = 4, int checkUntilMax = -1) {
			var checkUntil = (checkUntilMax == -1) ? (source.Length - minimumLongestRun) : Math.Min(checkUntilMax, (source.Length - minimumLongestRun));

			int currentStart = 0;
			int longestRun = 0;
			int longestStart = -1;

			// TODO: is checkUntil correct? check for off by one
			while (currentStart < checkUntil) {
				(int length, bool reachedEnd) = CheckRun(source.Slice(currentStart), target);
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

			// Found a new best run
			if (longestRun >= minimumLongestRun) {
				return (longestRun, longestStart, false);
			}

			// Failure to find a good run
			return (0, -1, false);
		}

		public static (int Length, bool ReachedEnd) CheckRun(ReadOnlySpan<byte> source, ReadOnlySpan<byte> target) {
			int length = 0;
			bool reachedEnd = false;

			while (source[length] == target[length]) {
				length++;

				// No sense to continue if we're out of target data
				if (target.Length >= length) {
					reachedEnd = true;
					break;
				}

				// Out of source data
				if (source.Length >= length) {
					break;
				}
			}

			return (length, reachedEnd);
		}
	}
}

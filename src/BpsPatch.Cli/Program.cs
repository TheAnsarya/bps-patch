// ========================================================================================================
// BPS Patch CLI - Command Line Interface
// ========================================================================================================
// User-friendly command-line tool for creating and applying BPS patches.
// Uses the BpsPatch.Core library for all patching functionality.
//
// Usage:
//   bps-patch encode <source> <target> <patch> [metadata]
//   bps-patch decode <source> <patch> <target>
//   bps-patch info <patch>
//   bps-patch verify <patch>
// ========================================================================================================

using BpsPatch.Core;

namespace BpsPatch.Cli;

/// <summary>
/// BPS Patch command-line interface.
/// </summary>
public static class Program {
	private const string Version = "1.1.0";

	public static int Main(string[] args) {
		if (args.Length == 0) {
			PrintHelp();
			return 0;
		}

		var command = args[0].ToLowerInvariant();

		try {
			return command switch {
				"encode" or "create" or "e" => HandleEncode(args[1..]),
				"decode" or "apply" or "d" => HandleDecode(args[1..]),
				"info" or "i" => HandleInfo(args[1..]),
				"verify" or "v" => HandleVerify(args[1..]),
				"help" or "-h" or "--help" => PrintHelp(),
				"version" or "-v" or "--version" => PrintVersion(),
				_ => UnknownCommand(command)
			};
		} catch (Exception ex) {
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Error.WriteLine($"Error: {ex.Message}");
			Console.ResetColor();

			if (Environment.GetEnvironmentVariable("BPS_DEBUG") == "1") {
				Console.Error.WriteLine(ex.StackTrace);
			}

			return 1;
		}
	}

	private static int HandleEncode(string[] args) {
		if (args.Length < 3) {
			Console.Error.WriteLine("Usage: bps-patch encode <source> <target> <patch> [options]");
			Console.Error.WriteLine();
			Console.Error.WriteLine("Arguments:");
			Console.Error.WriteLine("  source    Original file");
			Console.Error.WriteLine("  target    Modified file");
			Console.Error.WriteLine("  patch     Output patch file (.bps)");
			Console.Error.WriteLine();
			Console.Error.WriteLine("Options:");
			Console.Error.WriteLine("  -m, --metadata <text>     Patch metadata");
			Console.Error.WriteLine("  -a, --algorithm <name>    Matching algorithm: Auto, Linear, RabinKarp, SuffixArray");
			Console.Error.WriteLine("  -l, --lazy-matching       Enable lazy matching for better compression");
			Console.Error.WriteLine("  -c, --cost-based          Enable cost-based match selection");
			Console.Error.WriteLine("  --no-rle                  Disable RLE optimization");
			Console.Error.WriteLine("  --min-match <n>           Minimum match length (default: 4)");
			return 1;
		}

		// Parse positional arguments
		var positionalArgs = new List<string>();
		var metadata = "";
		var algorithm = MatchingAlgorithm.Auto;
		var useLazyMatching = false;
		var useCostBasedMatching = false;
		var useRleOptimization = true;
		var minMatchLength = 4;

		for (int i = 0; i < args.Length; i++) {
			var arg = args[i];
			if (arg.StartsWith("-")) {
				switch (arg.ToLowerInvariant()) {
					case "-m":
					case "--metadata":
						if (i + 1 < args.Length) {
							metadata = args[++i];
						}

						break;
					case "-a":
					case "--algorithm":
						if (i + 1 < args.Length) {
							var algName = args[++i];
							algorithm = algName.ToLowerInvariant() switch {
								"auto" => MatchingAlgorithm.Auto,
								"linear" => MatchingAlgorithm.Linear,
								"rabinkarp" or "rabin-karp" => MatchingAlgorithm.RabinKarp,
								"suffixarray" or "suffix-array" => MatchingAlgorithm.SuffixArray,
								_ => throw new ArgumentException($"Unknown algorithm: {algName}")
							};
						}
						break;
					case "-l":
					case "--lazy-matching":
						useLazyMatching = true;
						break;
					case "-c":
					case "--cost-based":
						useCostBasedMatching = true;
						break;
					case "--no-rle":
						useRleOptimization = false;
						break;
					case "--min-match":
						if (i + 1 < args.Length && int.TryParse(args[++i], out var minMatch)) {
							minMatchLength = minMatch;
						}

						break;
					default:
						throw new ArgumentException($"Unknown option: {arg}");
				}
			} else {
				positionalArgs.Add(arg);
			}
		}

		if (positionalArgs.Count < 3) {
			Console.Error.WriteLine("Error: source, target, and patch files are required");
			return 1;
		}

		var sourceFile = new FileInfo(positionalArgs[0]);
		var targetFile = new FileInfo(positionalArgs[1]);
		var patchFile = new FileInfo(positionalArgs[2]);

		// Legacy: metadata as 4th positional arg
		if (positionalArgs.Count > 3 && string.IsNullOrEmpty(metadata)) {
			metadata = positionalArgs[3];
		}

		if (!sourceFile.Exists) {
			Console.Error.WriteLine($"Source file not found: {sourceFile.FullName}");
			return 1;
		}

		if (!targetFile.Exists) {
			Console.Error.WriteLine($"Target file not found: {targetFile.FullName}");
			return 1;
		}

		Console.WriteLine($"Creating patch...");
		Console.WriteLine($"  Source: {sourceFile.Name} ({FormatSize(sourceFile.Length)})");
		Console.WriteLine($"  Target: {targetFile.Name} ({FormatSize(targetFile.Length)})");

		var options = new BpsEncoderOptions {
			Algorithm = algorithm,
			UseLazyMatching = useLazyMatching,
			UseCostBasedMatching = useCostBasedMatching,
			UseRleOptimization = useRleOptimization,
			MinimumMatchLength = minMatchLength,
			Progress = new ConsoleProgress("Encoding")
		};

		// Show options if non-default
		if (useLazyMatching || useCostBasedMatching || !useRleOptimization || algorithm != MatchingAlgorithm.Auto) {
			var optionsDesc = new List<string>();
			if (algorithm != MatchingAlgorithm.Auto) {
				optionsDesc.Add($"Algorithm={algorithm}");
			}

			if (useLazyMatching) {
				optionsDesc.Add("LazyMatching");
			}

			if (useCostBasedMatching) {
				optionsDesc.Add("CostBased");
			}

			if (!useRleOptimization) {
				optionsDesc.Add("NoRLE");
			}

			Console.WriteLine($"  Options: {string.Join(", ", optionsDesc)}");
		}

		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		BpsEncoder.CreatePatch(sourceFile, patchFile, targetFile, metadata, options);
		stopwatch.Stop();

		patchFile.Refresh();
		Console.WriteLine();
		Console.WriteLine($"  Patch:  {patchFile.Name} ({FormatSize(patchFile.Length)})");
		Console.WriteLine($"  Time:   {stopwatch.Elapsed.TotalSeconds:F2}s");
		Console.WriteLine();
		Console.ForegroundColor = ConsoleColor.Green;
		Console.WriteLine("✓ Patch created successfully");
		Console.ResetColor();

		return 0;
	}

	private static int HandleDecode(string[] args) {
		if (args.Length < 3) {
			Console.Error.WriteLine("Usage: bps-patch decode <source> <patch> <target>");
			Console.Error.WriteLine();
			Console.Error.WriteLine("Arguments:");
			Console.Error.WriteLine("  source    Original file");
			Console.Error.WriteLine("  patch     Patch file (.bps)");
			Console.Error.WriteLine("  target    Output file to create");
			return 1;
		}

		var sourceFile = new FileInfo(args[0]);
		var patchFile = new FileInfo(args[1]);
		var targetFile = new FileInfo(args[2]);

		if (!sourceFile.Exists) {
			Console.Error.WriteLine($"Source file not found: {sourceFile.FullName}");
			return 1;
		}

		if (!patchFile.Exists) {
			Console.Error.WriteLine($"Patch file not found: {patchFile.FullName}");
			return 1;
		}

		Console.WriteLine($"Applying patch...");
		Console.WriteLine($"  Source: {sourceFile.Name} ({FormatSize(sourceFile.Length)})");
		Console.WriteLine($"  Patch:  {patchFile.Name} ({FormatSize(patchFile.Length)})");

		var options = new BpsDecoderOptions {
			Progress = new ConsoleProgress("Decoding")
		};

		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		var result = BpsDecoder.ApplyPatch(sourceFile, patchFile, targetFile, options);
		stopwatch.Stop();

		targetFile.Refresh();
		Console.WriteLine();
		Console.WriteLine($"  Output: {targetFile.Name} ({FormatSize(targetFile.Length)})");
		Console.WriteLine($"  Time:   {stopwatch.Elapsed.TotalSeconds:F2}s");

		if (!string.IsNullOrEmpty(result.Metadata)) {
			Console.WriteLine($"  Info:   {result.Metadata}");
		}

		if (result.Warnings.Count > 0) {
			Console.WriteLine();
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("⚠ Warnings:");
			foreach (var warning in result.Warnings) {
				Console.WriteLine($"  - {warning}");
			}
			Console.ResetColor();
		}

		Console.WriteLine();
		if (result.Success) {
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("✓ Patch applied successfully");
		} else {
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("✓ Patch applied with warnings");
		}
		Console.ResetColor();

		return result.Success ? 0 : 2;
	}

	private static int HandleInfo(string[] args) {
		if (args.Length < 1) {
			Console.Error.WriteLine("Usage: bps-patch info <patch>");
			return 1;
		}

		var patchFile = new FileInfo(args[0]);

		if (!patchFile.Exists) {
			Console.Error.WriteLine($"Patch file not found: {patchFile.FullName}");
			return 1;
		}

		var info = BpsDecoder.ReadPatchInfo(patchFile);

		Console.WriteLine($"BPS Patch Information");
		Console.WriteLine($"─────────────────────");
		Console.WriteLine($"File:        {patchFile.Name}");
		Console.WriteLine($"Patch Size:  {FormatSize(patchFile.Length)}");
		Console.WriteLine($"Source Size: {FormatSize(info.SourceSize)}");
		Console.WriteLine($"Target Size: {FormatSize(info.TargetSize)}");

		if (!string.IsNullOrEmpty(info.Metadata)) {
			Console.WriteLine($"Metadata:    {info.Metadata}");
		}

		return 0;
	}

	private static int HandleVerify(string[] args) {
		if (args.Length < 1) {
			Console.Error.WriteLine("Usage: bps-patch verify <patch>");
			return 1;
		}

		var patchFile = new FileInfo(args[0]);

		if (!patchFile.Exists) {
			Console.Error.WriteLine($"Patch file not found: {patchFile.FullName}");
			return 1;
		}

		Console.WriteLine($"Verifying patch: {patchFile.Name}");

		bool valid = Crc32Calculator.ValidatePatch(patchFile);

		if (valid) {
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("✓ Patch file is valid (CRC32 OK)");
			Console.ResetColor();
			return 0;
		} else {
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("✗ Patch file is corrupted (CRC32 mismatch)");
			Console.ResetColor();
			return 1;
		}
	}

	private static int PrintHelp() {
		Console.WriteLine($"BPS Patch Tool v{Version}");
		Console.WriteLine("High-performance BPS patch creator and applier");
		Console.WriteLine();
		Console.WriteLine("Usage: bps-patch <command> [arguments]");
		Console.WriteLine();
		Console.WriteLine("Commands:");
		Console.WriteLine("  encode   Create a patch from source and target files");
		Console.WriteLine("  decode   Apply a patch to a source file");
		Console.WriteLine("  info     Display patch file information");
		Console.WriteLine("  verify   Verify patch file integrity");
		Console.WriteLine("  help     Show this help message");
		Console.WriteLine("  version  Show version information");
		Console.WriteLine();
		Console.WriteLine("Examples:");
		Console.WriteLine("  bps-patch encode original.rom modified.rom patch.bps \"My Hack v1.0\"");
		Console.WriteLine("  bps-patch decode original.rom patch.bps patched.rom");
		Console.WriteLine("  bps-patch info patch.bps");
		Console.WriteLine("  bps-patch verify patch.bps");
		Console.WriteLine();
		Console.WriteLine("Documentation: https://github.com/TheAnsarya/bps-patch");
		return 0;
	}

	private static int PrintVersion() {
		Console.WriteLine($"bps-patch {Version}");
		Console.WriteLine($"Runtime: {Environment.Version}");
		Console.WriteLine($"OS: {Environment.OSVersion}");
		return 0;
	}

	private static int UnknownCommand(string command) {
		Console.Error.WriteLine($"Unknown command: {command}");
		Console.Error.WriteLine("Run 'bps-patch help' for usage information.");
		return 1;
	}

	private static string FormatSize(long bytes) {
		string[] suffixes = ["B", "KB", "MB", "GB"];
		int i = 0;
		double size = bytes;

		while (size >= 1024 && i < suffixes.Length - 1) {
			size /= 1024;
			i++;
		}

		return $"{size:F1} {suffixes[i]}";
	}

	/// <summary>
	/// Simple console progress reporter.
	/// </summary>
	private sealed class ConsoleProgress : IProgress<EncodingProgress>, IProgress<DecodingProgress> {
		private readonly string _operation;
		private int _lastPercent = -1;

		public ConsoleProgress(string operation) {
			_operation = operation;
		}

		public void Report(EncodingProgress value) {
			int percent = (int)value.Percentage;
			if (percent != _lastPercent) {
				Console.Write($"\r  {_operation}: {percent}%  ");
				_lastPercent = percent;
			}
		}

		public void Report(DecodingProgress value) {
			int percent = (int)value.Percentage;
			if (percent != _lastPercent) {
				Console.Write($"\r  {_operation}: {percent}%  ");
				_lastPercent = percent;
			}
		}
	}
}

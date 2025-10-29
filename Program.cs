// BPS Patch - Modern .NET 10 Implementation
// For demonstration and testing of the BPS patch format

using System.Text;
using bps_patch;

// Command-line argument parsing
if (args.Length == 0)
{
	Console.WriteLine("BPS Patch Tool - .NET 10");
	Console.WriteLine("Usage:");
	Console.WriteLine("  bps-patch decode <source> <patch> <target>");
	Console.WriteLine("  bps-patch encode <source> <target> <patch> [manifest]");
	Console.WriteLine();
	Console.WriteLine("Running test decoder...");
	TestDecoder();
	return;
}

var command = args[0].ToLowerInvariant();

switch (command)
{
	case "decode" when args.Length >= 4: {
			var source = new FileInfo(args[1]);
			var patch = new FileInfo(args[2]);
			var target = new FileInfo(args[3]);

			Console.WriteLine($"Applying patch: {patch.Name}");
			var warnings = bps_patch.Decoder.ApplyPatch(source, patch, target);

			if (warnings.Count > 0) {
				Console.WriteLine("Warnings:");
				foreach (var warning in warnings) {
					Console.WriteLine($"  - {warning}");
				}
			} else {
				Console.WriteLine("Patch applied successfully!");
			}
		}

		break;

	case "encode" when args.Length >= 4: {
			var source = new FileInfo(args[1]);
			var target = new FileInfo(args[2]);
			var patch = new FileInfo(args[3]);
			var manifest = args.Length > 4 ? args[4] : "";

			Console.WriteLine($"Creating patch: {patch.Name}");
			bps_patch.Encoder.CreatePatch(source, patch, target, manifest);
			Console.WriteLine("Patch created successfully!");
		}

		break;

	default:
		Console.WriteLine("Invalid command or insufficient arguments.");
		Console.WriteLine("Use 'decode' or 'encode' with appropriate file paths.");
		break;
}

static void TestDecoder()
{
	// Example test - update paths as needed
	var source = new FileInfo(@"C:\working\patch\Final Fantasy II (U) (V1.1).smc");
	var patch = new FileInfo(@"C:\working\patch\from beat.bps");
	var target = new FileInfo(@"C:\working\patch\decode test.smc");

	if (!source.Exists || !patch.Exists)
	{
		Console.WriteLine("Test files not found. Update paths in Program.cs to test.");
		return;
	}

	Console.WriteLine($"Source: {source.Name} ({source.Length:N0} bytes)");
	Console.WriteLine($"Patch: {patch.Name} ({patch.Length:N0} bytes)");

	try
	{
		var warnings = bps_patch.Decoder.ApplyPatch(source, patch, target);

		Console.WriteLine($"Target: {target.Name} ({target.Length:N0} bytes)");

		if (warnings.Count > 0)
		{
			Console.WriteLine("\nWarnings:");
			foreach (var warning in warnings)
			{
				Console.WriteLine($"  - {warning}");
			}
		}
		else
		{
			Console.WriteLine("\n✓ Patch applied successfully with no warnings!");
		}
	}
	catch (Exception ex)
	{
		Console.WriteLine($"\n✗ Error: {ex.Message}");
	}
}



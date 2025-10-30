// ========================================================================================================
// BPS Patch Tool - Command-Line Interface
// ========================================================================================================
// Modern .NET 10 implementation using top-level statements for simplified entry point.
// Provides CLI for creating and applying BPS patches, plus a test mode for development.
//
// References:
// - Top-level statements: https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/top-level-statements
// - Command-line args: https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/main-command-line
// ========================================================================================================

using System.Text;
using bps_patch;

// ========================================================================================================
// Command-Line Argument Parsing
// ========================================================================================================
// Top-level statements replace the traditional Main() method in modern C# (C# 9+).
// The 'args' string array is automatically available containing command-line arguments.
//

// Debug mode - run sequential patch debug
if (args.Length == 1 && args[0] == "debug") {
	DebugPatch.DebugSequentialPatch();
	return;
}
// If no arguments are provided, we display usage instructions and run a test decoder
// to validate the implementation with sample files.
// ========================================================================================================
if (args.Length == 0) {
	// No arguments provided - display usage and run test mode
	Console.WriteLine("BPS Patch Tool - .NET 10");
	Console.WriteLine("Usage:");
	Console.WriteLine("  bps-patch decode <source> <patch> <target>");
	Console.WriteLine("  bps-patch encode <source> <target> <patch> [manifest]");
	Console.WriteLine();
	Console.WriteLine("Running test decoder...");

	// Execute test mode with sample files (paths configured in TestDecoder() function)
	TestDecoder();
	return;
}

// ========================================================================================================
// Command Dispatcher
// ========================================================================================================
// Parse the first argument to determine the operation mode (decode or encode).
// Uses pattern matching with 'when' guards (C# 7+) to validate argument count in the switch.
// ========================================================================================================
var command = args[0].ToLowerInvariant(); // Normalize to lowercase for case-insensitive matching

switch (command) {
	// ====================================================================================================
	// DECODE Operation: Apply existing BPS patch to source file
	// ====================================================================================================
	// Command format: bps-patch decode <source> <patch> <target>
	// - source: Original file to patch (e.g., unmodified ROM)
	// - patch: BPS patch file containing transformation instructions
	// - target: Output file to create (reconstructed from source + patch)
	//
	// The decoder validates CRC32 checksums and returns a list of warnings for any issues.
	// ====================================================================================================
	case "decode" when args.Length >= 4: {
			// Parse file paths from command-line arguments
			var source = new FileInfo(args[1]); // Original file
			var patch = new FileInfo(args[2]);  // BPS patch
			var target = new FileInfo(args[3]); // Output file

			Console.WriteLine($"Applying patch: {patch.Name}");

			// Apply the patch using the optimized decoder
			// Returns a list of warnings (e.g., CRC32 mismatches)
			var warnings = bps_patch.Decoder.ApplyPatch(source, patch, target);

			// Display warnings if any were encountered
			// Non-empty warnings indicate potential issues but the patch may still succeed
			if (warnings.Count > 0) {
				Console.WriteLine("Warnings:");
				foreach (var warning in warnings) {
					Console.WriteLine($"  - {warning}");
				}
			} else {
				// No warnings = perfect patch application
				Console.WriteLine("Patch applied successfully!");
			}
		}
		break;

	// ====================================================================================================
	// ENCODE Operation: Create new BPS patch from source and target files
	// ====================================================================================================
	// Command format: bps-patch encode <source> <target> <patch> [manifest]
	// - source: Original file (base for comparison)
	// - target: Modified file (contains desired changes)
	// - patch: Output BPS patch file to create
	// - manifest: Optional metadata string embedded in the patch
	//
	// The encoder analyzes differences between source and target, generates optimized patch actions,
	// and writes a compressed BPS file with CRC32 validation.
	// ====================================================================================================
	case "encode" when args.Length >= 4: {
			// Parse file paths from command-line arguments
			var source = new FileInfo(args[1]); // Original file
			var target = new FileInfo(args[2]); // Modified file
			var patch = new FileInfo(args[3]);  // Output patch

			// Optional manifest (metadata) string - empty string if not provided
			// Manifest is embedded in the BPS file and can contain patch info, version, etc.
			var manifest = args.Length > 4 ? args[4] : "";

			Console.WriteLine($"Creating patch: {patch.Name}");

			// Create the patch using the optimized encoder
			// Uses ArrayPool, buffered I/O, and variable-length encoding for efficiency
			bps_patch.Encoder.CreatePatch(source, patch, target, manifest);

			Console.WriteLine("Patch created successfully!");
		}
		break;

	// ====================================================================================================
	// INVALID Command Handler
	// ====================================================================================================
	// Catch-all for unrecognized commands or insufficient arguments
	// ====================================================================================================
	default:
		Console.WriteLine("Invalid command or insufficient arguments.");
		Console.WriteLine("Use 'decode' or 'encode' with appropriate file paths.");
		break;
}

// ========================================================================================================
// Test Mode: Validate Decoder with Sample Files
// ========================================================================================================
// This function demonstrates the decoder with hardcoded test file paths.
// Used during development and when running without command-line arguments.
//
// To use: Update the file paths below to point to your test files.
// ========================================================================================================
static void TestDecoder() {
	// ====================================================================================================
	// Configure Test File Paths
	// ====================================================================================================
	// Update these paths to match your test environment.
	// The example uses a Final Fantasy II ROM patch for demonstration.
	// ====================================================================================================
	var source = new FileInfo(@"C:\working\patch\Final Fantasy II (U) (V1.1).smc");
	var patch = new FileInfo(@"C:\working\patch\from beat.bps");
	var target = new FileInfo(@"C:\working\patch\decode test.smc");

	// ====================================================================================================
	// Validate Test Files Exist
	// ====================================================================================================
	// FileInfo.Exists checks if the file is present on disk.
	// If test files aren't found, we skip the test and display instructions.
	// ====================================================================================================
	if (!source.Exists || !patch.Exists) {
		Console.WriteLine("Test files not found. Update paths in Program.cs to test.");
		return;
	}

	// ====================================================================================================
	// Display Test File Information
	// ====================================================================================================
	// FileInfo.Length provides file size in bytes.
	// The ":N0" format specifier adds thousand separators (e.g., "1,234,567")
	// Reference: https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings
	// ====================================================================================================
	Console.WriteLine($"Source: {source.Name} ({source.Length:N0} bytes)");
	Console.WriteLine($"Patch: {patch.Name} ({patch.Length:N0} bytes)");

	// ====================================================================================================
	// Execute Patch Application with Error Handling
	// ====================================================================================================
	// Wrapped in try-catch to gracefully handle exceptions (e.g., malformed patch, I/O errors).
	// ====================================================================================================
	try {
		// Apply the patch and capture warnings
		var warnings = bps_patch.Decoder.ApplyPatch(source, patch, target);

		// Display the resulting target file size
		// FileInfo must be refreshed after creation to get updated Length
		target.Refresh();
		Console.WriteLine($"Target: {target.Name} ({target.Length:N0} bytes)");

		// ================================================================================================
		// Display Warnings or Success Message
		// ================================================================================================
		// Warnings are non-fatal (e.g., CRC32 mismatch) but indicate potential issues.
		// An empty warnings list means perfect patch application with validated checksums.
		// ================================================================================================
		if (warnings.Count > 0) {
			Console.WriteLine("\nWarnings:");
			foreach (var warning in warnings) {
				Console.WriteLine($"  - {warning}");
			}
		} else {
			// Unicode checkmark (✓) for visual confirmation of success
			Console.WriteLine("\n✓ Patch applied successfully with no warnings!");
		}
	} catch (Exception ex) {
		// ================================================================================================
		// Error Handling
		// ================================================================================================
		// Catches PatchFormatException (malformed patch), IOException (disk errors), etc.
		// Display exception message for debugging.
		// ================================================================================================
		Console.WriteLine($"\n✗ Error: {ex.Message}");
	}
}

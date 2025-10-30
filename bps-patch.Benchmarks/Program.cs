// ========================================================================================================
// BPS Patch Benchmarks - Program Entry Point
// ========================================================================================================
// BenchmarkDotNet runner for all BPS patch performance benchmarks.
//
// Usage:
//   dotnet run -c Release
//   dotnet run -c Release -- --filter *EncoderBenchmarks*
//
// References:
// - BenchmarkDotNet: https://benchmarkdotnet.org/
// - Performance Best Practices: https://learn.microsoft.com/en-us/dotnet/core/extensions/high-performance-logging
// ========================================================================================================

using BenchmarkDotNet.Running;

namespace bps_patch.Benchmarks;

/// <summary>
/// Entry point for running BPS patch benchmarks.
/// </summary>
public class Program {
	public static void Main(string[] args) {
		// Run all benchmarks in this assembly
		BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
	}
}

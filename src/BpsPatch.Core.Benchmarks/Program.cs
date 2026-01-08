// ========================================================================================================
// BPS Patch Benchmark Suite
// ========================================================================================================
// Performance benchmarks for the BPS patch library using BenchmarkDotNet.
// Run with: dotnet run -c Release
// ========================================================================================================

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Jobs;
using BpsPatch.Core;

BenchmarkRunner.Run<EncoderBenchmarks>();
BenchmarkRunner.Run<DecoderBenchmarks>();
BenchmarkRunner.Run<AlgorithmBenchmarks>();
BenchmarkRunner.Run<VariableLengthIntBenchmarks>();

[MemoryDiagnoser]
[SimpleJob]
public class EncoderBenchmarks {
	private FileInfo _sourceFile = null!;
	private FileInfo _targetFile = null!;
	private FileInfo _patchFile = null!;
	private byte[] _sourceData = null!;
	private byte[] _targetData = null!;
	private string _tempDir = null!;

	[Params(1024, 65536, 1048576)]
	public int FileSize { get; set; }

	[GlobalSetup]
	public void Setup() {
		_tempDir = Path.Combine(Path.GetTempPath(), $"bps_bench_{Guid.NewGuid()}");
		Directory.CreateDirectory(_tempDir);

		_sourceData = new byte[FileSize];
		_targetData = new byte[FileSize];
		new Random(42).NextBytes(_sourceData);

		// Create target that's 90% similar to source
		Array.Copy(_sourceData, _targetData, FileSize);
		var rng = new Random(43);
		for (int i = 0; i < FileSize / 10; i++) {
			_targetData[rng.Next(FileSize)] = (byte)rng.Next(256);
		}

		_sourceFile = new FileInfo(Path.Combine(_tempDir, "source.bin"));
		_targetFile = new FileInfo(Path.Combine(_tempDir, "target.bin"));
		_patchFile = new FileInfo(Path.Combine(_tempDir, "patch.bps"));

		File.WriteAllBytes(_sourceFile.FullName, _sourceData);
		File.WriteAllBytes(_targetFile.FullName, _targetData);
	}

	[GlobalCleanup]
	public void Cleanup() {
		try { Directory.Delete(_tempDir, true); } catch { }
	}

	[Benchmark(Baseline = true)]
	public void Encode_Auto() {
		var options = new BpsEncoderOptions { Algorithm = MatchingAlgorithm.Auto };
		BpsEncoder.CreatePatch(_sourceFile, _patchFile, _targetFile, "", options);
	}

	[Benchmark]
	public void Encode_Linear() {
		var options = new BpsEncoderOptions { Algorithm = MatchingAlgorithm.Linear };
		BpsEncoder.CreatePatch(_sourceFile, _patchFile, _targetFile, "", options);
	}

	[Benchmark]
	public void Encode_RabinKarp() {
		var options = new BpsEncoderOptions { Algorithm = MatchingAlgorithm.RabinKarp };
		BpsEncoder.CreatePatch(_sourceFile, _patchFile, _targetFile, "", options);
	}

	[Benchmark]
	public void Encode_SuffixArray() {
		var options = new BpsEncoderOptions { Algorithm = MatchingAlgorithm.SuffixArray };
		BpsEncoder.CreatePatch(_sourceFile, _patchFile, _targetFile, "", options);
	}
}

[MemoryDiagnoser]
[SimpleJob]
public class DecoderBenchmarks {
	private FileInfo _sourceFile = null!;
	private FileInfo _patchFile = null!;
	private FileInfo _outputFile = null!;
	private string _tempDir = null!;

	[Params(1024, 65536, 1048576)]
	public int FileSize { get; set; }

	[GlobalSetup]
	public void Setup() {
		_tempDir = Path.Combine(Path.GetTempPath(), $"bps_decode_bench_{Guid.NewGuid()}");
		Directory.CreateDirectory(_tempDir);

		var sourceData = new byte[FileSize];
		var targetData = new byte[FileSize];
		new Random(42).NextBytes(sourceData);
		Array.Copy(sourceData, targetData, FileSize);

		// Small differences
		var rng = new Random(43);
		for (int i = 0; i < FileSize / 10; i++) {
			targetData[rng.Next(FileSize)] = (byte)rng.Next(256);
		}

		_sourceFile = new FileInfo(Path.Combine(_tempDir, "source.bin"));
		var targetFile = new FileInfo(Path.Combine(_tempDir, "target.bin"));
		_patchFile = new FileInfo(Path.Combine(_tempDir, "patch.bps"));
		_outputFile = new FileInfo(Path.Combine(_tempDir, "output.bin"));

		File.WriteAllBytes(_sourceFile.FullName, sourceData);
		File.WriteAllBytes(targetFile.FullName, targetData);

		BpsEncoder.CreatePatch(_sourceFile, _patchFile, targetFile, "");
	}

	[GlobalCleanup]
	public void Cleanup() {
		try { Directory.Delete(_tempDir, true); } catch { }
	}

	[Benchmark]
	public void Decode() {
		BpsDecoder.ApplyPatch(_sourceFile, _patchFile, _outputFile);
	}

	[Benchmark]
	public void ReadPatchInfo() {
		BpsDecoder.ReadPatchInfo(_patchFile);
	}
}

[MemoryDiagnoser]
[SimpleJob]
public class AlgorithmBenchmarks {
	private byte[] _data = null!;
	private IMatchingStrategy _linear = null!;
	private IMatchingStrategy _rabinKarp = null!;
	private IMatchingStrategy _suffixArray = null!;

	[Params(1024, 16384, 65536)]
	public int DataSize { get; set; }

	[GlobalSetup]
	public void Setup() {
		_data = new byte[DataSize];
		new Random(42).NextBytes(_data);

		_linear = MatchingStrategyFactory.Create(MatchingAlgorithm.Linear);
		_rabinKarp = MatchingStrategyFactory.Create(MatchingAlgorithm.RabinKarp);
		_suffixArray = MatchingStrategyFactory.Create(MatchingAlgorithm.SuffixArray);
	}

	[Benchmark(Baseline = true)]
	public void LinearSearch() {
		_linear.Prepare(_data);
		for (int i = 0; i < 100; i++) {
			int pos = i * (DataSize / 100);
			_linear.FindBestMatch(_data, _data.AsSpan(pos, Math.Min(100, _data.Length - pos)), 4);
		}
	}

	[Benchmark]
	public void RabinKarpSearch() {
		_rabinKarp.Prepare(_data);
		for (int i = 0; i < 100; i++) {
			int pos = i * (DataSize / 100);
			_rabinKarp.FindBestMatch(_data, _data.AsSpan(pos, Math.Min(100, _data.Length - pos)), 4);
		}
	}

	[Benchmark]
	public void SuffixArraySearch() {
		_suffixArray.Prepare(_data);
		for (int i = 0; i < 100; i++) {
			int pos = i * (DataSize / 100);
			_suffixArray.FindBestMatch(_data, _data.AsSpan(pos, Math.Min(100, _data.Length - pos)), 4);
		}
	}
}

[MemoryDiagnoser]
[SimpleJob]
public class VariableLengthIntBenchmarks {
	private byte[] _buffer = new byte[10];

	[Params(0UL, 127UL, 16383UL, (ulong)int.MaxValue)]
	public ulong Value { get; set; }

	[Benchmark]
	public int Encode() {
		return VariableLengthInt.Encode(Value, _buffer);
	}

	[Benchmark]
	public ulong Decode() {
		int written = VariableLengthInt.Encode(Value, _buffer);
		return VariableLengthInt.Decode(_buffer.AsSpan(0, written), out _);
	}
}

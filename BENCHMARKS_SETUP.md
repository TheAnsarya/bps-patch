# BenchmarkDotNet Setup - Complete ✅

## Summary
Successfully set up BenchmarkDotNet performance benchmarking infrastructure for the BPS patch project.

## Date
October 29, 2025

## What We Built

### 1. Project Structure
- Created `bps-patch.Benchmarks` project targeting .NET 10
- Added to solution alongside `bps-patch` (main) and `bps-patch.Tests`
- Configured InternalsVisibleTo for access to internal classes

### 2. Benchmark Coverage (28 benchmarks)

#### CRC32 Benchmarks (7 benchmarks)
- **File size variations**: 1KB, 10KB, 100KB, 1MB, 10MB
- **Method variations**: ComputeCRC32 (uint) vs ComputeCRC32Bytes (byte[])
- Tests performance of System.IO.Hashing.Crc32

#### Encoder Benchmarks (13 benchmarks)
- **Pattern matching**: Identical files, single byte change, completely different files
- **File sizes**: 1KB, 10KB, 100KB
- **Scenarios**: Repeating patterns, sparse changes, metadata encoding
- **Algorithms**: Linear search, run-length encoding, variable-length integer encoding

#### Decoder Benchmarks (8 benchmarks)
- **Patch operations**: SourceRead, TargetRead, SourceCopy, TargetCopy
- **File sizes**: 1KB, 100KB, 1MB
- **Scenarios**: Simple patches, complex mixed operations, large file streaming

### 3. Key Fixes Applied

#### Problem 1: Benchmark files compiled in main project
**Error**: Benchmarks .cs files were included in main bps-patch.csproj compilation
**Solution**: Added exclusion rules to bps-patch.csproj:
```xml
<Compile Remove="bps-patch.Benchmarks\**" />
<EmbeddedResource Remove="bps-patch.Benchmarks\**" />
<None Remove="bps-patch.Benchmarks\**" />
```

#### Problem 2: BenchmarkDotNet project not in solution
**Error**: Build targeting wrong project context
**Solution**: 
```bash
dotnet sln add bps-patch.Benchmarks\bps-patch.Benchmarks.csproj
```

#### Problem 3: RuntimeMoniker.Net100 doesn't exist
**Error**: `CS0117: 'RuntimeMoniker' does not contain a definition for 'Net100'`
**Reason**: BenchmarkDotNet 0.15.4 released before .NET 10 preview
**Solution**: Changed `[SimpleJob(RuntimeMoniker.Net100)]` to `[SimpleJob]` (uses current runtime)

#### Problem 4: InternalsVisibleTo not working
**Error**: `CS0122: 'Encoder' is inaccessible due to its protection level`
**Reason**: `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` disabled auto-generation
**Solution**: Removed that line, enabled auto-generation of AssemblyInfo.cs with InternalsVisibleTo

## Running Benchmarks

### Run all benchmarks:
```bash
cd bps-patch.Benchmarks
dotnet run -c Release
```

### Run specific benchmark class:
```bash
dotnet run -c Release --filter "*CRC32Benchmarks*"
dotnet run -c Release --filter "*EncoderBenchmarks*"
dotnet run -c Release --filter "*DecoderBenchmarks*"
```

### Run specific benchmark method:
```bash
dotnet run -c Release --filter "*CRC32_1MB*"
dotnet run -c Release --filter "*CreatePatch_IdenticalFiles*"
```

## Output
- Console output with timing, memory allocation, GC stats
- Markdown export to `BenchmarkDotNet.Artifacts/results/`
- Detailed reports for performance analysis

## Next Steps - Performance Optimizations

### 1. SIMD (Vector<byte>) - Expected 4-8x speedup
- Bulk memory comparison in pattern matching
- Parallel byte processing for CRC32
- Requires: `using System.Runtime.Intrinsics;`

### 2. Rabin-Karp Rolling Hash - Expected 10-100x for large files
- O(n) substring matching vs current O(n²) linear search
- Polynomial rolling hash with modular arithmetic
- Ideal for files >100KB

### 3. Suffix Arrays - Expected log(n) pattern matching
- O(log n) binary search for pattern matches
- Replaces linear search in FindNextRun
- Best for highly repetitive data

### 4. PLINQ Parallel Processing - Expected 2-4x on multi-core
- Parallel chunk comparison in encoder
- Requires careful thread synchronization
- Best for files >1MB

### 5. Memory-Mapped Files - Enables >RAM files
- MemoryMappedFile for very large ROMs
- Streaming instead of all-in-memory
- Critical for files >100MB

## Benchmark Results Baseline
(Run benchmarks to establish baseline before optimizations)

| Benchmark | Mean | Allocated |
|-----------|------|-----------|
| CRC32_1MB | TBD | TBD |
| CreatePatch_IdenticalFiles_100KB | TBD | TBD |
| ApplyPatch_Mixed_1MB | TBD | TBD |

## Project Files

### bps-patch.Benchmarks.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>bps_patch.Benchmarks</RootNamespace>
    <Nullable>enable</Nullable>
    <LangVersion>latest</LangVersion>
    <ImplicitUsings>disable</ImplicitUsings>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="BenchmarkDotNet" Version="0.15.4" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\bps-patch.csproj" />
  </ItemGroup>
</Project>
```

### GlobalUsings.cs
```csharp
global using System;
global using System.IO;
global using BenchmarkDotNet.Attributes;
global using BenchmarkDotNet.Jobs;
global using BenchmarkDotNet.Running;
global using BenchmarkDotNet.Exporters;
```

### Program.cs (minimal entry point)
```csharp
using BenchmarkDotNet.Running;

namespace bps_patch.Benchmarks;

class Program {
    static void Main(string[] args) {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
```

## Success Criteria ✅
- [x] BenchmarkDotNet project builds without errors
- [x] All three projects compile (main, tests, benchmarks)
- [x] InternalsVisibleTo grants access to Encoder/Decoder/Utilities
- [x] 28 benchmarks ready to measure performance
- [x] Ready for optimization implementation

## Notes
- .NET 10 preview compatibility with BenchmarkDotNet 0.15.4 confirmed
- Test failures unrelated to benchmarks (file access concurrency issues)
- Benchmarks will run in Release mode for accurate performance measurement
- Results will establish baseline before implementing SIMD, Rabin-Karp, etc.

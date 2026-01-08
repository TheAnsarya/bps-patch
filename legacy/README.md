# Legacy Code

This folder contains the original root-level source files from before the library restructure.

**Status**: Archived - use `src/BpsPatch.Core/` for active development.

## Why This Exists

The original project had all source files at the root level. During the January 2026 restructure, we:

1. Created a proper library structure in `src/`
2. Moved the original files here for reference
3. The root `bps-patch.csproj` still compiles these for backward compatibility

## Files

| File | New Location |
|------|--------------|
| Decoder.cs | src/BpsPatch.Core/BpsDecoder.cs |
| Encoder.cs | src/BpsPatch.Core/BpsEncoder.cs |
| Utilities.cs | src/BpsPatch.Core/Crc32Calculator.cs |
| PatchAction.cs | src/BpsPatch.Core/BpsAction.cs |
| PatchFormatException.cs | src/BpsPatch.Core/BpsFormatException.cs |
| RabinKarp.cs | src/BpsPatch.Core/RabinKarpMatchingStrategy.cs |
| SuffixArray.cs | src/BpsPatch.Core/SuffixArrayMatchingStrategy.cs |
| MemoryMappedFileHelper.cs | (Not migrated - future feature) |
| DebugPatch.cs | (Debug utility - not migrated) |
| GlobalUsings.cs | src/BpsPatch.Core/GlobalUsings.cs |
| Program.cs | src/BpsPatch.Cli/Program.cs |

## Recommendation

For new development, use the library projects:
- **BpsPatch.Core** - The main library
- **BpsPatch.Cli** - Command-line interface
- **BpsPatch.Core.Tests** - Unit tests
- **BpsPatch.Core.Benchmarks** - Performance benchmarks

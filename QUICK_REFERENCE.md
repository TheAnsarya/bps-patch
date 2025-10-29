# Quick Reference - BPS Patch

## Build & Run

```bash
# Restore packages
dotnet restore

# Build (Debug)
dotnet build

# Build (Release)
dotnet build -c Release

# Run with arguments
dotnet run -- decode source.bin patch.bps target.bin

# Run without arguments (test mode)
dotnet run

# Publish as single executable
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true
```

## Common Commands

### Create a Patch
```bash
# Basic
bps-patch encode original.rom modified.rom patch.bps

# With metadata
bps-patch encode original.rom modified.rom patch.bps "My Hack v1.0"
```

### Apply a Patch
```bash
# Basic
bps-patch decode original.rom patch.bps patched.rom

# Verify (check for warnings)
bps-patch decode original.rom patch.bps output.rom
# Look for CRC32 warnings in output
```

## File Paths
Update test paths in `Program.cs`:

```csharp
static void TestDecoder()
{
    var source = new FileInfo(@"C:\roms\source.smc");
    var patch = new FileInfo(@"C:\patches\mypatch.bps");
    var target = new FileInfo(@"C:\output\patched.smc");
    
    // ... rest of code
}
```

## Code Examples

### Using Encoder Programmatically
```csharp
using bps_patch;

var source = new FileInfo("original.bin");
var target = new FileInfo("modified.bin");
var patch = new FileInfo("output.bps");

bps_patch.Encoder.CreatePatch(source, patch, target, "My Patch");
```

### Using Decoder Programmatically
```csharp
using bps_patch;

var source = new FileInfo("original.bin");
var patch = new FileInfo("patch.bps");
var target = new FileInfo("patched.bin");

var warnings = bps_patch.Decoder.ApplyPatch(source, patch, target);

if (warnings.Count > 0)
{
    Console.WriteLine("Warnings:");
    foreach (var warning in warnings)
    {
        Console.WriteLine($"  - {warning}");
    }
}
```

### Computing CRC32
```csharp
using bps_patch;

var file = new FileInfo("myfile.bin");
uint crc32 = Utilities.ComputeCRC32(file);
Console.WriteLine($"CRC32: 0x{crc32:X8}");
```

## Performance Tips

### Large Files
- Ensure sufficient RAM (files loaded into memory)
- Use Release build for optimal performance
- Run on SSD for faster I/O

### Benchmarking
```bash
# Install BenchmarkDotNet
dotnet add package BenchmarkDotNet

# Create benchmark class
# Run with:
dotnet run -c Release --project Benchmarks
```

## Troubleshooting

### Build Errors
```bash
# Clean build artifacts
dotnet clean
rm -rf bin obj

# Restore packages
dotnet restore

# Build
dotnet build
```

### File Not Found
- Check file paths are correct
- Use absolute paths or ensure working directory is correct
- Verify file permissions

### CRC32 Mismatches
- Source file may be different version
- Patch file may be corrupted
- Target file verification failed
- These are warnings, not errors - file still created

### Out of Memory
- File too large (> available RAM)
- Increase system memory
- Consider streaming version (future enhancement)

## Environment Variables

```bash
# Set .NET diagnostics
export DOTNET_EnableEventPipe=1
export DOTNET_EventPipeOutputPath=./diagnostics

# Performance profiling
export COMPlus_PerfMapEnabled=1
export COMPlus_EnableEventLog=1
```

## Development Workflow

1. Make code changes
2. Build: `dotnet build`
3. Test: `dotnet run`
4. Check errors: `dotnet build 2>&1 | grep error`
5. Format: Use VS Code or Rider
6. Commit changes

## Useful Commands

```bash
# List project references
dotnet list package

# Add package
dotnet add package PackageName

# Remove package
dotnet remove package PackageName

# Update all packages
dotnet list package --outdated
dotnet add package PackageName

# Generate NuGet package
dotnet pack -c Release

# Run with specific runtime
dotnet run --runtime win-x64
dotnet run --runtime linux-x64
dotnet run --runtime osx-x64
```

## IDE Setup

### Visual Studio Code
1. Install C# extension
2. Open folder: `bps-patch/bps-patch`
3. Press F5 to debug
4. Edit launch.json for command-line args

### Visual Studio 2022
1. Open `bps-patch.sln`
2. Set command-line args in project properties
3. F5 to debug

### JetBrains Rider
1. Open `bps-patch.sln`
2. Edit run configuration
3. Add program arguments
4. F5 to debug

## Documentation

- **Copilot Instructions**: `.github/copilot-instructions.md`
- **Modernization Log**: `logs/modernization-session-2025-10-28.md`
- **Change Summary**: `MODERNIZATION_SUMMARY.md`
- **This Guide**: `QUICK_REFERENCE.md`

## Support

For issues or questions:
1. Check README.md
2. Review copilot-instructions.md
3. Check logs/ for session notes
4. Open GitHub issue

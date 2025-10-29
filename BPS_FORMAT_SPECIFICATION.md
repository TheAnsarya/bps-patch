# BPS (Binary Patch System) File Format Specification

**Version**: 1.0  
**Date**: October 29, 2025  
**Implementation**: .NET 10 C#  
**Status**: Production

---

## Table of Contents

1. [Overview](#overview)
2. [File Structure](#file-structure)
3. [Variable-Length Integer Encoding](#variable-length-integer-encoding)
4. [Patch Actions](#patch-actions)
5. [CRC32 Validation](#crc32-validation)
6. [Encoding Algorithm](#encoding-algorithm)
7. [Decoding Algorithm](#decoding-algorithm)
8. [File Size Constraints](#file-size-constraints)
9. [Implementation Notes](#implementation-notes)
10. [References](#references)

---

## Overview

BPS (Binary Patch System) is a binary patch format designed for creating and applying patches to binary files, primarily used in ROM hacking and retro gaming. It provides efficient delta compression with integrity validation.

### Key Features

- **Compact representation**: Uses variable-length integers to minimize patch size
- **Four patch operations**: SourceRead, TargetRead, SourceCopy, TargetCopy
- **CRC32 integrity checking**: Validates source, target, and patch file integrity
- **Metadata support**: Optional manifest/metadata embedded in patch file
- **Efficient encoding**: Optimized for common patterns like run-length encoding and data reuse

### Use Cases

- ROM translation patches (changing text/graphics in game ROMs)
- Bug fix patches (correcting specific bytes in executables)
- ROM expansion patches (increasing ROM size for new content)
- Incremental updates (applying sequential version updates)

---

## File Structure

A BPS patch file consists of the following sections in order:

```
+------------------+
| Header (4 bytes) |  "BPS1" ASCII magic number
+------------------+
| Source Size      |  Variable-length integer
+------------------+
| Target Size      |  Variable-length integer
+------------------+
| Metadata Size    |  Variable-length integer
+------------------+
| Metadata         |  UTF-8 string (optional, can be empty)
+------------------+
| Actions          |  Series of encoded patch commands
+------------------+
| Footer (12 bytes)|  3 CRC32 checksums (source, target, patch)
+------------------+
```

### Header (4 bytes)

- **Magic Number**: ASCII string `"BPS1"` (0x42 0x50 0x53 0x31)
- **Purpose**: Identifies file as BPS format version 1
- **Validation**: Decoder must verify this matches exactly

### Size Fields (Variable-Length)

All size values are encoded as variable-length integers (see [Variable-Length Integer Encoding](#variable-length-integer-encoding)):

- **Source Size**: Size of the original file in bytes
- **Target Size**: Size of the resulting file after patching
- **Metadata Size**: Length of metadata string in bytes (0 if no metadata)

### Metadata (Optional)

- **Format**: UTF-8 encoded string
- **Length**: Specified by metadata size field
- **Content**: Arbitrary data, typically XML or JSON containing:
  - Author information
  - Patch description
  - Version numbers
  - Creation date
  - Copyright/licensing

**Example metadata**:
```xml
<patch>
  <author>John Doe</author>
  <version>1.2</version>
  <description>Translation patch for Example Game</description>
</patch>
```

### Actions (Variable-Length)

Series of encoded commands that transform source file into target file. Each action consists of:

1. **Command byte(s)**: Variable-length integer encoding both length and action type
2. **Data (optional)**: Raw bytes for TargetRead operations

See [Patch Actions](#patch-actions) for details.

### Footer (12 bytes, Fixed)

Three 32-bit CRC32 checksums in little-endian format:

- **Bytes 0-3**: CRC32 of source file
- **Bytes 4-7**: CRC32 of target file  
- **Bytes 8-11**: CRC32 of patch file (everything before these 12 bytes)

---

## Variable-Length Integer Encoding

BPS uses a compact variable-length encoding to minimize patch size. Integers are encoded using 7 bits per byte with a continuation bit.

### Encoding Algorithm

```csharp
public static byte[] EncodeNumber(ulong value) {
    var result = new List<byte>();
    
    while (true) {
        byte current = (byte)(value & 0x7F);  // Get lower 7 bits
        value >>= 7;                           // Shift right 7 bits
        
        if (value == 0) {
            result.Add((byte)(current | 0x80)); // Set continuation bit on last byte
            break;
        }
        
        result.Add(current);                   // No continuation bit on intermediate bytes
    }
    
    return result.ToArray();
}
```

### Decoding Algorithm

```csharp
public static ulong DecodeNumber(Stream stream) {
    ulong result = 0;
    int shift = 0;
    
    while (true) {
        int current = stream.ReadByte();
        if (current == -1) throw new EndOfStreamException();
        
        result |= (ulong)(current & 0x7F) << shift;  // Add 7 bits to result
        
        if ((current & 0x80) != 0) {                 // Check continuation bit
            break;                                    // Last byte reached
        }
        
        shift += 7;                                   // Advance to next 7-bit chunk
    }
    
    return result;
}
```

### Examples

| Value | Encoded Bytes | Binary Representation |
|-------|---------------|----------------------|
| 0 | `0x80` | `10000000` |
| 127 | `0xFF` | `11111111` |
| 128 | `0x00 0x81` | `00000000 10000001` |
| 255 | `0x7F 0x81` | `01111111 10000001` |
| 16384 | `0x00 0x00 0x81` | `00000000 00000000 10000001` |

**Note**: The last byte always has bit 7 set (0x80). Intermediate bytes have bit 7 clear.

---

## Patch Actions

BPS defines four action types encoded in the lower 2 bits of each command:

### Action Types

| Value | Name | Description | Use Case |
|-------|------|-------------|----------|
| 0 | `SourceRead` | Copy bytes from source file at current position | Unchanged data |
| 1 | `TargetRead` | Read new bytes from patch file | Changed/new data |
| 2 | `SourceCopy` | Copy bytes from arbitrary source position | Reused data blocks |
| 3 | `TargetCopy` | Copy bytes from earlier target position | Run-length encoding |

### Command Encoding

Each command is encoded as a variable-length integer that combines:
- **Length**: Number of bytes to process
- **Action**: Which operation to perform (lower 2 bits)

```
Command = ((length - 1) << 2) | action
```

**Example**:
- Copy 10 bytes using SourceRead (action 0):
  - `command = ((10 - 1) << 2) | 0 = 36`
  - Encoded as variable-length integer: `0xA4` (binary: `10100100`)

### SourceRead (Action 0)

**Format**: `[Command]`

**Operation**:
```csharp
for (int i = 0; i < length; i++) {
    target[targetPos++] = source[sourcePos++];
}
```

**Purpose**: Copy consecutive bytes from source that haven't changed

**Optimization**: Encoder groups long sequences of unchanged bytes into single SourceRead commands

### TargetRead (Action 1)

**Format**: `[Command] [Data bytes]`

**Operation**:
```csharp
for (int i = 0; i < length; i++) {
    target[targetPos++] = patch.ReadByte();
}
```

**Purpose**: Insert new or changed bytes directly from patch file

**Data**: Raw bytes follow the command in patch file

### SourceCopy (Action 2)

**Format**: `[Command] [Offset]`

**Operation**:
```csharp
long sourceOffset = sourcePos + DecodeSignedNumber(patch);
for (int i = 0; i < length; i++) {
    target[targetPos++] = source[sourceOffset++];
}
```

**Purpose**: Copy bytes from a different location in source file (data reuse)

**Offset Encoding**: Signed offset from current source position (zigzag encoding)

**Zigzag Encoding**:
```csharp
// Decode signed offset
long DecodeSignedNumber(ulong encoded) {
    return ((encoded & 1) != 0) ? -(long)(encoded >> 1) : (long)(encoded >> 1);
}
```

### TargetCopy (Action 3)

**Format**: `[Command] [Offset]`

**Operation**:
```csharp
long targetOffset = targetPos + DecodeSignedNumber(patch);
for (int i = 0; i < length; i++) {
    target[targetPos++] = target[targetOffset++];
}
```

**Purpose**: Copy bytes from earlier in target file (run-length encoding, repeated patterns)

**Offset Encoding**: Signed offset from current target position

**Special Case**: Handles overlapping copies correctly (e.g., offset=-1 for repeating last byte)

---

## CRC32 Validation

BPS uses CRC32 (Cyclic Redundancy Check) to validate file integrity.

### CRC32 Implementation

- **Algorithm**: CRC-32/ISO-HDLC (polynomial 0x04C11DB7)
- **Initial value**: 0xFFFFFFFF
- **Final XOR**: 0xFFFFFFFF
- **Reflection**: Input and output bits reflected
- **.NET API**: `System.IO.Hashing.Crc32`

### Validation Process

The decoder validates three checksums:

#### 1. Source File CRC32
```csharp
uint sourceCrc = ComputeCRC32(sourceFile);
if (sourceCrc != expectedSourceCrc) {
    warnings.Add("Source file hash mismatch");
}
```

#### 2. Target File CRC32
```csharp
uint targetCrc = ComputeCRC32(targetFile);
if (targetCrc != expectedTargetCrc) {
    warnings.Add("Target file hash mismatch");
}
```

#### 3. Patch File CRC32 (Self-Validation)

The patch file's CRC32 is validated using a special property:

```csharp
// CRC32(patchData + patchCrc32) should equal this constant
const uint CRC32_RESULT_CONSTANT = 0x2144df1c;

uint patchValidation = ComputeCRC32(patchFile);
if (patchValidation != CRC32_RESULT_CONSTANT) {
    warnings.Add("Patch file hash mismatch");
}
```

**Mathematical Property**: When you compute CRC32 of data that includes its own CRC32 at the end, the result is always `0x2144df1c`. This allows validating the patch without storing an additional checksum.

### Hash Mismatch Handling

- **Source mismatch**: Wrong source file used (e.g., different version)
- **Target mismatch**: Patch application failed or corrupted output
- **Patch mismatch**: Corrupted patch file

Warnings are returned but patching continues - the caller decides whether to accept the result.

---

## Encoding Algorithm

The encoder analyzes differences between source and target files to generate optimal patch actions.

### High-Level Process

```
1. Open source and target files
2. Load both files into memory (using ArrayPool for efficiency)
3. Write BPS header and metadata
4. Process target file sequentially:
   - Compare with source at current position
   - Look for matches in source file
   - Encode appropriate action (SourceRead, TargetRead, SourceCopy, TargetCopy)
5. Write footer with CRC32 checksums
6. Return warnings if any
```

### Detailed Algorithm

```csharp
while (targetPos < targetSize) {
    // Try to find a match in source file
    (int matchLength, int matchOffset) = FindBestMatch(
        source, target, sourcePos, targetPos, targetSize
    );
    
    if (matchLength >= MINIMUM_MATCH_LENGTH) {
        // Long enough match found in source
        if (matchOffset == sourcePos) {
            // Sequential match - use SourceRead
            WriteSourceReadCommand(matchLength);
        } else {
            // Non-sequential match - use SourceCopy
            WriteSourceCopyCommand(matchLength, matchOffset - sourcePos);
        }
        targetPos += matchLength;
        sourcePos += matchLength;
    } else {
        // No good match - use TargetRead for new/changed data
        int runLength = FindTargetReadRun(target, targetPos, targetSize);
        WriteTargetReadCommand(target, targetPos, runLength);
        targetPos += runLength;
        sourcePos += runLength;
    }
}
```

### Match Finding Strategy

The current implementation uses **linear search** with early termination:

```csharp
int FindBestMatch(byte[] source, byte[] target, int targetPos, int maxLength) {
    int bestLength = 0;
    int bestOffset = 0;
    
    // Search entire source file for best match
    for (int sourceOffset = 0; sourceOffset < sourceSize; sourceOffset++) {
        int matchLength = CountMatchingBytes(
            source, sourceOffset,
            target, targetPos,
            Math.Min(maxLength, sourceSize - sourceOffset)
        );
        
        if (matchLength > bestLength) {
            bestLength = matchLength;
            bestOffset = sourceOffset;
        }
        
        // Early termination if perfect match found
        if (matchLength == maxLength) break;
    }
    
    return (bestLength, bestOffset);
}
```

**Performance Notes**:
- Current: O(n²) linear search
- Future optimization: Suffix arrays or rolling hash for O(n log n) or O(n) performance

### Minimum Match Length

```csharp
const int MINIMUM_MATCH_LENGTH = 4;
```

Matches shorter than 4 bytes are not encoded as SourceCopy/TargetCopy because:
- Command overhead: Variable-length command + offset encoding
- For very short matches, TargetRead is more efficient

---

## Decoding Algorithm

The decoder applies patch actions sequentially to reconstruct the target file.

### High-Level Process

```
1. Validate patch file header ("BPS1")
2. Read source, target, and metadata sizes
3. Read metadata (if present)
4. Allocate target buffer
5. Process actions sequentially until target is complete
6. Write target file
7. Validate CRC32 checksums
8. Return warnings if any
```

### Detailed Algorithm

```csharp
byte[] target = new byte[targetSize];
long sourcePos = 0;
long targetPos = 0;

while (targetPos < targetSize) {
    // Read command (variable-length integer)
    ulong command = DecodeNumber(patch);
    
    // Extract action and length from command
    long length = (long)(command >> 2) + 1;
    PatchAction action = (PatchAction)(command & 3);
    
    // Execute action
    switch (action) {
        case PatchAction.SourceRead:
            // Copy from source at current position
            source.ReadExactly(target.AsSpan((int)targetPos, (int)length));
            sourcePos += length;
            targetPos += length;
            break;
            
        case PatchAction.TargetRead:
            // Read new bytes from patch
            patch.ReadExactly(target.AsSpan((int)targetPos, (int)length));
            targetPos += length;
            break;
            
        case PatchAction.SourceCopy:
            // Copy from source at specified offset
            long offset = DecodeSignedNumber(DecodeNumber(patch));
            long sourceOffset = sourcePos + offset;
            Array.Copy(source, sourceOffset, target, targetPos, length);
            sourcePos += length;
            targetPos += length;
            break;
            
        case PatchAction.TargetCopy:
            // Copy from earlier in target
            long targetOffset = targetPos + DecodeSignedNumber(DecodeNumber(patch));
            // Handle overlapping copies correctly
            for (int i = 0; i < length; i++) {
                target[targetPos++] = target[targetOffset++];
            }
            sourcePos += length;
            break;
    }
}
```

### TargetCopy Overlap Handling

TargetCopy can have overlapping source and destination ranges (e.g., copying from position N-1 to position N), which creates a run-length encoding effect:

```csharp
// Example: Repeat last byte 10 times
// offset = -1, length = 10
// target[99] = 'A'
for (int i = 0; i < 10; i++) {
    target[100 + i] = target[99 + i];  // Copies 'A' repeatedly
}
// Result: target[100..109] all contain 'A'
```

---

## File Size Constraints

### Maximum File Size

**Limit**: 2,147,483,647 bytes (2 GB - 1 byte, or `int.MaxValue`)

**Reason**: .NET arrays are limited to `int.MaxValue` elements

**Validation**:
```csharp
if (sourceFile.Length > int.MaxValue) {
    throw new ArgumentException("Source file exceeds maximum size");
}
if (targetFile.Length > int.MaxValue) {
    throw new ArgumentException("Target file exceeds maximum size");
}
```

### Minimum Patch Size

**Limit**: 19 bytes minimum

**Breakdown**:
- Header: 4 bytes (`"BPS1"`)
- Source size: 1 byte minimum (value 0)
- Target size: 1 byte minimum (value 0)
- Metadata size: 1 byte (value 0 for no metadata)
- Footer: 12 bytes (3× CRC32)
- **Total**: 19 bytes

### Empty File Handling

- **Empty source**: Valid (size = 0)
- **Empty target**: Invalid - encoder rejects zero-byte target files
  ```csharp
  if (targetFile.Length == 0) {
      throw new ArgumentException("Target file is zero bytes");
  }
  ```

---

## Implementation Notes

### Memory Management

**ArrayPool Usage**:
```csharp
byte[] sourceData = ArrayPool<byte>.Shared.Rent((int)sourceFile.Length);
try {
    // Use buffer
} finally {
    ArrayPool<byte>.Shared.Return(sourceData);
}
```

**Benefits**:
- Reduces GC pressure for large files
- Reuses memory across multiple operations
- Critical for performance with large ROM files

### Buffered I/O

**Buffer Size**: 81,920 bytes (80 KB)

```csharp
const int BUFFER_SIZE = 81920;
using var patch = new BufferedStream(patchFile.OpenWrite(), BUFFER_SIZE);
```

**Purpose**:
- Reduces system calls for small reads/writes
- Improves performance for sequential I/O patterns
- Optimal size for most file systems

### File Sharing

**Windows Compatibility**:
```csharp
using var stream = new FileStream(
    path, 
    FileMode.Open, 
    FileAccess.Read, 
    FileShare.ReadWrite  // Allow concurrent access
);
```

**Reason**: Windows file locking can prevent reading files that were just written. `FileShare.ReadWrite` allows the same process to open files multiple times.

### Stackalloc for Small Buffers

```csharp
Span<byte> header = stackalloc byte[4];
```

**When to Use**:
- Fixed-size buffers < 1 KB
- Short-lived data (function scope)
- Avoids heap allocation entirely

### Endianness

**All multi-byte integers use little-endian format**:
```csharp
uint value = BitConverter.ToUInt32(bytes);  // Little-endian on all platforms
```

### Platform Compatibility

- **Tested**: Windows (x64), .NET 10 preview
- **Expected**: Linux, macOS (little-endian architectures)
- **Not tested**: Big-endian systems (would require byte swapping)

---

## References

### Official Specifications

1. **BPS Format Documentation**: https://github.com/blakesmith/beat/blob/master/doc/bps.txt
2. **beat Patcher (Reference Implementation)**: https://github.com/blakesmith/beat

### .NET Documentation

1. **ArrayPool**: https://learn.microsoft.com/en-us/dotnet/api/system.buffers.arraypool-1
2. **Span<T>**: https://learn.microsoft.com/en-us/dotnet/api/system.span-1
3. **CRC32**: https://learn.microsoft.com/en-us/dotnet/api/system.io.hashing.crc32
4. **BufferedStream**: https://learn.microsoft.com/en-us/dotnet/api/system.io.bufferedstream
5. **File Sharing**: https://learn.microsoft.com/en-us/dotnet/api/system.io.fileshare

### Algorithms

1. **CRC-32**: https://en.wikipedia.org/wiki/Cyclic_redundancy_check
2. **Variable-length Quantity**: https://en.wikipedia.org/wiki/Variable-length_quantity
3. **Zigzag Encoding**: https://developers.google.com/protocol-buffers/docs/encoding#signed-ints

### ROM Hacking Community

1. **RomHacking.net**: https://www.romhacking.net/
2. **BPS Patch Format Discussion**: https://www.romhacking.net/forum/

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2025-10-29 | Initial specification based on .NET 10 implementation |

---

## License

This specification document is provided for reference and educational purposes. The BPS format is an open standard. Implementations should credit the original beat patcher by byuu (Near).

---

## Contact

For questions or corrections to this specification:
- GitHub Issues: https://github.com/TheAnsarya/bps-patch/issues
- Email: [Project maintainer contact]

---

**End of Specification**

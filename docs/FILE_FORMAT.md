# BPS File Format Specification

A comprehensive reference for the Binary Patch System (BPS) file format.

**Version**: 1.0  
**Last Updated**: January 7, 2026  
**Related Issue**: [#12](https://github.com/TheAnsarya/bps-patch/issues/12)

---

## Overview

BPS (Binary Patch System) is a binary delta patching format designed for efficiently encoding differences between two files. It was created by byuu for use in ROM hacking and retro gaming communities.

### Key Features

- **Compact encoding**: Variable-length integers minimize patch size
- **Four action types**: Optimized for different data patterns
- **CRC32 validation**: Built-in integrity checking
- **Metadata support**: Optional embedded information

---

## File Structure

```
┌─────────────────────────────────────────────────────────┐
│                      BPS FILE                           │
├─────────────────────────────────────────────────────────┤
│  HEADER                                                 │
│  ├─ Magic bytes: "BPS1" (4 bytes)                      │
│  ├─ Source size (VLI)                                  │
│  ├─ Target size (VLI)                                  │
│  ├─ Metadata size (VLI)                                │
│  └─ Metadata (variable, optional)                      │
├─────────────────────────────────────────────────────────┤
│  ACTIONS                                                │
│  └─ Repeated until target is fully written:            │
│     ├─ Command (VLI) = (length << 2) | action          │
│     └─ Action-specific data                            │
├─────────────────────────────────────────────────────────┤
│  FOOTER                                                 │
│  ├─ Source CRC32 (4 bytes, little-endian)             │
│  ├─ Target CRC32 (4 bytes, little-endian)             │
│  └─ Patch CRC32  (4 bytes, little-endian)             │
└─────────────────────────────────────────────────────────┘
```

---

## Variable-Length Integer (VLI) Encoding

BPS uses a custom variable-length encoding for integers, similar to LEB128 but with inverted continuation semantics.

### Encoding Rules

- Each byte carries 7 bits of data
- **MSB = 0**: More bytes follow (continuation)
- **MSB = 1**: Final byte (termination)
- After each continuation byte, subtract 1 before shifting

### Encoding Algorithm

```csharp
public static int Encode(ulong number, Span<byte> buffer)
{
	int index = 0;
	while (true)
	{
		byte x = (byte)(number & 0x7f);
		number >>= 7;
		
		if (number == 0)
		{
			buffer[index++] = (byte)(0x80 | x);  // Final byte
			return index;
		}
		
		buffer[index++] = x;  // Continuation byte
		number--;
	}
}
```

### Decoding Algorithm

```csharp
public static ulong Decode(ReadOnlySpan<byte> data, out int bytesRead)
{
	ulong result = 0;
	int shift = 0;
	bytesRead = 0;
	
	while (true)
	{
		byte x = data[bytesRead++];
		result += (ulong)(x & 0x7f) << shift;
		
		if ((x & 0x80) != 0)  // Final byte
			return result;
			
		result += 1UL << shift;
		shift += 7;
	}
}
```

### Encoding Examples

| Value | Encoded Bytes | Explanation |
|-------|---------------|-------------|
| 0 | `0x80` | MSB set = final |
| 1 | `0x81` | 1 + final bit |
| 127 | `0xFF` | 0x7F + 0x80 |
| 128 | `0x00, 0x81` | 0 (cont), then 128 |
| 255 | `0x7F, 0x81` | 127 (cont), then 128 |
| 300 | `0x2C, 0x81` | 44 (cont), then 129 |

---

## Patch Actions

BPS defines four action types, encoded in the lower 2 bits of each command:

| Value | Action | Description |
|-------|--------|-------------|
| 0 | SourceRead | Copy from source at current position |
| 1 | TargetRead | Write literal bytes from patch |
| 2 | SourceCopy | Copy from source at arbitrary offset |
| 3 | TargetCopy | Copy from already-written target |

### Command Format

```
Command VLI = (length << 2) | action
```

Where:
- `action` = lower 2 bits (0-3)
- `length` = remaining bits, actual length is `(command >> 2) + 1`

### Action Details

#### SourceRead (0)

Copies bytes from source file at the current read position.

```
Command: (length-1) << 2 | 0
Data: none
Effect: Copy `length` bytes from source[sourcePos] to target[targetPos]
```

**Use case**: Source and target are identical at this position.

#### TargetRead (1)

Writes literal bytes directly from the patch file.

```
Command: (length-1) << 2 | 1
Data: `length` bytes of literal data
Effect: Write literal bytes to target[targetPos]
```

**Use case**: New data that doesn't exist in source.

#### SourceCopy (2)

Copies bytes from source file at an arbitrary offset (relative addressing).

```
Command: (length-1) << 2 | 2
Data: Offset VLI (signed, zigzag encoded)
Effect: 
  - sourceRelativeOffset += decodeOffset(data)
  - Copy `length` bytes from source[sourceRelativeOffset] to target[targetPos]
```

**Offset encoding**: Signed zigzag representation
- `((offset & 1) != 0) ? -(offset >> 1) : (offset >> 1)`

**Use case**: Data exists in source but at different position.

#### TargetCopy (3)

Copies bytes from already-written target data (for repeated patterns).

```
Command: (length-1) << 2 | 3
Data: Offset VLI (signed, zigzag encoded)
Effect:
  - targetRelativeOffset += decodeOffset(data)
  - Copy `length` bytes from target[targetRelativeOffset] to target[targetPos]
```

**Important**: Can copy overlapping regions (for run-length encoding).

**Use case**: Repeated patterns in target (compression).

---

## Offset Encoding (Signed Zigzag)

Offsets use signed zigzag encoding to efficiently represent positive and negative values:

### Encoding

```csharp
long encoded = offset < 0 
	? ((-offset - 1) << 1) | 1   // Negative
	: (offset << 1);              // Positive
```

### Decoding

```csharp
long offset = ((encoded & 1) != 0)
	? -((encoded >> 1) + 1)   // Negative (odd)
	: (encoded >> 1);          // Positive (even)
```

### Examples

| Original | Encoded | Explanation |
|----------|---------|-------------|
| 0 | 0 | Positive, shifted |
| 1 | 2 | 1 << 1 = 2 |
| -1 | 1 | (-(-1)-1) << 1 | 1 = 1 |
| -2 | 3 | (2-1) << 1 | 1 = 3 |
| 100 | 200 | 100 << 1 = 200 |
| -100 | 199 | 99 << 1 | 1 = 199 |

---

## Footer / CRC32 Validation

The last 12 bytes contain three CRC32 checksums:

```
Bytes 0-3:  Source file CRC32 (little-endian)
Bytes 4-7:  Target file CRC32 (little-endian)
Bytes 8-11: Patch file CRC32 (little-endian, of bytes 0 to patch_size-4)
```

### Validation Process

1. Compute CRC32 of source file, compare to stored source CRC
2. Compute CRC32 of patch data (excluding final 4 bytes)
3. Include stored source/target CRCs in patch CRC computation
4. Compare computed patch CRC to stored patch CRC

### CRC32 Algorithm

Standard CRC32 with polynomial 0xEDB88320 (IEEE 802.3).

```csharp
// Using System.IO.Hashing
uint crc = Crc32.HashToUInt32(data);
```

---

## Complete Example

### Source File (6 bytes)
```
48 65 6C 6C 6F 21  ("Hello!")
```

### Target File (11 bytes)
```
48 65 6C 6C 6F 20 57 6F 72 6C 64  ("Hello World")
```

### Generated Patch

```
Offset  Bytes           Description
------  --------------  -----------
0x00    42 50 53 31     Magic: "BPS1"
0x04    86              Source size VLI: 6
0x05    8B              Target size VLI: 11
0x06    80              Metadata size VLI: 0

0x07    14              Command: ((5-1)<<2)|0 = SourceRead 5 bytes
						(Copy "Hello" from source)

0x08    19              Command: ((6-1)<<2)|1 = TargetRead 6 bytes
0x09    20 57 6F 72 6C 64  Literal: " World"

0x0F    XX XX XX XX     Source CRC32
0x13    XX XX XX XX     Target CRC32
0x17    XX XX XX XX     Patch CRC32
```

---

## Implementation Notes

### Memory Considerations

- Source and target are often loaded entirely into memory
- For large files, consider streaming with memory-mapped files
- Maximum practical size limited by `int.MaxValue` (~2GB) for Span<T>

### Performance Tips

1. **SourceRead**: Most efficient - no data in patch
2. **TargetCopy**: Good for repeated patterns
3. **SourceCopy**: Useful for moved blocks
4. **TargetRead**: Least efficient - adds to patch size

### Common Patterns

| Pattern | Best Action |
|---------|-------------|
| Unchanged data | SourceRead |
| New data | TargetRead |
| Moved block | SourceCopy |
| Repeated pattern | TargetCopy |
| RLE compression | TargetCopy (overlapping) |

---

## References

- Original BPS specification: https://github.com/blakesmith/beat/blob/master/doc/bps.txt
- byuu's beat tool: https://github.com/blakesmith/beat
- BpsPatch implementation: https://github.com/TheAnsarya/bps-patch

---

## Revision History

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-01-07 | Initial comprehensive documentation |

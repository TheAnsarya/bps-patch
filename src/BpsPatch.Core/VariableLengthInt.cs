// ========================================================================================================
// Variable-Length Integer Encoding/Decoding
// ========================================================================================================
// BPS uses a custom variable-length encoding scheme similar to LEB128.
// This class provides efficient encoding/decoding with Span-based APIs.
//
// Encoding: 7 bits per byte, MSB=1 indicates final byte
// Example: 300 → [0x2C, 0x81] (2 bytes)
//
// References:
// - BPS Specification: https://github.com/blakesmith/beat/blob/master/doc/bps.txt
// - LEB128: https://en.wikipedia.org/wiki/LEB128
// ========================================================================================================

namespace BpsPatch.Core;

/// <summary>
/// Provides efficient encoding and decoding of variable-length integers used in BPS format.
/// </summary>
/// <remarks>
/// <para>
/// BPS uses a custom variable-length encoding where each byte carries 7 bits of data.
/// The MSB (most significant bit) indicates whether this is the final byte:
/// </para>
/// <list type="bullet">
/// <item><description>MSB = 0: Continuation byte, more bytes follow</description></item>
/// <item><description>MSB = 1: Final byte, no more bytes</description></item>
/// </list>
/// <para>
/// This differs from standard LEB128 which uses MSB for continuation.
/// </para>
/// </remarks>
public static class VariableLengthInt {
	/// <summary>
	/// Maximum bytes needed to encode a ulong (64 bits / 7 bits per byte = 10 bytes).
	/// </summary>
	public const int MaxEncodedLength = 10;

	/// <summary>
	/// Encodes a number to BPS variable-length format, writing to the provided span.
	/// </summary>
	/// <param name="number">The number to encode.</param>
	/// <param name="buffer">Buffer to write encoded bytes to (must be at least 10 bytes).</param>
	/// <returns>Number of bytes written.</returns>
	/// <exception cref="ArgumentException">Buffer is too small.</exception>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Encode(ulong number, Span<byte> buffer) {
		if (buffer.Length < MaxEncodedLength) {
			throw new ArgumentException($"Buffer must be at least {MaxEncodedLength} bytes", nameof(buffer));
		}

		int index = 0;

		while (true) {
			// Extract lowest 7 bits
			byte x = (byte)(number & 0x7f);
			number >>= 7;

			if (number == 0) {
				// Final byte: set MSB to indicate termination
				buffer[index++] = (byte)(0x80 | x);
				return index;
			}

			// Continuation byte: MSB clear
			buffer[index++] = x;
			number--;
		}
	}

	/// <summary>
	/// Encodes a number to BPS variable-length format, returning a new byte array.
	/// </summary>
	/// <param name="number">The number to encode.</param>
	/// <returns>Encoded bytes.</returns>
	/// <remarks>
	/// For performance-critical code, prefer <see cref="Encode(ulong, Span{byte})"/>
	/// with a reusable buffer to avoid allocations.
	/// </remarks>
	public static byte[] Encode(ulong number) {
		Span<byte> buffer = stackalloc byte[MaxEncodedLength];
		int length = Encode(number, buffer);
		return buffer[..length].ToArray();
	}

	/// <summary>
	/// Calculates the number of bytes needed to encode a number without actually encoding it.
	/// </summary>
	/// <param name="number">The number to calculate encoding length for.</param>
	/// <returns>Number of bytes needed to encode the number.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int EncodedLength(ulong number) {
		int length = 1;
		while (number >= 0x80) {
			number = (number >> 7) - 1;
			length++;
		}
		return length;
	}

	/// <summary>
	/// Decodes a variable-length encoded number from a stream.
	/// </summary>
	/// <param name="stream">Stream to read from.</param>
	/// <returns>Decoded number.</returns>
	/// <exception cref="BpsFormatException">Unexpected end of stream.</exception>
	public static ulong Decode(Stream stream) {
		ulong data = 0;
		ulong shift = 1;

		while (true) {
			int x = stream.ReadByte();

			if (x == -1) {
				throw new BpsFormatException("Unexpected end of patch file while reading variable-length integer");
			}

			// Extract 7 bits of data
			data += (ulong)(x & 0x7f) * shift;

			// Check MSB: if set, this is the final byte
			if ((x & 0x80) != 0) {
				return data;
			}

			// Prepare for next byte
			shift <<= 7;
			data += shift;
		}
	}

	/// <summary>
	/// Decodes a variable-length encoded number from a span.
	/// </summary>
	/// <param name="data">Span containing encoded bytes.</param>
	/// <param name="bytesRead">Number of bytes consumed.</param>
	/// <returns>Decoded number.</returns>
	/// <exception cref="BpsFormatException">Invalid encoding or insufficient data.</exception>
	public static ulong Decode(ReadOnlySpan<byte> data, out int bytesRead) {
		ulong result = 0;
		ulong shift = 1;
		bytesRead = 0;

		while (bytesRead < data.Length) {
			byte x = data[bytesRead++];

			result += (ulong)(x & 0x7f) * shift;

			if ((x & 0x80) != 0) {
				return result;
			}

			shift <<= 7;
			result += shift;
		}

		throw new BpsFormatException("Unexpected end of data while reading variable-length integer");
	}

	/// <summary>
	/// Calculates the number of bytes needed to encode a number.
	/// </summary>
	/// <param name="number">The number to encode.</param>
	/// <returns>Number of bytes required.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetEncodedLength(ulong number) {
		int length = 1;
		while (number > 0x7f) {
			number = (number >> 7) - 1;
			length++;
		}
		return length;
	}
}

// ========================================================================================================
// CRC32 Calculator
// ========================================================================================================
// Utility class for CRC32 checksum computation using System.IO.Hashing.
// Provides both file-based and in-memory calculation methods.
//
// References:
// - System.IO.Hashing: https://learn.microsoft.com/en-us/dotnet/api/system.io.hashing.crc32
// - CRC32 Algorithm: https://en.wikipedia.org/wiki/Cyclic_redundancy_check
// ========================================================================================================

namespace BpsPatch.Core;

/// <summary>
/// Provides CRC32 checksum computation for BPS patch validation.
/// </summary>
/// <remarks>
/// <para>
/// BPS patches include three CRC32 checksums in the footer:
/// </para>
/// <list type="bullet">
/// <item><description>Source file CRC32 - validates original file</description></item>
/// <item><description>Target file CRC32 - validates reconstructed file</description></item>
/// <item><description>Patch file CRC32 - self-validation using residue property</description></item>
/// </list>
/// <para>
/// The patch CRC32 uses a special property: CRC32(data + CRC32(data)) equals a constant
/// (<see cref="ResultConstant"/>), allowing the decoder to validate the entire patch
/// including its own checksum.
/// </para>
/// </remarks>
public static class Crc32Calculator
{
    /// <summary>
    /// Magic constant: CRC32(data + CRC32(data)) always equals this value.
    /// Used to validate patch file integrity without knowing the original CRC.
    /// </summary>
    /// <remarks>
    /// This is the CRC32 "residue" - when you compute CRC32 over data that ends
    /// with its own CRC32, the result is always this constant (0x2144df1c).
    /// </remarks>
    public const uint ResultConstant = 0x2144df1c;

    /// <summary>
    /// Default buffer size for file I/O operations (80 KB).
    /// </summary>
    private const int BufferSize = 81920;

    /// <summary>
    /// Computes CRC32 checksum for a file.
    /// </summary>
    /// <param name="file">File to compute CRC32 for.</param>
    /// <returns>CRC32 checksum as unsigned 32-bit integer.</returns>
    /// <exception cref="FileNotFoundException">File does not exist.</exception>
    /// <exception cref="IOException">Error reading file.</exception>
    public static uint Compute(FileInfo file)
    {
        using var stream = new FileStream(
            file.FullName,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);

        return Compute(stream);
    }

    /// <summary>
    /// Computes CRC32 checksum from a stream.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <returns>CRC32 checksum as unsigned 32-bit integer.</returns>
    public static uint Compute(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[BufferSize];
        var crc32 = new Crc32();

        int bytesRead;
        while ((bytesRead = stream.Read(buffer)) > 0)
        {
            crc32.Append(buffer[..bytesRead]);
        }

        Span<byte> hashBytes = stackalloc byte[4];
        crc32.GetHashAndReset(hashBytes);
        return BitConverter.ToUInt32(hashBytes);
    }

    /// <summary>
    /// Computes CRC32 checksum for in-memory data.
    /// </summary>
    /// <param name="data">Data to compute CRC32 for.</param>
    /// <returns>CRC32 checksum as unsigned 32-bit integer.</returns>
    public static uint Compute(ReadOnlySpan<byte> data)
    {
        var crc32 = new Crc32();
        crc32.Append(data);

        Span<byte> hashBytes = stackalloc byte[4];
        crc32.GetHashAndReset(hashBytes);
        return BitConverter.ToUInt32(hashBytes);
    }

    /// <summary>
    /// Computes CRC32 checksum for a file and returns as byte array.
    /// </summary>
    /// <param name="file">File to compute CRC32 for.</param>
    /// <returns>CRC32 checksum as 4-byte array (little-endian).</returns>
    /// <remarks>
    /// Includes retry logic for files that may be in use by other processes.
    /// </remarks>
    public static byte[] ComputeBytes(FileInfo file)
    {
        const int maxRetries = 5;
        const int retryDelayMs = 50;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                using var stream = new FileStream(
                    file.FullName,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite);

                return ComputeBytes(stream);
            }
            catch (IOException) when (attempt < maxRetries - 1)
            {
                Thread.Sleep(retryDelayMs);
            }
        }

        // Final attempt without catching
        using var finalStream = new FileStream(
            file.FullName,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite);

        return ComputeBytes(finalStream);
    }

    /// <summary>
    /// Computes CRC32 checksum from a stream and returns as byte array.
    /// </summary>
    /// <param name="stream">Stream to read from.</param>
    /// <returns>CRC32 checksum as 4-byte array (little-endian).</returns>
    public static byte[] ComputeBytes(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[BufferSize];
        var crc32 = new Crc32();

        int bytesRead;
        while ((bytesRead = stream.Read(buffer)) > 0)
        {
            crc32.Append(buffer[..bytesRead]);
        }

        byte[] result = new byte[4];
        crc32.GetHashAndReset(result);
        return result;
    }

    /// <summary>
    /// Computes CRC32 checksum for in-memory data and returns as byte array.
    /// </summary>
    /// <param name="data">Data to compute CRC32 for.</param>
    /// <returns>CRC32 checksum as 4-byte array (little-endian).</returns>
    public static byte[] ComputeBytes(ReadOnlySpan<byte> data)
    {
        var crc32 = new Crc32();
        crc32.Append(data);

        byte[] result = new byte[4];
        crc32.GetHashAndReset(result);
        return result;
    }

    /// <summary>
    /// Validates a patch file using the CRC32 residue property.
    /// </summary>
    /// <param name="patchFile">Patch file to validate.</param>
    /// <returns>True if the patch file is valid, false otherwise.</returns>
    /// <remarks>
    /// A valid BPS patch file includes its own CRC32 at the end. When computing
    /// CRC32 over the entire file (including that trailing CRC32), the result
    /// should equal <see cref="ResultConstant"/> (0x2144df1c).
    /// </remarks>
    public static bool ValidatePatch(FileInfo patchFile)
    {
        return Compute(patchFile) == ResultConstant;
    }
}

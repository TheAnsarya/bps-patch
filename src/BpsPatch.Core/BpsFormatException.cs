// ========================================================================================================
// BPS Patch Format Exception
// ========================================================================================================
// Custom exception type for malformed BPS patch files.
// Thrown when patch data doesn't conform to the BPS specification.
//
// References:
// - BPS Specification: https://github.com/blakesmith/beat/blob/master/doc/bps.txt
// - Exception Best Practices: https://learn.microsoft.com/en-us/dotnet/standard/exceptions/best-practices-for-exceptions
// ========================================================================================================

namespace BpsPatch.Core;

/// <summary>
/// Exception thrown when a BPS patch file is malformed or invalid.
/// </summary>
/// <remarks>
/// <para>
/// This exception indicates structural issues with the patch file, such as:
/// </para>
/// <list type="bullet">
/// <item><description>Invalid header (not "BPS1")</description></item>
/// <item><description>Truncated file (unexpected end of data)</description></item>
/// <item><description>Invalid variable-length integer encoding</description></item>
/// <item><description>Corrupted patch commands</description></item>
/// </list>
/// <para>
/// Note: CRC32 mismatches are reported as warnings, not exceptions,
/// since the patch may still apply successfully.
/// </para>
/// </remarks>
[Serializable]
public class BpsFormatException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BpsFormatException"/> class.
    /// </summary>
    public BpsFormatException()
        : base("Invalid BPS patch format")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BpsFormatException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public BpsFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BpsFormatException"/> class
    /// with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="inner">The exception that caused this exception.</param>
    public BpsFormatException(string message, Exception inner)
        : base(message, inner)
    {
    }
}

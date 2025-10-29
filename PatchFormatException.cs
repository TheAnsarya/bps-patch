namespace bps_patch;

/// <summary>
/// Exception thrown when a BPS patch file is malformed or invalid.
/// See: https://learn.microsoft.com/en-us/dotnet/standard/exceptions/how-to-create-user-defined-exceptions
/// </summary>
public class PatchFormatException : Exception {
	/// <summary>
	/// Initializes a new instance of the PatchFormatException class.
	/// </summary>
	public PatchFormatException() : base() { }

	/// <summary>
	/// Initializes a new instance with a specified error message.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	public PatchFormatException(string? message) : base(message) { }

	/// <summary>
	/// Initializes a new instance with a specified error message and inner exception.
	/// </summary>
	/// <param name="message">The message that describes the error.</param>
	/// <param name="innerException">The exception that caused this exception.</param>
	public PatchFormatException(string? message, Exception? innerException) : base(message, innerException) { }
}



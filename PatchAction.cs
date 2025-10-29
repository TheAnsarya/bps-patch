namespace bps_patch;

/// <summary>
/// Defines the four types of patch operations used in BPS format.
/// See: https://github.com/blakesmith/beat/blob/master/doc/bps.txt
/// </summary>
enum PatchAction : byte {
	/// <summary>
	/// Copy bytes from source file at current position (identical data)
	/// </summary>
	SourceRead = 0,

	/// <summary>
	/// Read new bytes directly from patch file (changed data)
	/// </summary>
	TargetRead = 1,

	/// <summary>
	/// Copy bytes from another location in source file (reused data)
	/// </summary>
	SourceCopy = 2,

	/// <summary>
	/// Copy bytes from earlier in target file (RLE-like repetition)
	/// </summary>
	TargetCopy = 3
}



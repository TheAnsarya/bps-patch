using System;
using System.Collections.Generic;
using System.Text;

namespace bps_patch {
	public class PatchFormatException: Exception {
		public PatchFormatException() : base() { }
		public PatchFormatException(string? message) : base(message) { }
		public PatchFormatException(string? message, Exception? innerException) : base(message, innerException) { }
	}
}

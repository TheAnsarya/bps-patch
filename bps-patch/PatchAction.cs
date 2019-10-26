using System;
using System.Collections.Generic;
using System.Text;

namespace bps_patch {
	enum PatchAction : byte {
		SourceRead,
		TargetRead,
		SourceCopy,
		TargetCopy
	};
}

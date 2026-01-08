// Global using directives for common namespaces
// See: https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-10#global-using-directives

global using System;
global using System.Buffers;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Runtime.CompilerServices;
global using System.Threading.Tasks;

// Make internal classes visible to test project
[assembly: InternalsVisibleTo("bps-patch.Tests")]

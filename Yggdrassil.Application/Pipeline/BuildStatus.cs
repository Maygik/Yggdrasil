using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrassil.Application.Pipeline
{
    public enum BuildStatus
    {
        Success,                // Build completed successfully with no warnings or errors
        SuccessWithWarnings,    // Build completed successfully but with some warnings that may require attention
        Blocked,                // User action is required before the build can proceed (e.g. choosing a name)
        Failed                  // Build failed due to errors that must be fixed before a successful build can be achieved
    }
}

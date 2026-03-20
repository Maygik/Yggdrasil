using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrassil.Application.UseCases
{
    public class ServiceResult
    {
        public bool Success;
        public string? ErrorMessage;

        // Maybe these should be linked to events instead of just being lists
        // Then we can have stuff actually show whilst it's going through the
        // process, rather than just bulk at the end?
        public List<string> Warnings = new List<string>();
        public List<string> Messages = new List<string>();
        public bool HasWarnings => Warnings.Count > 0;
        public bool HasMessages => Messages.Count > 0;

        public ServiceResult(bool success, string? error = null)
        {
            Success = success;
            ErrorMessage = error;
        }
    }
}

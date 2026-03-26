using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrasil.Application.Abstractions;

namespace Yggdrasil.Infrastructure.QC
{
    /// <summary>
    ///  Writes assembled QC and QCI files to disk using QcAssembler output
    /// </summary>
    public class QCWriter : IQcWriter
    {
        /// <summary>
        /// Writes the given QC content to the specified output path. This is a simple wrapper around File.WriteAllText.
        /// But centralizes QC writing logic in case we want to add additional processing, logging, or error handling in the future.
        /// </summary>
        /// <param name="outputPath"></param>
        /// <param name="qcContent"></param>
        public static void WriteQc(string outputPath, string qcContent)
        {
            File.WriteAllText(outputPath, qcContent);
        }
    }
}

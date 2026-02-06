using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Domain.QC;

namespace Yggdrassil.Application.Abstractions
{
    public interface IQcAssembler
    {
        public string AssembleQc(QcConfig config);
    }
}

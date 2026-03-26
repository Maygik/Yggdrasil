using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yggdrasil.Domain.QC;

namespace Yggdrasil.Application.Abstractions
{
    public interface IQcAssembler
    {
        public string AssembleQc(QcConfig config);
    }
}

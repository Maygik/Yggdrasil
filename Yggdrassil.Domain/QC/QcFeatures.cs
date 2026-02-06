using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrassil.Domain.QC
{
    public sealed class QcFeatures
    {
        public bool UseIk       { get; set; }   = false;
        public bool UseHitboxes { get; set; }   = false;
        public bool UseRagdoll  { get; set; }   = false;
    }
}

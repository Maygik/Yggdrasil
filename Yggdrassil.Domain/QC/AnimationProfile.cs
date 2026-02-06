using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrassil.Domain.QC
{
    /// <summary>
    /// Enumeration of supported animation profiles for models.
    /// Used during QC generation to apply appropriate animation settings.
    /// </summary>
    public enum AnimationProfile
    {
        None,
        RagdollOnly,
        MalePlayer,
        FemalePlayer,
        MaleNPC,
        FemaleNPC,
        CombineNPC,
        MetrocopNPC,
    }
}

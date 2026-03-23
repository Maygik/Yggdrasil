using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yggdrassil.Domain.Rigging
{
    /// <summary>
    ///  Defines a logical slot in a rig where a bone can be mapped.
    ///  E.g. "Head", "LeftWrist", "RightThigh", "Hips", etc.
    ///  These can then be translated to actual bone names in different skeletons for export.
    /// </summary>
    public class RigSlot
    {
        public string DisplayName { get; set; } = string.Empty; // The display name of this slot (e.g. "Head")
        public string LogicalBone { get; set; } = string.Empty; // The logical bone (e.g. ValveBiped.Bip01_Head1) that this slot represents
        public string? AssignedBone { get; set; } = null; // The actual bone name assigned to this slot during rig mapping. Null if unassigned.

        public RigSlot(string logicalBone, string displayName)
        {
            LogicalBone = logicalBone;
            DisplayName = displayName;
        }
        public override string ToString()
        {
            return $"RigSlot: {DisplayName} (Mapping: {LogicalBone}, AssignedBone: {AssignedBone ?? "None"})";
        }
    }
}

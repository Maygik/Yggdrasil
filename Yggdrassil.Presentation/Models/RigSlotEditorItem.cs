namespace Yggdrassil.Presentation.Models
{
    public sealed class RigSlotEditorItem
    {
        public int SlotIndex { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string LogicalBone { get; set; } = string.Empty;
        public string AssignedBoneDisplay { get; set; } = "Unassigned";
        public bool IsAssigned { get; set; }
    }
}

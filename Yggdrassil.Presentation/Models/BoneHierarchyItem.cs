using System.Collections.ObjectModel;

namespace Yggdrassil.Presentation.Models
{
    public sealed class BoneHierarchyItem
    {
        public string Name { get; set; } = string.Empty;
        public bool IsDeform { get; set; }
        public string BoneKindLabel => IsDeform ? "Deform" : "Helper";
        public ObservableCollection<BoneHierarchyItem> Children { get; } = new();
    }
}

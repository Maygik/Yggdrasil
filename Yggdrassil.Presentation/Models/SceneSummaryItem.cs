using System.Collections.ObjectModel;

namespace Yggdrassil.Presentation.Models
{
    public sealed class MeshSummaryItem
    {
        public string Name { get; set; } = string.Empty;
        public int VertexCount { get; set; }
        public int FaceCount { get; set; }
        public ObservableCollection<string> Materials { get; set; } = new();
    }

    public sealed class MaterialUsageItem
    {
        public string Name { get; set; } = string.Empty;
        public int MeshUsageCount { get; set; }
    }
}

using System.Collections.ObjectModel;

using Yggdrasil.Domain.QC;

namespace Yggdrasil.Presentation.Models
{
    public sealed class MaterialPathEditorItem
    {
        public int MaterialPathIndex { get; set; }
        public string Path { get; set; } = string.Empty;
    }

    public sealed class BodygroupEditorItem
    {
        public int BodygroupIndex { get; set; }
        public string Name { get; set; } = "DefaultBodygroup";
        public ObservableCollection<BodygroupOptionItem> Options { get; set; } = new();
        public Bodygroup SourceBodygroup { get; set; } = new("DefaultBodygroup", new());
    }

    public sealed class BodygroupOptionItem
    {
        public BodygroupEditorItem ParentBodygroup { get; set; } = null!;
        public string? MeshName { get; set; }
        public int OptionIndex { get; set; }
        public string DisplayName => string.IsNullOrWhiteSpace(MeshName) ? "(Blank Option)" : MeshName!;
        public bool IsBlank => string.IsNullOrWhiteSpace(MeshName);
    }
}

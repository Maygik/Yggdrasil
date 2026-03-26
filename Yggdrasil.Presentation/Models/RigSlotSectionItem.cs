using System.Collections.ObjectModel;

namespace Yggdrasil.Presentation.Models
{
    public sealed class RigSlotSectionItem
    {
        public string Title { get; set; } = string.Empty;
        public bool IsExpanded { get; set; } = true;
        public ObservableCollection<RigSlotEditorItem> Slots { get; } = new();
    }
}

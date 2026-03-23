using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Yggdrassil.Presentation.Models
{
    public sealed class RigSlotEditorItem : INotifyPropertyChanged
    {
        private string? _assignedBoneName;

        public int SlotIndex { get; set; }
        public string SectionTitle { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string LogicalBone { get; set; } = string.Empty;

        public string SelectionDisplayName =>
            string.IsNullOrWhiteSpace(SectionTitle) ? DisplayName : $"{SectionTitle} - {DisplayName}";

        public string AssignedBoneDisplay =>
            string.IsNullOrWhiteSpace(_assignedBoneName) ? "Unassigned" : _assignedBoneName!;

        public bool IsAssigned => !string.IsNullOrWhiteSpace(_assignedBoneName);

        public void UpdateAssignment(string? assignedBoneName)
        {
            if (string.Equals(_assignedBoneName, assignedBoneName, StringComparison.Ordinal))
                return;

            _assignedBoneName = assignedBoneName;
            OnPropertyChanged(nameof(AssignedBoneDisplay));
            OnPropertyChanged(nameof(IsAssigned));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Yggdrasil.Presentation.Models
{
    public sealed class RigSlotEditorItem : INotifyPropertyChanged
    {
        private string? _assignedBoneName;
        private bool _hasDuplicateAssignment;
        private bool _isSelected;

        public int SlotIndex { get; set; }
        public string SectionTitle { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string LogicalBone { get; set; } = string.Empty;
        public string? AssignedBoneName => _assignedBoneName;

        public string SelectionDisplayName =>
            string.IsNullOrWhiteSpace(SectionTitle) ? DisplayName : $"{SectionTitle} - {DisplayName}";

        public string AssignedBoneDisplay =>
            string.IsNullOrWhiteSpace(_assignedBoneName) ? "Unassigned" : _assignedBoneName!;

        public bool IsAssigned => !string.IsNullOrWhiteSpace(_assignedBoneName);

        public bool HasDuplicateAssignment => _hasDuplicateAssignment;

        public bool IsSelected => _isSelected;

        public string AssignmentToolTip =>
            _hasDuplicateAssignment
                ? $"{AssignedBoneDisplay} is assigned to multiple rig slots."
                : AssignedBoneDisplay;

        public void UpdateAssignment(string? assignedBoneName)
        {
            if (string.Equals(_assignedBoneName, assignedBoneName, StringComparison.Ordinal))
                return;

            _assignedBoneName = assignedBoneName;
            OnPropertyChanged(nameof(AssignedBoneDisplay));
            OnPropertyChanged(nameof(IsAssigned));
            OnPropertyChanged(nameof(AssignmentToolTip));
        }

        public void UpdateDuplicateAssignment(bool hasDuplicateAssignment)
        {
            if (_hasDuplicateAssignment == hasDuplicateAssignment)
                return;

            _hasDuplicateAssignment = hasDuplicateAssignment;
            OnPropertyChanged(nameof(HasDuplicateAssignment));
            OnPropertyChanged(nameof(AssignmentToolTip));
        }

        public void UpdateSelection(bool isSelected)
        {
            if (_isSelected == isSelected)
                return;

            _isSelected = isSelected;
            OnPropertyChanged(nameof(IsSelected));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Yggdrasil.Presentation.Models
{
    public sealed class MaterialListItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string Name { get; set; } = string.Empty;

        public bool IsSelected => _isSelected;

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

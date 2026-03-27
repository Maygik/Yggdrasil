using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using Yggdrasil.Presentation.Models;
using Yggdrasil.Presentation.Services;

namespace Yggdrasil.Presentation.Pages
{
    public sealed partial class MaterialsPage : Page
    {
        public AppHost Host { get; }
        public ObservableCollection<MaterialListItem> MaterialItems { get; } = new();
        private MaterialListItem? _selectedMaterial;

        public MaterialsPage()
        {
            Host = App.Instance.Host;
            InitializeComponent();

            Host.Shell.PropertyChanged += Shell_PropertyChanged;
            Unloaded += MaterialsPage_Unloaded;

            RefreshFromShell();
        }

        private void Shell_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Host.Shell.CurrentSession)
                || e.PropertyName == nameof(Host.Shell.HasOpenProject)
                || e.PropertyName == nameof(Host.Shell.CanExportModel))
            {
                RefreshFromShell();
                return;
            }

            if (e.PropertyName == nameof(Host.Shell.SelectedMaterialName))
            {
                ApplyShellSelection();
            }
        }

        private void RefreshFromShell()
        {
            var selectedMaterialName = Host.Shell.SelectedMaterialName;

            NoProjectPanel.Visibility = Host.Shell.HasOpenProject ? Visibility.Collapsed : Visibility.Visible;
            MaterialsPanel.Visibility = Host.Shell.HasOpenProject ? Visibility.Visible : Visibility.Collapsed;

            MaterialItems.Clear();

            var materialNames = Host.Shell.CurrentSession?.Project?.Scene?.MaterialSettings.Keys
                .OrderBy(name => name, System.StringComparer.OrdinalIgnoreCase);

            if (materialNames is not null)
            {
                foreach (var materialName in materialNames)
                {
                    MaterialItems.Add(new MaterialListItem
                    {
                        Name = materialName
                    });
                }
            }

            NoMaterialsPanel.Visibility = MaterialItems.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            MaterialListItemsControl.Visibility = MaterialItems.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            SetSelectedMaterial(selectedMaterialName is not null
                ? MaterialItems.FirstOrDefault(item => string.Equals(item.Name, selectedMaterialName, System.StringComparison.Ordinal))
                : null);
        }

        private void MaterialRow_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is MaterialListItem materialItem)
            {
                SetSelectedMaterial(materialItem);
            }
        }

        private void MaterialRow_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is MaterialListItem materialItem)
            {
                Host.Shell.HoveredMaterialName = materialItem.Name;
            }
        }

        private void MaterialRow_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (sender is FrameworkElement element
                && element.Tag is MaterialListItem materialItem
                && string.Equals(Host.Shell.HoveredMaterialName, materialItem.Name, System.StringComparison.Ordinal))
            {
                Host.Shell.HoveredMaterialName = null;
            }
        }

        private void SetSelectedMaterial(MaterialListItem? materialItem)
        {
            if (ReferenceEquals(_selectedMaterial, materialItem))
            {
                Host.Shell.SelectedMaterialName = materialItem?.Name;
                UpdateSelectedMaterialUi();
                return;
            }

            _selectedMaterial?.UpdateSelection(false);
            _selectedMaterial = materialItem;
            _selectedMaterial?.UpdateSelection(true);
            Host.Shell.SelectedMaterialName = materialItem?.Name;

            UpdateSelectedMaterialUi();
        }

        private void UpdateSelectedMaterialUi()
        {
            SelectedMaterialNameTextBlock.Text = _selectedMaterial is null
                ? "Selected material: None"
                : $"Selected material: {_selectedMaterial.Name}";
        }

        private void MaterialsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Host.Shell.HoveredMaterialName = null;
            Host.Shell.PropertyChanged -= Shell_PropertyChanged;
        }

        private void ApplyShellSelection()
        {
            var selectedMaterialName = Host.Shell.SelectedMaterialName;
            var selectedItem = selectedMaterialName is null
                ? null
                : MaterialItems.FirstOrDefault(item => string.Equals(item.Name, selectedMaterialName, System.StringComparison.Ordinal));

            if (selectedMaterialName is not null && selectedItem is null)
            {
                Host.Shell.SelectedMaterialName = null;
                return;
            }

            SetSelectedMaterial(selectedItem);
        }
    }
}

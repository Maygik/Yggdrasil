using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using Yggdrassil.Presentation.Models;
using Yggdrassil.Presentation.Services;

namespace Yggdrassil.Presentation.Pages
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
            }
        }

        private void RefreshFromShell()
        {
            var selectedMaterialName = _selectedMaterial?.Name;

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
                : MaterialItems.FirstOrDefault());
        }

        private void MaterialRow_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is MaterialListItem materialItem)
            {
                SetSelectedMaterial(materialItem);
            }
        }

        private void SetSelectedMaterial(MaterialListItem? materialItem)
        {
            if (ReferenceEquals(_selectedMaterial, materialItem))
            {
                UpdateSelectedMaterialUi();
                return;
            }

            _selectedMaterial?.UpdateSelection(false);
            _selectedMaterial = materialItem;
            _selectedMaterial?.UpdateSelection(true);

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
            Host.Shell.PropertyChanged -= Shell_PropertyChanged;
        }
    }
}

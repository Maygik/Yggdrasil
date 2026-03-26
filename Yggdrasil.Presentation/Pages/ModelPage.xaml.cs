using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Yggdrasil.Presentation.Models;
using Yggdrasil.Presentation.Services;
using Windows.UI;
using System.Linq;
using System.Collections.Generic;

namespace Yggdrasil.Presentation.Pages
{
    public sealed partial class ModelPage : Page
    {
        public AppHost Host { get; }
        public ObservableCollection<MeshSummaryItem> MeshSummaryItems { get; } = new();
        public ObservableCollection<MaterialUsageItem> MaterialUsageItems { get; } = new();

        public ModelPage()
        {
            Host = App.Instance.Host;
            InitializeComponent();

            Host.Shell.PropertyChanged += Shell_PropertyChanged;
            Unloaded += ModelPage_Unloaded;

            ResetSceneSummaryPlaceholders();
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
            NoProjectPanel.Visibility = Host.Shell.HasOpenProject ? Visibility.Collapsed : Visibility.Visible;
            ModelSettingsPanel.Visibility = Host.Shell.HasOpenProject ? Visibility.Visible : Visibility.Collapsed;

            var scene = Host.Shell.CurrentSession?.Project?.Scene;
            var hasLoadedModel = scene is not null
                && (scene.RootBone is not null || scene.MeshGroups.Count > 0);

            SceneSummaryPanel.Visibility = hasLoadedModel ? Visibility.Visible : Visibility.Collapsed;
            ModelToolsPanel.Visibility = hasLoadedModel ? Visibility.Visible : Visibility.Collapsed;

            if (scene is not null && hasLoadedModel)
            {
                // Populate general fields
                TotalVerticesTextBlock.Text = scene.MeshGroups.Sum(mg => mg.Meshes.Sum(m => m.Vertices.Count)).ToString();
                TotalFacesTextBlock.Text = scene.MeshGroups.Sum(mg => mg.Meshes.Sum(m => m.Faces.Count)).ToString();
                TotalBonesTextBlock.Text = scene.RootBone is not null ? scene.RootBone.GetAllDescendantsAndSelf().Count.ToString() : "---";

                Dictionary<string, int> materialUsage = new();

                // Populate mesh groups summary
                MeshSummaryItems.Clear();
                foreach (var meshGroup in scene.MeshGroups)
                {
                    var vertexCount = meshGroup.Meshes.Sum(m => m.Vertices.Count);
                    var faceCount = meshGroup.Meshes.Sum(m => m.Faces.Count);

                    var materials = meshGroup.Meshes
                        .Select(m => m.Material)
                        .Where(mat => !string.IsNullOrWhiteSpace(mat))
                        .Distinct()
                        .ToList();
                    MeshSummaryItems.Add(new MeshSummaryItem
                    {
                        Name = meshGroup.Name,
                        VertexCount = vertexCount,
                        FaceCount = faceCount,
                        Materials = new ObservableCollection<string>(materials)
                    });

                    // Track material usage
                    foreach (var material in materials)
                    {
                        if (materialUsage.ContainsKey(material))
                        {
                            materialUsage[material]++;
                        }
                        else
                        {
                            materialUsage[material] = 1;
                        }
                    }
                }

                // Populate material usage summary
                MaterialUsageItems.Clear();
                foreach (var kvp in materialUsage)
                {
                    MaterialUsageItems.Add(new MaterialUsageItem
                    {
                        Name = kvp.Key,
                        MeshUsageCount = kvp.Value
                    });
                }
            }
        }

        private void ResetSceneSummaryPlaceholders()
        {
            TotalVerticesTextBlock.Text = "-";
            TotalFacesTextBlock.Text = "-";
            TotalBonesTextBlock.Text = "-";

            MeshSummaryItems.Clear();
            MaterialUsageItems.Clear();
        }

        private async void BrowseImportModelButton_Click(object sender, RoutedEventArgs e)
        {
            var modelPath = await Host.FileDialogs.ShowImportModelDialogAsync();
            if (!string.IsNullOrWhiteSpace(modelPath))
            {
                ImportModelPathTextBox.Text = modelPath;
            }
        }

        private async void ImportModelButton_Click(object sender, RoutedEventArgs e)
        {
            var modelPath = ImportModelPathTextBox.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(modelPath))
            {
                var selectedPath = await Host.FileDialogs.ShowImportModelDialogAsync();
                if (!string.IsNullOrWhiteSpace(selectedPath))
                {
                    ImportModelPathTextBox.Text = selectedPath;
                    modelPath = selectedPath;
                }
            }

            if (string.IsNullOrWhiteSpace(modelPath))
            {
                await FlashImportPathTextBoxAsync();
                return;
            }

            Host.Shell.ImportModel(modelPath, AutoMapCheckBox.IsChecked == true);
        }

        private async Task FlashImportPathTextBoxAsync()
        {
            var originalBackground = ImportModelPathTextBox.Background;
            var originalBorderBrush = ImportModelPathTextBox.BorderBrush;

            ImportModelPathTextBox.Background = new SolidColorBrush(Color.FromArgb(255, 70, 20, 20));
            ImportModelPathTextBox.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 140, 40, 40));

            await Task.Delay(300);

            ImportModelPathTextBox.Background = originalBackground;
            ImportModelPathTextBox.BorderBrush = originalBorderBrush;
        }

        private void ModelPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Host.Shell.PropertyChanged -= Shell_PropertyChanged;
        }
    }
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Yggdrassil.Domain.Project;
using Yggdrassil.Domain.QC;
using Yggdrassil.Presentation.Models;
using Yggdrassil.Presentation.Services;

namespace Yggdrassil.Presentation.Pages
{
    public sealed partial class ProjectPage : Page
    {
        public AppHost Host { get; }
        public ObservableCollection<MaterialPathEditorItem> MaterialPathItems { get; } = new();
        public ObservableCollection<BodygroupEditorItem> BodygroupItems { get; } = new();
        private bool _isRefreshing; // Bool, used as simple mutex

        public ProjectPage()
        {
            Host = App.Instance.Host;
            InitializeComponent();

            AnimationProfileComboBox.ItemsSource = Enum.GetValues<AnimationProfile>().ToList();
            Host.Shell.PropertyChanged += Shell_PropertyChanged;
            Unloaded += ProjectPage_Unloaded;

            SurfacePropComboBox.ItemsSource = CommonSurfaceProps;

            RefreshFromShell();
        }

        private void ProjectPage_Unloaded1(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void AnimationProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isRefreshing)
                return;

            if (Host.Shell.CurrentSession?.Project is null)
                return;

            if (AnimationProfileComboBox.SelectedItem is AnimationProfile profile)
            {
                Host.Shell.CurrentSession.Project.Qc.AnimationProfile = profile;
            }
        }

        private void ProjectNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isRefreshing || Host.Shell.CurrentSession?.Project is null)
                return;

            Host.Shell.CurrentSession.Project.Name = ProjectNameTextBox.Text;
        }

        // Store the most common surface props
        // Using one outside of these is 1/10000 models
        // Still let them type though
        private static readonly string[] CommonSurfaceProps = new[]
        {
            "flesh",
            "default",
            "metal",
            "wood",
            "dirt",
            "grass",
            "slime",
            "glass",
            "plastic",
            "porcelain"
        };


        // actually clicking on the item doesn't trigger text changed
        private void SurfacePropComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isRefreshing || Host.Shell.CurrentSession?.Project is null)
                return;

            if (e.AddedItems.FirstOrDefault() is string selectedProp)
            {
                Host.Shell.CurrentSession.Project.Qc.SurfaceProp = selectedProp;
            }
        }

        // but if the user types in a custom surface prop, we want to handle that too, so we also listen for text submitted
        private void SurfacePropComboBox_TextSubmitted(ComboBox sender, ComboBoxTextSubmittedEventArgs e)
        {
            if (_isRefreshing || Host.Shell.CurrentSession?.Project is null)
                return;

            Host.Shell.CurrentSession.Project.Qc.SurfaceProp = e.Text;
        }

        private void OutputDirectoryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isRefreshing || Host.Shell.CurrentSession?.Project is null)
                return;

            var project = Host.Shell.CurrentSession.Project;
            var normalizedPath = Host.Backend.ProjectEditor.NormalizeProjectRelativePath(project, OutputDirectoryTextBox.Text);
            project.Build.OutputDirectory = normalizedPath;

            if (!string.Equals(OutputDirectoryTextBox.Text, normalizedPath, StringComparison.Ordinal))
            {
                _isRefreshing = true;
                OutputDirectoryTextBox.Text = normalizedPath;
                OutputDirectoryTextBox.SelectionStart = normalizedPath.Length;
                _isRefreshing = false;
            }
        }

        private async void BrowseOutputDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            var project = Host.Shell.CurrentSession?.Project;

            if (project is null)
                return;
            else if (string.IsNullOrWhiteSpace(project.Directory))
                return;

            if (App.Instance.MainWindow is null)
                return;

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Instance.MainWindow);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);

            var picker = new FolderPicker(windowId);
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            var folder = await picker.PickSingleFolderAsync();
            if (folder is null)
                return;

            var relativePath = Host.Backend.ProjectEditor.NormalizeProjectRelativePath(project, folder.Path);

            _isRefreshing = true;
            OutputDirectoryTextBox.Text = relativePath;
            OutputDirectoryTextBox.SelectionStart = relativePath.Length;
            _isRefreshing = false;

            project.Build.OutputDirectory = relativePath;
        }

        private void ModelPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isRefreshing || Host.Shell.CurrentSession?.Project is null)
                return;

            Host.Shell.CurrentSession.Project.Qc.ModelPath = ModelPathTextBox.Text;
        }

        private void AddMaterialPathButton_Click(object sender, RoutedEventArgs e)
        {
            if (Host.Shell.CurrentSession?.Project is null)
                return;

            Host.Backend.ProjectEditor.AddMaterialPath(Host.Shell.CurrentSession.Project, NewMaterialPathTextBox.Text);

            RefreshFromShell();
        }

        private void RemoveMaterialPathButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement button)
                return;
            if (Host.Shell.CurrentSession?.Project is null)
                return;

            if (button.Tag is not MaterialPathEditorItem materialPathItem)
                return;

            Host.Backend.ProjectEditor.RemoveMaterialPath(
                Host.Shell.CurrentSession.Project,
                materialPathItem.MaterialPathIndex);

            RefreshFromShell();
        }

        private void AddBodygroupButton_Click(object sender, RoutedEventArgs e)
        {
            if (Host.Shell.CurrentSession?.Project is null)
                return;

            Host.Backend.ProjectEditor.AddBodygroup(Host.Shell.CurrentSession.Project, NewBodygroupNameTextBox.Text, [""]);
            RefreshFromShell();
        }

        private void AddBodygroupOptionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement button)
                return;
            if (button.Tag is not BodygroupEditorItem editorItem)
                return;
            var project = Host.Shell.CurrentSession?.Project;
            if (project is null)
                return;

            Host.Backend.ProjectEditor.AddBodygroupOption(project, editorItem.BodygroupIndex, "");

            RefreshFromShell();
        }

        private void RemoveBodygroupOptionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement button)
                return;

            if (button.Tag is not BodygroupOptionItem optionItem)
                return;

            var project = Host.Shell.CurrentSession?.Project;
            if (project is null)
                return;

            Host.Backend.ProjectEditor.RemoveBodygroupOption(
                project,
                optionItem.ParentBodygroup.BodygroupIndex,
                optionItem.OptionIndex);

            RefreshFromShell();
        }

        private void RemoveBodygroupButton_Click(object sender, RoutedEventArgs e)
        {
            // Remove this bodygroup from the project Qc.Bodygroups list, then refresh the form to update the UI

            // Get the bodygroup item associated with the clicked button
            if (sender is not FrameworkElement button)
                return;

            if (button.Tag is not BodygroupEditorItem bodygroupItem)
                return;

            var project = Host.Shell.CurrentSession?.Project;
            if (project is null)
                return;

            Host.Backend.ProjectEditor.RemoveBodygroup(project, bodygroupItem.BodygroupIndex);

            RefreshFromShell();
        }

        private void BodygroupNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_isRefreshing || Host.Shell.CurrentSession?.Project is null)
                return;

            if (sender is not TextBox textBox || textBox.Tag is not BodygroupEditorItem editorItem)
                return;

            var project = Host.Shell.CurrentSession.Project;
            var value = textBox.Text;

            // Screw it manual renaming
            project.Qc.Bodygroups[editorItem.BodygroupIndex].Name = value;

            RefreshFromShell();
        }

        private void BodygroupOptionTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_isRefreshing || Host.Shell.CurrentSession?.Project is null)
                return;

            if (sender is not TextBox textBox || textBox.Tag is not BodygroupOptionItem optionItem)
                return;

            var value = textBox.Text;
            optionItem.MeshName = value;

            var submeshes = optionItem.ParentBodygroup.SourceBodygroup.Submeshes;
            if (optionItem.OptionIndex < 0 || optionItem.OptionIndex >= submeshes.Count)
                return;

            submeshes[optionItem.OptionIndex] = value;
        }


        private void Shell_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Host.Shell.CurrentSession)
                || e.PropertyName == nameof(Host.Shell.HasOpenProject))
            {
                RefreshFromShell();
            }
        }
        private void RefreshFromShell()
        {
            // Update the UI based on whether a project is open or not
            NoProjectPanel.Visibility = Host.Shell.HasOpenProject ? Visibility.Collapsed : Visibility.Visible;
            ProjectSettingsPanel.Visibility = Host.Shell.HasOpenProject ? Visibility.Visible : Visibility.Collapsed;

            // If there is no open project, clear the form fields
            if (!Host.Shell.HasOpenProject || Host.Shell.CurrentSession?.Project is null)
            {
                ClearForm();
                return;
            }

            PopulateFromCurrentProject(Host.Shell.CurrentSession.Project);
        }

        private void ClearForm()
        {
            _isRefreshing = true;
            // Clear all form fields and reset to default state
            ProjectNameTextBox.Text = string.Empty;
            SurfacePropComboBox.Text = string.Empty;
            OutputDirectoryTextBox.Text = string.Empty;
            ModelPathTextBox.Text = string.Empty;
            AnimationProfileComboBox.SelectedItem = AnimationProfile.None;

            MaterialPathItems.Clear();
            BodygroupItems.Clear();
            _isRefreshing = false;
        }

        private void PopulateFromCurrentProject(Project project)
        {
            _isRefreshing = true;
            ProjectNameTextBox.Text = project.Name ?? string.Empty;
            SurfacePropComboBox.Text = project.Qc.SurfaceProp ?? string.Empty;
            SurfacePropComboBox.SelectedItem = project.Qc.SurfaceProp;
            OutputDirectoryTextBox.Text = project.Build.OutputDirectory ?? string.Empty;
            ModelPathTextBox.Text = project.Qc.ModelPath ?? string.Empty;
            AnimationProfileComboBox.SelectedItem = project.Qc.AnimationProfile;

            MaterialPathItems.Clear();
            foreach (var materialPath in project.Qc.CdMaterialsPaths)
            {
                MaterialPathItems.Add(new MaterialPathEditorItem
                {
                    MaterialPathIndex = MaterialPathItems.Count,
                    Path = materialPath
                });
            }


            // Populate the BodygroupItems collection based on the project's Qc.Bodygroups
            BodygroupItems.Clear();
            foreach (var bodygroup in project.Qc.Bodygroups)
            {
                var editorItem = new BodygroupEditorItem
                {
                    BodygroupIndex = BodygroupItems.Count,
                    Name = bodygroup.Name,
                    SourceBodygroup = bodygroup,
                    Options = new ObservableCollection<BodygroupOptionItem>(
                        bodygroup.Submeshes.Select((submesh, index) => new BodygroupOptionItem
                        {
                            ParentBodygroup = null!, // This will be set to the editorItem after it's created
                            MeshName = submesh,
                            OptionIndex = index
                        }))
                };

                // Set the ParentBodygroup for each option
                foreach (var option in editorItem.Options)
                {
                    option.ParentBodygroup = editorItem;
                }

                BodygroupItems.Add(editorItem);
            }
            _isRefreshing = false;
        }



        private void ProjectPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Host.Shell.PropertyChanged -= Shell_PropertyChanged;
        }
    }
}

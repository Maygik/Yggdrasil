using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Yggdrasil.Application.UseCases;
using Yggdrasil.Domain.Project;
using Yggdrasil.Presentation.Services;

namespace Yggdrasil.Presentation.Pages
{
    public sealed partial class ExportPage : Page
    {
        public AppHost Host { get; }
        public ObservableCollection<string> ExportMessages { get; } = new();

        private bool _isRefreshing;
        private bool _isExporting;

        public ExportPage()
        {
            Host = App.Instance.Host;
            InitializeComponent();

            Host.Shell.PropertyChanged += Shell_PropertyChanged;
            Unloaded += ExportPage_Unloaded;

            RefreshFromShell();
        }

        private void Shell_PropertyChanged(object? sender, PropertyChangedEventArgs e)
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
            ExportPanel.Visibility = Host.Shell.HasOpenProject ? Visibility.Visible : Visibility.Collapsed;

            if (!Host.Shell.HasOpenProject || Host.Shell.CurrentSession?.Project is null)
            {
                ClearForm();
                return;
            }

            PopulateFromCurrentProject(Host.Shell.CurrentSession.Project);
            UpdateExportAvailability();
        }

        private void PopulateFromCurrentProject(Project project)
        {
            _isRefreshing = true;

            OutputDirectoryTextBox.Text = project.Build.OutputDirectory ?? string.Empty;
            ResolvedOutputDirectoryTextBlock.Text = ResolveOutputDirectory(project);

            ExportQcCheckBox.IsEnabled = true;
            ExportQcCheckBox.IsChecked = true;

            ExportMeshesAndAnimationsCheckBox.IsEnabled = Host.Shell.CanExportModel;
            ExportMeshesAndAnimationsCheckBox.IsChecked = Host.Shell.CanExportModel;

            ExportMaterialsCheckBox.IsEnabled = false;
            ExportMaterialsCheckBox.IsChecked = false;

            ExportActionHintTextBlock.Text = Host.Shell.CanExportModel
                ? "Configure the destination and export options here. The export action itself is still not implemented."
                : "QC export can be configured now. Mesh and animation export will unlock after a model is imported.";

            _isRefreshing = false;
        }

        private void ClearForm()
        {
            _isRefreshing = true;

            OutputDirectoryTextBox.Text = string.Empty;
            ResolvedOutputDirectoryTextBlock.Text = string.Empty;
            ExportQcCheckBox.IsChecked = false;
            ExportQcCheckBox.IsEnabled = false;
            ExportMeshesAndAnimationsCheckBox.IsChecked = false;
            ExportMeshesAndAnimationsCheckBox.IsEnabled = false;
            ExportMaterialsCheckBox.IsChecked = false;
            ExportActionHintTextBlock.Text = string.Empty;
            ExportButton.IsEnabled = false;
            ExportProgressRing.IsActive = false;
            ExportProgressRing.Visibility = Visibility.Collapsed;
            ResetExportResults();

            _isRefreshing = false;
        }

        private void UpdateExportAvailability()
        {
            var hasOutputDirectory = !string.IsNullOrWhiteSpace(Host.Shell.CurrentSession?.Project?.Build.OutputDirectory);
            var hasSelection = ExportQcCheckBox.IsChecked == true
                || (ExportMeshesAndAnimationsCheckBox.IsEnabled && ExportMeshesAndAnimationsCheckBox.IsChecked == true);

            ExportButton.IsEnabled = Host.Shell.HasOpenProject
                && hasOutputDirectory
                && hasSelection
                && !_isExporting;
            ExportProgressRing.IsActive = _isExporting;
            ExportProgressRing.Visibility = _isExporting ? Visibility.Visible : Visibility.Collapsed;

            if (!Host.Shell.HasOpenProject)
            {
                ExportActionHintTextBlock.Text = string.Empty;
            }
            else if (!hasOutputDirectory)
            {
                ExportActionHintTextBlock.Text = "Set an output directory before exporting.";
            }
            else if (!hasSelection)
            {
                ExportActionHintTextBlock.Text = "Select at least one export option.";
            }
            else if (!Host.Shell.CanExportModel)
            {
                ExportActionHintTextBlock.Text = "QC export can be configured now. Mesh and animation export will unlock after a model is imported.";
            }
            else
            {
                ExportActionHintTextBlock.Text = "Ready to export to the configured output directory.";
            }

        }

        private string ResolveOutputDirectory(Project project)
        {
            var outputDirectory = project.Build.OutputDirectory;
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                return "Set an output directory to choose where exported files should be written.";
            }

            if (!Path.IsPathRooted(outputDirectory) && !string.IsNullOrWhiteSpace(project.Directory))
            {
                return $"Resolved path: {Path.GetFullPath(Path.Combine(project.Directory, outputDirectory))}";
            }

            return $"Resolved path: {outputDirectory}";
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

            ResolvedOutputDirectoryTextBlock.Text = ResolveOutputDirectory(project);
            UpdateExportAvailability();
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

            // Suggested location is the current path set in the project, if that's null then documents
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
            ResolvedOutputDirectoryTextBlock.Text = ResolveOutputDirectory(project);
            UpdateExportAvailability();
        }

        private void ExportOptionCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_isRefreshing)
                return;

            UpdateExportAvailability();
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isExporting || Host.Shell.CurrentSession is null)
                return;

            var request = new ExportBuildRequest
            {
                Project = Host.Shell.CurrentSession.Project,
                exportMeshes = ExportMeshesAndAnimationsCheckBox.IsChecked == true,
                exportQc = ExportQcCheckBox.IsChecked == true
            };

            try
            {
                _isExporting = true;
                ResetExportResults();
                UpdateExportAvailability();

                var result = await Task.Run(() => Host.Backend.ExportBuild.Execute(request));
                RenderExportResult(result);

                if (!result.Success)
                {
                    Host.Shell.StatusMessage = $"Export failed: {result.ErrorMessage ?? "Unknown error."}";
                }
                else if (result.HasWarnings)
                {
                    Host.Shell.StatusMessage = "Export completed with warnings.";
                }
                else
                {
                    Host.Shell.StatusMessage = "Export completed successfully.";
                }
            }
            catch (Exception ex)
            {
                var failedResult = new ExportBuildResult(false, ex.Message);
                failedResult.Messages.Add("An unexpected exception occurred during export.");
                RenderExportResult(failedResult);
                Host.Shell.StatusMessage = $"Export failed: {ex.Message}";
            }
            finally
            {
                _isExporting = false;
                UpdateExportAvailability();
            }
        }

        private void ResetExportResults()
        {
            ExportMessages.Clear();
            ResultsPanel.Visibility = Visibility.Collapsed;
            ExportResultInfoBar.IsOpen = false;
            ExportResultInfoBar.Title = string.Empty;
            ExportResultInfoBar.Message = string.Empty;
            ExportResultInfoBar.Severity = InfoBarSeverity.Informational;
        }

        private void RenderExportResult(ExportBuildResult result)
        {
            ResetExportResults();
            ResultsPanel.Visibility = Visibility.Visible;
            ExportResultInfoBar.IsOpen = true;

            if (!result.Success)
            {
                ExportResultInfoBar.Title = "Export failed";
                ExportResultInfoBar.Severity = InfoBarSeverity.Error;
                ExportResultInfoBar.Message = result.ErrorMessage ?? "An unknown export error occurred.";
            }
            else if (result.HasWarnings)
            {
                ExportResultInfoBar.Title = "Export completed with warnings";
                ExportResultInfoBar.Severity = InfoBarSeverity.Warning;
                ExportResultInfoBar.Message = result.Warnings.FirstOrDefault() ?? "Export completed with warnings.";
            }
            else
            {
                ExportResultInfoBar.Title = "Export complete";
                ExportResultInfoBar.Severity = InfoBarSeverity.Success;
                ExportResultInfoBar.Message = result.Messages.FirstOrDefault() ?? "Export completed successfully.";
            }

            if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
            {
                ExportMessages.Add($"Error: {result.ErrorMessage}");
            }

            foreach (var message in result.Messages)
            {
                ExportMessages.Add(message);
            }

            foreach (var warning in result.Warnings)
            {
                ExportMessages.Add($"Warning: {warning}");
            }
        }

        private void ExportPage_Unloaded(object sender, RoutedEventArgs e)
        {
            Host.Shell.PropertyChanged -= Shell_PropertyChanged;
        }
    }
}

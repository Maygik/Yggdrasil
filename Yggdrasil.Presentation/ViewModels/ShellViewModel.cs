using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Yggdrasil.Application;
using Yggdrasil.Application.UseCases;
using Yggdrasil.Presentation.Models;
using Yggdrasil.Presentation.Services;

namespace Yggdrasil.Presentation.ViewModels
{
    /// <summary>
    /// Hold the main state for the app shell
    /// Current session, navigation destination, title, project loaded state etc
    /// </summary>
    public class ShellViewModel : INotifyPropertyChanged
    {
        private readonly AppServices _backend;
        private readonly FileDialogService _fileDialogs;
        private readonly RecentProjectsService _recentProjects;

        public ShellViewModel(AppServices backend, FileDialogService fileDialogs, RecentProjectsService recentProjects) 
        {
            _backend = backend;
            _fileDialogs = fileDialogs;
            _recentProjects = recentProjects;
        }

        private ProjectSessionViewModel? _currentSession;
        public ProjectSessionViewModel? CurrentSession
        {
            get => _currentSession;
            private set
            {
                if (_currentSession == value) return;

                _currentSession = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(WindowTitle));
                OnPropertyChanged(nameof(HasOpenProject));
                OnPropertyChanged(nameof(CanImportModel));
                OnPropertyChanged(nameof(CanExportModel));
                OnPropertyChanged(nameof(CurrentProjectName));
                OnPropertyChanged(nameof(CurrentProjectPath));
            }
        }

        private IReadOnlyList<RecentProjectEntry> _recentProjectsList = Array.Empty<RecentProjectEntry>();
        public IReadOnlyList<RecentProjectEntry> RecentProjects
        {
            get => _recentProjectsList;
            private set
            {
                if (_recentProjectsList == value) return;
                _recentProjectsList = value;
                OnPropertyChanged();
            }
        }


        public string AppTitle => "Yggdrasil";
        public string WindowTitle => CurrentSession != null ? $"{AppTitle} - {CurrentSession.DisplayName}" : AppTitle;
        public bool HasOpenProject => CurrentSession != null;
        public bool CanImportModel => CurrentSession != null;
        public bool CanExportModel =>
            CurrentSession?.Project?.Scene is { } scene
            && (scene.RootBone is not null || scene.MeshGroups.Count > 0);
        public string CurrentProjectName => CurrentSession?.DisplayName ?? "No project open";
        public string CurrentProjectPath => CurrentSession?.ProjectFilePath ?? "Open or create a project to get started.";

        private string? _statusMessage;
        public string? StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage == value) return;
                
                _statusMessage = value;
                OnPropertyChanged();
            }
        }


        public async Task CreateProjectAsync(FrameworkElement owner)
        {
            var request = await _fileDialogs.ShowCreateProjectDialogAsync(owner);
            if (request is null) return;

            var result = _backend.CreateProject.Execute(request.Value);

            // Null/Error checks
            if (!result.Success)
            {
                StatusMessage = "Failed to create project.";
                return;
            }
            else if (result.CreatedProject is null)
            {
                StatusMessage = "Project creation succeeded but no project was returned.";
                return;
            }
            else if (string.IsNullOrWhiteSpace(result.ProjectFilePath))
            {
                StatusMessage = "Project creation succeeded but no file path was returned.";
                return;
            }


            CurrentSession = new ProjectSessionViewModel(result.CreatedProject, result.ProjectFilePath);
            RecentProjects = await _recentProjects.AddOrUpdateAsync(new RecentProjectEntry
            { 
                Name = CurrentSession.DisplayName,          // Use the display name as the recent project name
                FilePath = CurrentSession.ProjectFilePath,  // Store the file path for future reference
                LastOpened = DateTimeOffset.Now,            // Update the last opened time
                IsMissing = false                           // We just created it, so it can't be missing
            }
            );

            StatusMessage = result.Messages.FirstOrDefault() ?? $"Project {result.CreatedProject.Name} created successfully.";
        }

        public async Task OpenProjectAsync()
        {
            var projectPath = await _fileDialogs.ShowOpenProjectDialogAsync();

            if (string.IsNullOrWhiteSpace(projectPath))
            {
                StatusMessage = "No project file selected.";
                return;
            }

            await OpenprojectFromPathAsync(projectPath);
        }

        public async Task OpenprojectFromPathAsync(string projectPath)
        {
            var request = new OpenProjectRequest { ProjectFilePath = projectPath };
            var result = _backend.OpenProject.Execute(request);
            if (!result.Success)
            {
                StatusMessage = $"Failed to open project: {result.ErrorMessage ?? "Unknown error."}";
                return;
            }
            else if (result.OpenedProject is null)
            {
                StatusMessage = "Project opening succeeded but no project was returned.";
                return;
            }

            CurrentSession = new ProjectSessionViewModel(result.OpenedProject, projectPath);
            RecentProjects = await _recentProjects.AddOrUpdateAsync(new RecentProjectEntry
            {
                Name = CurrentSession.DisplayName,          // Use the display name as the recent project name
                FilePath = CurrentSession.ProjectFilePath,  // Store the file path for future reference
                LastOpened = DateTimeOffset.Now,            // Update the last opened time
                IsMissing = false                           // We just opened it successfully, so it can't be missing
            });

            StatusMessage = result.Messages.FirstOrDefault() ?? $"Project {result.OpenedProject.Name} opened successfully.";
        }

        public async Task OpenRecentProjectAsync(RecentProjectEntry entry)
        {
            if (entry.IsMissing)
            {
                StatusMessage = $"Cannot open project '{entry.Name}' because the file is missing.";
                return;
            }
            await OpenprojectFromPathAsync(entry.FilePath);
        }

        public void SaveProject()
        {
            if (CurrentSession?.Project is null || string.IsNullOrWhiteSpace(CurrentSession.ProjectFilePath))
            {
                StatusMessage = "No project is currently open.";
                return;
            }

            var result = _backend.SaveProject.Execute(new SaveProjectRequest
            {
                Project = CurrentSession.Project
            });

            if (!result.Success)
            {
                StatusMessage = $"Failed to save project: {result.ErrorMessage ?? "Unknown error."}";
                return;
            }
            else
            {
                StatusMessage = result.Messages.FirstOrDefault() ?? $"Project {CurrentSession.Project.Name} saved successfully.";
            }
        }

        public void ImportModel(string modelPath, bool autoMap)
        {
            if (CurrentSession?.Project is null)
            {
                StatusMessage = "No project is currently open.";
                return;
            }

            if (string.IsNullOrWhiteSpace(modelPath))
            {
                StatusMessage = "Model path cannot be empty.";
                return;
            }

            var request = new ImportModelRequest(modelPath, CurrentSession.Project, autoMap);
            var result = _backend.ImportModel.Execute(request);

            if (!result.Success)
            {
                StatusMessage = $"Failed to import model: {result.ErrorMessage ?? "Unknown error."}";
                return;
            }

            StatusMessage = result.Messages.FirstOrDefault() ?? $"Imported model '{modelPath}'.";

            // The project session object stays the same, but several UI surfaces depend on
            // the scene/imported-model state changing underneath it.
            OnPropertyChanged(nameof(CurrentSession));
            OnPropertyChanged(nameof(CanExportModel));
        }

        public void SetCurrentSession(ProjectSessionViewModel? session)
        {
            CurrentSession = session;
        }

        public async Task InitializeAsync()
        {
            RecentProjects = await _recentProjects.LoadAsync();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}

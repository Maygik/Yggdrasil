using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Yggdrassil.Application;
using Yggdrassil.Presentation.Services;

namespace Yggdrassil.Presentation.ViewModels
{
    /// <summary>
    /// Hold the main state for the app shell
    /// Current session, navigation destination, title, project loaded state etc
    /// </summary>
    public class ShellViewModel : INotifyPropertyChanged
    {
        private readonly AppServices _backend;
        private readonly FileDialogService _fileDialogs;

        public ShellViewModel(AppServices backend, FileDialogService fileDialogs) 
        {
            _backend = backend;
            _fileDialogs = fileDialogs;
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
            }
        }

        public string AppTitle => "Yggdrassil";
        public string WindowTitle => CurrentSession != null ? $"{AppTitle} - {CurrentSession.DisplayName}" : AppTitle;
        public bool HasOpenProject => CurrentSession != null;
        public bool CanImportModel => CurrentSession != null;
        public bool CanExportModel => CurrentSession?.Project?.Scene != null;

        NavigationViewItem CurrentPage;

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


        public async Task CreateProjectAsync()
        {
            throw new NotImplementedException();
        }

        public async Task OpenProjectAsync()
        {
            throw new NotImplementedException();
        }

        public async Task SaveProjectAsync()
        {
            throw new NotImplementedException();
        }

        public async Task ImportModelAsync()
        {
            throw new NotImplementedException();
        }

        public void SetCurrentSession(ProjectSessionViewModel? session)
        {
            CurrentSession = session;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}

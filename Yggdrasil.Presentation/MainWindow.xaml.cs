using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Yggdrasil.Presentation.Pages;
using Yggdrasil.Presentation.Services;
using Yggdrasil.Presentation.ViewModels;
using Yggdrasil.Renderer.Scene;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Yggdrasil.Presentation
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public AppHost Host { get; }

        public MainWindow(AppHost host)
        {
            Host = host;
            InitializeComponent();
            ApplyWindowIcon();

            Title = Host.Shell.WindowTitle;
            Host.Shell.PropertyChanged += OnShellPropertyChanged;
            ContentFrame.NavigationFailed += ContentFrame_NavigationFailed;
            MainNavigationView.SelectedItem = HomeNavigationItem;
            ContentFrame.Navigate(typeof(HomePage));
            Activated += MainWindow_Activated;
            Closed += MainWindow_Closed;
        }

        private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            Activated -= MainWindow_Activated;
            await Host.Shell.InitializeAsync();


            ViewportControl.Connect(Host.Viewport);
            SyncViewportScene();
            SyncViewportSelection();
        }

        private void ApplyWindowIcon()
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Yggdrasil.ico");
            if (!File.Exists(iconPath))
                return;

            try
            {
                AppWindow.SetIcon(iconPath);
                AppWindow.SetTaskbarIcon(iconPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private void OnShellPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ShellViewModel.WindowTitle))
            {
                Title = Host.Shell.WindowTitle;
            }

            if (e.PropertyName == nameof(ShellViewModel.CurrentSession)
                || e.PropertyName == nameof(ShellViewModel.HasOpenProject)
                || e.PropertyName == nameof(ShellViewModel.CanExportModel))
            {
                SyncViewportScene();
                SyncViewportSelection();
            }

            if (e.PropertyName == nameof(ShellViewModel.SelectedMaterialName)
                || e.PropertyName == nameof(ShellViewModel.HoveredMaterialName))
            {
                SyncViewportSelection();
            }
        }

        private async void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            Closed -= MainWindow_Closed;
            Host.Shell.PropertyChanged -= OnShellPropertyChanged;
            ContentFrame.NavigationFailed -= ContentFrame_NavigationFailed;

            try
            {
                await ViewportControl.DisconnectAsync();
                await Host.Renderer.DisposeAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private async void MainNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                return;
            }

            if (args.SelectedItemContainer?.Tag is not string tag)
            {
                return;
            }

            var pageType = GetPageType(tag);
            if (ContentFrame.CurrentSourcePageType != pageType)
            {
                try
                {
                    var navigated = ContentFrame.Navigate(pageType);
                    if (!navigated)
                    {
                        await ReportNavigationErrorAsync($"Navigation to {pageType.Name} returned false.");
                    }
                }
                catch (Exception ex)
                {
                    await ReportNavigationErrorAsync($"Navigation to {pageType.Name} failed: {ex.Message}", ex);
                }
            }
        }

        private async void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            var pageName = e.SourcePageType?.Name ?? "the requested page";
            await ReportNavigationErrorAsync($"Navigation to {pageName} failed: {e.Exception?.Message ?? "Unknown error."}", e.Exception);
        }

        private async Task ReportNavigationErrorAsync(string message, Exception? exception = null)
        {
            Host.Shell.StatusMessage = message;
            System.Diagnostics.Debug.WriteLine(exception?.ToString() ?? message);

            if (ContentFrame.XamlRoot is null)
                return;

            try
            {
                var dialog = new ContentDialog
                {
                    Title = "Navigation Error",
                    Content = message,
                    CloseButtonText = "OK",
                    XamlRoot = ContentFrame.XamlRoot
                };

                await dialog.ShowAsync();
            }
            catch (Exception dialogException)
            {
                System.Diagnostics.Debug.WriteLine(dialogException);
            }
        }

        private static Type GetPageType(string? tag) => tag switch
        {
            "home" => typeof(HomePage),
            "project" => typeof(ProjectPage),
            "model" => typeof(ModelPage),
            "materials" => typeof(MaterialsPage),
            "rigging" => typeof(RiggingPage),
            "viewport" => typeof(ViewportPage),
            "export" => typeof(ExportPage),
            _ => typeof(HomePage),
        };

        private void SyncViewportScene()
        {
            Host.Viewport.SetScene(Host.Shell.CurrentSession?.Project?.Scene);
            ViewportControl.SetCameraState(Host.Viewport.CurrentCameraState);
            ViewportControl.SetLightState(Host.Viewport.CurrentLightState);
        }

        private void SyncViewportSelection()
        {
            Host.Viewport.UpdateSelection(new RenderSelectionState
            {
                SelectedMaterialName = Host.Shell.SelectedMaterialName,
                HoveredMaterialName = Host.Shell.HoveredMaterialName
            });
        }
    }
}

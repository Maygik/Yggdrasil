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
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Yggdrasil.Presentation.Pages;
using Yggdrasil.Presentation.Services;
using Yggdrasil.Presentation.ViewModels;

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

            Title = Host.Shell.WindowTitle;
            Host.Shell.PropertyChanged += OnShellPropertyChanged;
            ContentFrame.NavigationFailed += ContentFrame_NavigationFailed;
            MainNavigationView.SelectedItem = HomeNavigationItem;
            ContentFrame.Navigate(typeof(HomePage));
            Activated += MainWindow_Activated;
        }

        private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            Activated -= MainWindow_Activated;
            await Host.Shell.InitializeAsync();
        }

        private void OnShellPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ShellViewModel.WindowTitle))
            {
                Title = Host.Shell.WindowTitle;
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
            "export" => typeof(ExportPage),
            _ => typeof(HomePage),
        };
    }
}

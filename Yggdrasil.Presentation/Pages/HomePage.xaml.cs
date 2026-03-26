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
using Windows.Foundation;
using Windows.Foundation.Collections;
using Yggdrasil.Presentation.Models;
using Yggdrasil.Presentation.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Yggdrasil.Presentation.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        public AppHost Host { get; }

        public HomePage()
        {
            Host = App.Instance.Host;
            InitializeComponent();
        }




        private async void NewProjectButton_Click(object sender, RoutedEventArgs e)
        {
            // Need a FrameWorkorkElement to show the dialog, so we can use the button that was clicked as the parent
            // Get the button that was clicked
            if (sender is not FrameworkElement button)
                throw new InvalidOperationException("The sender of the New Project button click event must be a FrameworkElement.");

            await Host.Shell.CreateProjectAsync(button);
        }

        private async void OpenProjectButton_Click(object sender, RoutedEventArgs e)
        {
            await Host.Shell.OpenProjectAsync();
        }

        private async void SaveProjectButton_Click(object sender, RoutedEventArgs e)
        {
            if (Host == null)
                throw new InvalidOperationException("AppHost is not available.");

            if (!Host.Shell.HasOpenProject)
                throw new InvalidOperationException("No project is currently open. Cannot save.");
            else
            {
                // A project is currently open, so we can save it directly
                Host.Shell.SaveProject();
            }
        }

        private async void RecentProjectsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            // Open the selected recent project
            if (Host == null)
                throw new InvalidOperationException("AppHost is not available.");

            // Get the RecentProjectsEntry from the clicked item
            if (e.ClickedItem is RecentProjectEntry entry)
            {
                // Use the AppHost to open the project
                await Host.Shell.OpenprojectFromPathAsync(entry.FilePath);
            }

        }
    }
}

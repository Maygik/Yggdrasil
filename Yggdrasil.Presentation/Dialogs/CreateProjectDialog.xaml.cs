using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Yggdrasil.Presentation.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CreateProjectDialog : ContentDialog
    {
        public CreateProjectDialog()
        {
            InitializeComponent();
        }
        
        public async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (string.IsNullOrWhiteSpace(ProjectName) || string.IsNullOrWhiteSpace(ProjectDirectory))
            {
                args.Cancel = true;

                await new ContentDialog
                {
                    Title = "Invalid Input",
                    Content = "Please enter a valid project name and directory.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
            }
        }

        public async void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.Instance.MainWindow is null)
                return;

            var hWnd = WindowNative.GetWindowHandle(App.Instance.MainWindow);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);

            // Make the picker
            // Have it start in the last used directory, or the documents folder if we don't have one
            var picker = new FolderPicker(windowId)
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };

            var folder = await picker.PickSingleFolderAsync();
            if (folder is not null)
            {
                ProjectDirectoryTextBox.Text = folder.Path;
            }
        }

        public string ProjectName => ProjectNameTextBox.Text.Trim();
        public string ProjectDirectory => ProjectDirectoryTextBox.Text.Trim();
    }
}

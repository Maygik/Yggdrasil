using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinRT.Interop;
using Yggdrassil.Application.UseCases;
using Yggdrassil.Presentation.Dialogs;

namespace Yggdrassil.Presentation.Services
{
    public class FileDialogService
    {
        public async Task<string?> ShowOpenProjectDialogAsync()
        {
            if (App.Instance.MainWindow is null)
                return null;

            var hWnd = WindowNative.GetWindowHandle(App.Instance.MainWindow);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);

            // Make the file picker
            var picker = new Microsoft.Windows.Storage.Pickers.FileOpenPicker(windowId)
            {
                ViewMode = Microsoft.Windows.Storage.Pickers.PickerViewMode.List,
                SuggestedStartLocation = Microsoft.Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
            };

            // Filter to only show project files
            picker.FileTypeFilter.Add(".yggproj");

            var file = await picker.PickSingleFileAsync();
            if (file is not null)
            {
                return file.Path;
            }

            return null;
        }

        public async Task<CreateProjectRequest?> ShowCreateProjectDialogAsync(FrameworkElement owner)
        {
            var dialog = new CreateProjectDialog();
            dialog.XamlRoot = owner.XamlRoot;

            var result = await dialog.ShowAsync();

            // If the user cancels the dialog, return null
            if (result != Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
            {
                return null;
            }

            return new CreateProjectRequest
            {
                Name = dialog.ProjectName,
                ProjectDirectory = dialog.ProjectDirectory
            };
        }

        public async Task<string?> ShowImportModelDialogAsync()
        {
            if (App.Instance.MainWindow is null)
                return null;

            var hWnd = WindowNative.GetWindowHandle(App.Instance.MainWindow);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);

            var picker = new Microsoft.Windows.Storage.Pickers.FileOpenPicker(windowId)
            {
                ViewMode = Microsoft.Windows.Storage.Pickers.PickerViewMode.List,
                SuggestedStartLocation = Microsoft.Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary
            };

            picker.FileTypeFilter.Add(".fbx");
            picker.FileTypeFilter.Add(".smd");
            picker.FileTypeFilter.Add(".dmx");
            picker.FileTypeFilter.Add(".obj");
            picker.FileTypeFilter.Add(".dae");
            picker.FileTypeFilter.Add(".blend");
            picker.FileTypeFilter.Add(".gltf");
            picker.FileTypeFilter.Add(".glb");

            var file = await picker.PickSingleFileAsync();
            return file?.Path;
        }
    }
}

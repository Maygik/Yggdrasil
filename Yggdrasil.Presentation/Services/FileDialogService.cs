using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Yggdrasil.Application.UseCases;
using Yggdrasil.Presentation.Dialogs;
using WinRT.Interop;

namespace Yggdrasil.Presentation.Services
{
    /// <summary>
    /// Handles showing file and folder dialogs using the Windows API Code Pack's CommonOpenFileDialog for a modern file dialog experience.
    /// </summary>
    public class FileDialogService
    {
        private const int HResultCanceled = unchecked((int)0x800704C7); // HRESULT for "The operation was canceled by the user." Standard across file dialogs.

        private readonly PickerLocationService _pickerLocations = new(); // Saves the last used directories for different dialog types

        // Shows a file open dialog for opening Yggdrasil project files (.yggproj) with remembered last directory and appropriate filters.
        public async Task<string?> ShowOpenProjectDialogAsync()
        {
            return await ShowOpenFileDialogAsync(
                title: "Open Yggdrasil Project",
                historyKey: PickerSettingsIds.OpenProject,
                defaultDirectory: GetDefaultDocumentsDirectory(),
                filters:
                [
                    new FileDialogFilter("Yggdrasil Projects", "*.yggproj")
                ]);
        }

        // Shows a content dialog for creating a new project, allowing the user to specify a project name and directory. Returns null if the user cancels.
        public async Task<CreateProjectRequest?> ShowCreateProjectDialogAsync(FrameworkElement owner)
        {
            var dialog = new CreateProjectDialog();
            dialog.XamlRoot = owner.XamlRoot;

            var result = await dialog.ShowAsync();

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

        // Shows a file open dialog for importing 3D model files with remembered last directory and appropriate filters for common 3D model formats.
        // TODO: Check that the formats all work
        public async Task<string?> ShowImportModelDialogAsync()
        {
            return await ShowOpenFileDialogAsync(
                title: "Import Model",
                historyKey: PickerSettingsIds.ImportModel,
                defaultDirectory: GetDefaultDocumentsDirectory(),
                filters:
                [
                    new FileDialogFilter("Model Files", "*.fbx;*.smd;*.dmx;*.obj;*.dae;*.blend;*.gltf;*.glb")
                ]);
        }

        // Shows a file open dialog for selecting texture files with remembered last directory and appropriate filters for common image formats used as textures.
        public async Task<string?> ShowOpenTextureDialogAsync()
        {
            return await ShowOpenFileDialogAsync(
                title: "Select Texture",
                historyKey: PickerSettingsIds.MaterialEditorTexture,
                defaultDirectory: GetDefaultPicturesDirectory(),
                filters:
                [
                    new FileDialogFilter("Texture Files", "*.png;*.tga;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.webp;*.dds;*.gif;*.psd")
                ]);
        }

        // Shows a folder picker dialog for selecting a directory, with remembered last directory and fallback to documents if needed. Returns null if the user cancels.
        public async Task<string?> ShowFolderDialogAsync(string title, string historyKey, string? fallbackDirectory = null)
        {
            var startDirectory = ResolveStartDirectory(historyKey, fallbackDirectory, GetDefaultDocumentsDirectory());
            var selectedPath = ShowPathDialog(
                title,
                FOS.PICKFOLDERS | FOS.PATHMUSTEXIST | FOS.FORCEFILESYSTEM,
                historyKey,
                startDirectory,
                filters: null);

            return await Task.FromResult(selectedPath);
        }

        // Shows a file open dialog for selecting a single file, with remembered last directory and specified filters. Returns null if the user cancels.
        private async Task<string?> ShowOpenFileDialogAsync(
            string title,
            string historyKey,
            string defaultDirectory,
            FileDialogFilter[] filters,
            string? fallbackDirectory = null)
        {
            var startDirectory = ResolveStartDirectory(historyKey, fallbackDirectory, defaultDirectory);
            var selectedPath = ShowPathDialog(
                title,
                FOS.FILEMUSTEXIST | FOS.PATHMUSTEXIST | FOS.FORCEFILESYSTEM,
                historyKey,
                startDirectory,
                filters);

            return await Task.FromResult(selectedPath);
        }

        // Core method that shows either a file or folder dialog based on the options provided.
        // It handles COM interop with the Windows API, sets up the dialog options, filters, and starting directory, and returns the selected path or null if canceled.
        private string? ShowPathDialog(
            string title,
            FOS optionsToAdd,
            string historyKey,
            string startDirectory,
            FileDialogFilter[]? filters)
        {
            IFileOpenDialog? dialog = null;
            IShellItem? startFolder = null;
            IShellItem? resultItem = null;

            try
            {
                // Create the file open dialog COM object
                dialog = (IFileOpenDialog)new NativeFileOpenDialog();
                dialog.GetOptions(out var existingOptions);
                dialog.SetOptions(existingOptions | optionsToAdd | FOS.NOCHANGEDIR);
                dialog.SetTitle(title);

                // Set up file type filters if provided
                if (filters is { Length: > 0 })
                {
                    var nativeFilters = new COMDLG_FILTERSPEC[filters.Length];
                    for (var i = 0; i < filters.Length; i++)
                    {
                        nativeFilters[i] = new COMDLG_FILTERSPEC
                        {
                            pszName = filters[i].Name,
                            pszSpec = filters[i].Spec
                        };
                    }

                    dialog.SetFileTypes((uint)nativeFilters.Length, nativeFilters);
                    dialog.SetFileTypeIndex(1);
                }

                // Try to set the starting folder of the dialog
                startFolder = TryCreateShellItem(startDirectory);
                if (startFolder is not null)
                {
                    dialog.SetFolder(startFolder);
                    dialog.SetDefaultFolder(startFolder);
                }

                // Show the dialog and check the result
                var result = dialog.Show(GetOwnerWindowHandle());
                if (result == HResultCanceled)
                {
                    return null;
                }

                // If the result is not success, throw an exception
                Marshal.ThrowExceptionForHR(result);

                // Get the selected item and extract its file system path
                dialog.GetResult(out resultItem);
                resultItem.GetDisplayName(SIGDN.FILESYSPATH, out var selectedPath);

                if (string.IsNullOrWhiteSpace(selectedPath))
                {
                    return null;
                }

                // Remember the directory for next time. If the user selected a file, remember its parent directory. If they selected a folder, remember that folder.
                var directoryToRemember = Directory.Exists(selectedPath)
                    ? selectedPath
                    : Path.GetDirectoryName(selectedPath);

                _pickerLocations.SaveDirectory(historyKey, directoryToRemember);
                return selectedPath;
            }
            finally
            {
                // Clean up COM objects to prevent memory leaks
                ReleaseComObject(resultItem);
                ReleaseComObject(startFolder);
                ReleaseComObject(dialog);
            }
        }

        // Resolves the starting directory for a file/folder dialog by checking the last used directory for the given history key, then a provided fallback directory, and finally a default directory if neither of the first two exist.
        private string ResolveStartDirectory(string historyKey, string? fallbackDirectory, string defaultDirectory)
        {
            return GetExistingDirectory(_pickerLocations.GetDirectory(historyKey))
                ?? GetExistingDirectory(fallbackDirectory)
                ?? defaultDirectory;
        }

        // Given a path, this method checks if it exists as a file or directory. If it's a file, it returns the parent directory.
        // If it's a directory, it returns that directory. If neither exists, it returns null.
        private static string? GetExistingDirectory(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            string? candidate;

            try
            {
                candidate = Path.GetFullPath(path);
            }
            catch
            {
                return null;
            }

            if (File.Exists(candidate))
            {
                candidate = Path.GetDirectoryName(candidate);
            }

            while (!string.IsNullOrWhiteSpace(candidate))
            {
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                candidate = Path.GetDirectoryName(candidate);
            }

            return null;
        }

        private static IShellItem? TryCreateShellItem(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            try
            {
                SHCreateItemFromParsingName(path, IntPtr.Zero, typeof(IShellItem).GUID, out var shellItem);
                return shellItem;
            }
            catch
            {
                return null;
            }
        }

        private static IntPtr GetOwnerWindowHandle()
        {
            return App.Instance.MainWindow is null
                ? IntPtr.Zero
                : WindowNative.GetWindowHandle(App.Instance.MainWindow);
        }

        private static string GetDefaultDocumentsDirectory()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        private static string GetDefaultPicturesDirectory()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        }

        private static void ReleaseComObject(object? comObject)
        {
            if (comObject is not null && Marshal.IsComObject(comObject))
            {
                Marshal.FinalReleaseComObject(comObject);
            }
        }

        private readonly record struct FileDialogFilter(string Name, string Spec);


        // P/Invoke declaration for SHCreateItemFromParsingName, which creates an IShellItem from a file system path. This is used to set the initial folder of the file dialog.
        // Why is this a thing? I hate it
        // Why doesn't the default file dialog just take a path string for the initial folder?
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            IntPtr pbc,
            [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);

        // Struct used to define file type filters for the file dialog. It contains a display name and a filter specification (e.g. "*.txt;*.docx").
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct COMDLG_FILTERSPEC
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszName; // The display name of the filter (e.g. "Text Documents")

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszSpec; // The filter pattern (e.g. "*.txt;*.docx")
        }

        // Flags for configuring the behavior of the file dialog.
        // These are used when setting options on the IFileDialog interface to control things like whether the dialog is for picking folders,
        // whether it should force file system items, and whether it should require that paths and files exist.
        [Flags]
        private enum FOS : uint
        {
            NOCHANGEDIR = 0x00000008, // Don't change the current working directory when the dialog is shown
            PICKFOLDERS = 0x00000020, // Show folders in the dialog and allow selection of folders instead of files
            FORCEFILESYSTEM = 0x00000040, // Only show file system items (no virtual items from the shell namespace)
            PATHMUSTEXIST = 0x00000800, // Require that the user selects an existing path (folder or file)
            FILEMUSTEXIST = 0x00001000 // Require that the user selects an existing file (implies PATHMUSTEXIST)
        }

        private enum SIGDN : uint
        {
            FILESYSPATH = 0x80058000 // Retrieve the file system path of the item. This is used to get the actual file path from the IShellItem result.
        }

        [ComImport]
        [Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
        private class NativeFileOpenDialog
        {
        }

        [ComImport]
        [Guid("42F85136-DB7E-439C-85F1-E4075D135FC8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileDialog
        {
            [PreserveSig]
            int Show(IntPtr parent);

            void SetFileTypes(uint cFileTypes, [MarshalAs(UnmanagedType.LPArray)] COMDLG_FILTERSPEC[] rgFilterSpec);
            void SetFileTypeIndex(uint iFileType);
            void GetFileTypeIndex(out uint piFileType);
            void Advise(IntPtr pfde, out uint pdwCookie);
            void Unadvise(uint dwCookie);
            void SetOptions(FOS fos);
            void GetOptions(out FOS pfos);
            void SetDefaultFolder(IShellItem psi);
            void SetFolder(IShellItem psi);
            void GetFolder(out IShellItem ppsi);
            void GetCurrentSelection(out IShellItem ppsi);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            void GetResult(out IShellItem ppsi);
            void AddPlace(IShellItem psi, uint fdap);
            void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            void Close(int hr);
            void SetClientGuid([MarshalAs(UnmanagedType.LPStruct)] Guid guid);
            void ClearClientData();
            void SetFilter(IntPtr pFilter);
        }

        [ComImport]
        [Guid("D57C7288-D4AD-4768-BE02-9D969532D960")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog : IFileDialog
        {
            [PreserveSig]
            new int Show(IntPtr parent);

            new void SetFileTypes(uint cFileTypes, [MarshalAs(UnmanagedType.LPArray)] COMDLG_FILTERSPEC[] rgFilterSpec);
            new void SetFileTypeIndex(uint iFileType);
            new void GetFileTypeIndex(out uint piFileType);
            new void Advise(IntPtr pfde, out uint pdwCookie);
            new void Unadvise(uint dwCookie);
            new void SetOptions(FOS fos);
            new void GetOptions(out FOS pfos);
            new void SetDefaultFolder(IShellItem psi);
            new void SetFolder(IShellItem psi);
            new void GetFolder(out IShellItem ppsi);
            new void GetCurrentSelection(out IShellItem ppsi);
            new void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            new void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            new void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            new void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
            new void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            new void GetResult(out IShellItem ppsi);
            new void AddPlace(IShellItem psi, uint fdap);
            new void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            new void Close(int hr);
            new void SetClientGuid([MarshalAs(UnmanagedType.LPStruct)] Guid guid);
            new void ClearClientData();
            new void SetFilter(IntPtr pFilter);

            void GetResults(out IntPtr ppenum);
            void GetSelectedItems(out IntPtr ppsai);
        }

        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid bhid, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IntPtr ppv);
            void GetParent(out IShellItem ppsi);
            void GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
            void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
            void Compare(IShellItem psi, uint hint, out int piOrder);
        }
    }
}

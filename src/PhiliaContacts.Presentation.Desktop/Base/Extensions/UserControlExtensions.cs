using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using RunnethOverStudio.AppToolkit.Modules.ComponentModel;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static System.Environment;

namespace PhiliaContacts.Presentation.Desktop.Base.Extensions;

/// <summary>
/// Provides extension methods for <see cref="UserControl"/>.
/// </summary>
internal static class UserControlExtensions
{
    /// <summary>
    /// Opens a file picker dialog using the default starting location.
    /// </summary>
    /// <param name="view">The user control used to resolve the current top-level window.</param>
    /// <param name="title">The dialog title.</param>
    /// <param name="fileTypesLabel">The display label for the file type filter.</param>
    /// <param name="fileTypePatterns">The file pattern filters, such as <c>*.json</c>.</param>
    /// <returns>The selected file, or <see langword="null"/> when no file is selected or the picker cannot be opened.</returns>
    internal static async Task<IStorageFile?> GetUserSelectedFileAsync(this UserControl view, string title, string fileTypesLabel, params string[] fileTypePatterns)
    {
        return await view.GetUserSelectedFileAsync(title, null, fileTypesLabel, fileTypePatterns);
    }

    /// <summary>
    /// Opens a file picker dialog using an optional <see cref="SpecialFolder"/> as the starting location.
    /// </summary>
    /// <param name="view">The user control used to resolve the current top-level window.</param>
    /// <param name="title">The dialog title.</param>
    /// <param name="startingFolder">The optional special folder used as the initial location.</param>
    /// <param name="fileTypesLabel">The display label for the file type filter.</param>
    /// <param name="fileTypePatterns">The file pattern filters, such as <c>*.json</c>.</param>
    /// <returns>The selected file, or <see langword="null"/> when no file is selected or the picker cannot be opened.</returns>
    internal static async Task<IStorageFile?> GetUserSelectedFileAsync(this UserControl view, string title, SpecialFolder? startingFolder, string fileTypesLabel, params string[] fileTypePatterns)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(view);
        if (topLevel is null || !topLevel.StorageProvider.CanOpen)
        {
            return null;
        }

        string? startingFolderPath = null;
        if (startingFolder is not null)
        {
            try { startingFolderPath = Environment.GetFolderPath(startingFolder.Value); }
            catch { /* No need to throw an exception here, just use default starting folder path. */ }
        }

        Uri? startingLocation = string.IsNullOrWhiteSpace(startingFolderPath)
            ? null
            : Uri.TryCreate(startingFolderPath, UriKind.Absolute, out Uri? result) ? result : null;

        FilePickerOpenOptions filePickerOpenOptions = new()
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(fileTypesLabel) { Patterns = fileTypePatterns }],
            SuggestedStartLocation = startingLocation is null
                ? null
                : await topLevel.StorageProvider.TryGetFolderFromPathAsync(startingLocation)
        };

        IReadOnlyList<IStorageFile> files = await topLevel.StorageProvider.OpenFilePickerAsync(filePickerOpenOptions);

        return files.Count > 0 ? files[0] : null;
    }

    /// <summary>
    /// Opens a folder picker dialog using an optional <see cref="SpecialFolder"/> as the starting location.
    /// </summary>
    /// <param name="view">The user control used to resolve the current top-level window.</param>
    /// <param name="startingFolder">The optional special folder used as the initial location.</param>
    /// <returns>The selected folder, or <see langword="null"/> when no folder is selected or the picker cannot be opened.</returns>
    internal static async Task<IStorageFolder?> GetUserSelectedFolderAsync(this UserControl view, SpecialFolder? startingFolder = null)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(view);
        if (topLevel is null || !topLevel.StorageProvider.CanPickFolder)
        {
            return null;
        }

        string? startingFolderPath = null;
        if (startingFolder is not null)
        {
            try { startingFolderPath = Environment.GetFolderPath(startingFolder.Value); }
            catch { /* No need to throw an exception here, just use default starting folder path. */ }
        }

        FolderPickerOpenOptions folderPickerOpenOptions = new()
        {
            AllowMultiple = false,
            SuggestedStartLocation = startingFolderPath is null
                ? null
                : await topLevel.StorageProvider.TryGetFolderFromPathAsync(startingFolderPath)
        };

        IReadOnlyList<IStorageFolder> folders = await topLevel.StorageProvider.OpenFolderPickerAsync(folderPickerOpenOptions);

        return folders.Count > 0 ? folders[0] : null;
    }

    /// <summary>
    /// Opens a save file dialog with the provided default file information.
    /// </summary>
    /// <param name="view">The user control used to resolve the current top-level window.</param>
    /// <param name="title">The dialog title.</param>
    /// <param name="suggestedFileName">The default file name shown in the save dialog.</param>
    /// <param name="fileTypesLabel">The display label for the file type choice.</param>
    /// <param name="extension">The default file extension without wildcard.</param>
    /// <returns>The selected save file target, or <see langword="null"/> when the operation is canceled or unavailable.</returns>
    internal static async Task<IStorageFile?> SaveUserSelectedFileAsync(this UserControl view, string title, string suggestedFileName, string fileTypesLabel, string extension)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(view);
        if (topLevel is null || !topLevel.StorageProvider.CanSave)
        {
            return null;
        }

        return await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedFileName,
            DefaultExtension = extension,
            ShowOverwritePrompt = true,
            FileTypeChoices = [new FilePickerFileType(fileTypesLabel) { Patterns = ["*." + extension] }]
        });
    }

    /// <summary>
    /// Displays a modal confirmation dialog with continue and cancel options.
    /// </summary>
    /// <param name="view">The user control used to resolve the owner window.</param>
    /// <param name="title">The dialog title.</param>
    /// <param name="message">The confirmation message displayed to the user.</param>
    /// <returns><see langword="true"/> when the user confirms; otherwise, <see langword="false"/>.</returns>
    internal static async Task<bool> ConfirmAsync(this UserControl view, string title, string message)
    {
        if (TopLevel.GetTopLevel(view) is not Window owner)
        {
            return false;
        }

        Window dialog = new() { Title = title, Width = 420, Height = 175, WindowStartupLocation = WindowStartupLocation.CenterOwner };

        Button cancel = new() { Content = "Cancel" };

        Button accept = new() { Content = "Continue" };
        accept.Click += (_, _) => dialog.Close(true);
        cancel.Click += (_, _) => dialog.Close(false);

        dialog.Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(18),
            Spacing = 18,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { accept, cancel } }
            }
        };

        return await dialog.ShowDialog<bool>(owner);
    }

    internal static void LoadModelEvents(this UserControl view)
    {
        if (view.DataContext is BaseViewModel viewModel)
        {
            viewModel.AddModelEvents();
        }
    }

    internal static void UnloadModelEvents(this UserControl view)
    {
        if (view.DataContext is BaseViewModel viewModel)
        {
            viewModel.RemoveModelEvents();
        }
    }
}

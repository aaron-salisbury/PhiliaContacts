using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using PhiliaContacts.Presentation.Desktop.ViewModels;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PhiliaContacts.Presentation.Desktop.Views;

public partial class ContactsView : UserControl
{
    public ContactsView()
    {
        InitializeComponent();

        AttachedToVisualTree += async (_, _) =>
        {
            if (DataContext is ContactsViewModel model)
            {
                await model.InitializeAsync();
            }
        };
    }

    private async void ImportOlder_Click(object? sender, RoutedEventArgs e)
    {
        IStorageFile? file = await PickAsync("Choose an older Philia Contacts JSON file", "JSON", "*.json");

        if (file is not null && DataContext is ContactsViewModel model &&
            await ConfirmAsync("Import older data", "Import contacts from this file? Another source may contain overlapping contacts. The original file will be retained and backed up."))
        {
            await model.ImportOlderAsync(file.Path.LocalPath);
        }
    }

    private async void Delete_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ContactsViewModel model && model.SelectedContact is { } contact &&
            await ConfirmAsync("Delete contact", $"Permanently delete {contact.DisplayName}?"))
        {
            await model.DeleteCommand.ExecuteAsync(null);
        }
    }

    private async void ImportVCard_Click(object? sender, RoutedEventArgs e)
    {
        IStorageFile? file = await PickAsync("Choose a vCard file", "vCard", "*.vcf");
        if (file is not null && DataContext is ContactsViewModel model)
        {
            await using Stream stream = await file.OpenReadAsync();
            await model.PreviewVCardAsync(stream);
        }
    }

    private async void ChoosePhoto_Click(object? sender, RoutedEventArgs e)
    {
        IStorageFile? file = await PickAsync("Choose a JPEG or PNG photo", "Images", "*.png", "*.jpg", "*.jpeg");
        if (file is not null && DataContext is ContactsViewModel model)
        {
            await using Stream stream = await file.OpenReadAsync();
            await model.SetPhotoAsync(stream);
        }
    }

    private async void ExportJson_Click(object? sender, RoutedEventArgs e)
    {
        IStorageFile? file = await SaveAsync("Export contacts as JSON", "PhiliaContacts-export.json", "JSON", "json");
        if (file is not null && DataContext is ContactsViewModel model)
        {
            await using Stream stream = await file.OpenWriteAsync();
            stream.SetLength(0);
            await model.ExportJsonAsync(stream);
        }
    }

    private async void ExportVCard_Click(object? sender, RoutedEventArgs e)
    {
        IStorageFile? file = await SaveAsync("Export contacts as vCard", "PhiliaContacts.vcf", "vCard", "vcf");
        if (file is not null && DataContext is ContactsViewModel model)
        {
            await using Stream stream = await file.OpenWriteAsync();
            stream.SetLength(0);
            await model.ExportVCardAsync(stream);
        }
    }

    private async Task<IStorageFile?> PickAsync(string title, string label, params string[] patterns)
    {
        TopLevel? window = TopLevel.GetTopLevel(this);
        if (window is null)
        {
            return null;
        }

        IReadOnlyList<IStorageFile> files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(label) { Patterns = patterns }]
        });

        return files.Count > 0 ? files[0] : null;
    }

    private async Task<IStorageFile?> SaveAsync(string title, string suggested, string label, string extension)
    {
        TopLevel? window = TopLevel.GetTopLevel(this);
        if (window is null)
        {
            return null;
        }

        return await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggested,
            DefaultExtension = extension,
            ShowOverwritePrompt = true,
            FileTypeChoices = [new FilePickerFileType(label) { Patterns = ["*." + extension] }]
        });
    }

    private async Task<bool> ConfirmAsync(string title, string message)
    {
        if (TopLevel.GetTopLevel(this) is not Window owner)
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
}

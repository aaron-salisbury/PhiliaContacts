using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PhiliaContacts.Presentation.Desktop.Base.Extensions;
using PhiliaContacts.Presentation.Desktop.ViewModels;
using System;
using System.IO;

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
        IStorageFile? file = await this.GetUserSelectedFileAsync("Choose an older Philia Contacts JSON file", Environment.SpecialFolder.LocalApplicationData, "JSON", "*.json");

        if (file is not null && DataContext is ContactsViewModel model &&
            await this.ConfirmAsync("Import older data", "Import contacts from this file? Another source may contain overlapping contacts. The original file will be retained and backed up."))
        {
            await model.ImportOlderAsync(file.Path.LocalPath);
        }
    }

    private async void Delete_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ContactsViewModel model && model.SelectedContact is { } contact &&
            await this.ConfirmAsync("Delete contact", $"Permanently delete {contact.DisplayName}?"))
        {
            await model.DeleteCommand.ExecuteAsync(null);
        }
    }

    private async void ImportVCard_Click(object? sender, RoutedEventArgs e)
    {
        IStorageFile? file = await this.GetUserSelectedFileAsync("Choose a vCard file", Environment.SpecialFolder.MyDocuments, "vCard", "*.vcf");
        if (file is not null && DataContext is ContactsViewModel model)
        {
            await using Stream stream = await file.OpenReadAsync();
            await model.PreviewVCardAsync(stream);
        }
    }

    private async void ExportJson_Click(object? sender, RoutedEventArgs e)
    {
        IStorageFile? file = await this.SaveUserSelectedFileAsync("Export contacts as JSON", "PhiliaContacts-export.json", "JSON", "json");
        if (file is not null && DataContext is ContactsViewModel model)
        {
            await using Stream stream = await file.OpenWriteAsync();
            stream.SetLength(0);
            await model.ExportJsonAsync(stream);
        }
    }

    private async void ExportVCard_Click(object? sender, RoutedEventArgs e)
    {
        IStorageFile? file = await this.SaveUserSelectedFileAsync("Export contacts as vCard", "PhiliaContacts.vcf", "vCard", "vcf");
        if (file is not null && DataContext is ContactsViewModel model)
        {
            await using Stream stream = await file.OpenWriteAsync();
            stream.SetLength(0);
            await model.ExportVCardAsync(stream);
        }
    }
}

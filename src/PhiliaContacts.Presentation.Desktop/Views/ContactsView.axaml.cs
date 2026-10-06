using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PhiliaContacts.Presentation.Desktop.Base.Extensions;
using PhiliaContacts.Presentation.Desktop.ViewModels;
using System;

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
}

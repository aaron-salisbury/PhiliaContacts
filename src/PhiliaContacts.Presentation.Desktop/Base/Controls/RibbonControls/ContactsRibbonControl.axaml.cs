using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PhiliaContacts.Presentation.Desktop.Base.Extensions;
using PhiliaContacts.Presentation.Desktop.ViewModels;
using System.IO;

namespace PhiliaContacts.Presentation.Desktop.Base.Controls.RibbonControls;

public partial class ContactsRibbonControl : BaseRibbonControl
{
    public ContactsRibbonControl()
    {
        InitializeComponent();
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
        IStorageFile? file = await this.GetUserSelectedFileAsync("Choose a vCard file", "vCard", "*.vcf");
        if (file is not null && DataContext is ContactsViewModel model)
        {
            await using Stream stream = await file.OpenReadAsync();
            await model.ImportVCardAsync(stream);
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

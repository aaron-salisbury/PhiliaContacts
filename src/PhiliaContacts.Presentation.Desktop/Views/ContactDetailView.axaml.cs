using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PhiliaContacts.Presentation.Desktop.Base.Extensions;
using PhiliaContacts.Presentation.Desktop.ViewModels;
using System;
using System.IO;

namespace PhiliaContacts.Presentation.Desktop.Views;

public partial class ContactDetailView : UserControl
{
    public ContactDetailView()
    {
        InitializeComponent();
    }

    private async void ChoosePhoto_Click(object? sender, RoutedEventArgs e)
    {
        IStorageFile? file = await this.GetUserSelectedFileAsync("Choose a JPEG or PNG photo", "Images", "*.png", "*.jpg", "*.jpeg");
        if (file is not null && DataContext is ContactDetailViewModel model)
        {
            await using Stream stream = await file.OpenReadAsync();
            await model.SetPhotoAsync(stream);
        }
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhiliaContacts.Presentation.Desktop.Models;
using RunnethOverStudio.AppToolkit.Modules.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PhiliaContacts.Presentation.Desktop.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly SettingsViewModel _settingsViewModel;

    [ObservableProperty]
    private BaseViewModel _currentContent;

    [ObservableProperty]
    private bool _isPaneOpen;

    [ObservableProperty]
    private string _pageTitle;

    [ObservableProperty]
    private MenuPaneItemTemplate? _selectedPaneItem;

    public ObservableCollection<MenuPaneItemTemplate> PaneItems { get; }

    public MainViewModel(ContactsViewModel contactsViewModel, SettingsViewModel settingsViewModel)
    {
        ArgumentNullException.ThrowIfNull(contactsViewModel);

        _settingsViewModel = settingsViewModel ?? throw new ArgumentNullException(nameof(settingsViewModel));

        List<MenuPaneItemTemplate> paneItemTemplates =
        [
            // Icon key ref: https://pictogrammers.com/library/mdi/

            new MenuPaneItemTemplate(contactsViewModel, "CardAccountMail", "Contacts")
        ];

        IsPaneOpen = false;
        PaneItems = new ObservableCollection<MenuPaneItemTemplate>(paneItemTemplates);
        SelectedPaneItem = PaneItems[0];
        CurrentContent = SelectedPaneItem.Content;
        PageTitle = SelectedPaneItem.Label;
    }

    [RelayCommand]
    private void TriggerPane()
    {
        IsPaneOpen = !IsPaneOpen;
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task SettingsAsync()
    {
        await _settingsViewModel.InitializeAsync();
        SelectedPaneItem = null;
        CurrentContent = _settingsViewModel;
        PageTitle = "Settings";
    }

    partial void OnSelectedPaneItemChanged(MenuPaneItemTemplate? value)
    {
        if (value is null)
        {
            return;
        }

        CurrentContent = value.Content;
        PageTitle = value.Label;
    }
}

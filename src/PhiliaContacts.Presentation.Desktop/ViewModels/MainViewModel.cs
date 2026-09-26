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

    public MainViewModel(HomeViewModel homeViewModel, SettingsViewModel settingsViewModel)
    {
        ArgumentNullException.ThrowIfNull(homeViewModel);

        _settingsViewModel = settingsViewModel ?? throw new ArgumentNullException(nameof(settingsViewModel));

        List<MenuPaneItemTemplate> paneItemTemplates =
        [
            // Icon key ref: https://pictogrammers.com/library/mdi/

            new MenuPaneItemTemplate(homeViewModel, "CardAccountMail", "Contacts")
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
        await _settingsViewModel.RefreshAsync();
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

using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhiliaContacts.Presentation.Desktop.Base;
using RunnethOverStudio.AppToolkit.Modules.ComponentModel;

namespace PhiliaContacts.Presentation.Desktop.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    [ObservableProperty] private bool _themeIsSystem;
    [ObservableProperty] private bool _themeIsLight;
    [ObservableProperty] private bool _themeIsDark;

    public string AppDisplayName { get; }

    public string ApplicationInfo { get; }

    public string CopyHolder { get; }

    public string AppDescription { get; }

    public string PrivacyURL { get; }

    public string IssuesURL { get; }

    public SettingsViewModel()
    {
        AppDisplayName = AppInfo.AppDisplayName;
        ApplicationInfo = $"{AppInfo.AppDisplayName} - {AppInfo.Version}";
        CopyHolder = AppInfo.CopyHolder;
        AppDescription = AppInfo.AppDescription;
        PrivacyURL = AppInfo.PrivacyURL;
        IssuesURL = AppInfo.IssuesURL;

        //TODO: Currently the user's selection does not persist across app restarts.
        if (Application.Current?.RequestedThemeVariant == ThemeVariant.Light)
        {
            _themeIsLight = true;
        }
        else if (Application.Current?.RequestedThemeVariant == ThemeVariant.Dark)
        {
            _themeIsDark = true;
        }
        else
        {
            _themeIsSystem = true;
        }
    }

    [RelayCommand]
    private void SetTheme(string choice)
    {
        ThemeVariant variant;

        switch (choice.ToLower())
        {
            case "light":
                ThemeIsSystem = false;
                ThemeIsLight = true;
                ThemeIsDark = false;
                variant = ThemeVariant.Light;
                break;
            case "dark":
                ThemeIsSystem = false;
                ThemeIsLight = false;
                ThemeIsDark = true;
                variant = ThemeVariant.Dark;
                break;
            default:
                ThemeIsSystem = true;
                ThemeIsLight = false;
                ThemeIsDark = false;
                variant = ThemeVariant.Default;
                break;
        }

        Application.Current?.RequestedThemeVariant = variant;
    }
}

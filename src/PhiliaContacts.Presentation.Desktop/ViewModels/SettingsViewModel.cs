using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhiliaContacts.Presentation.Desktop.Base;
using RunnethOverStudio.AppToolkit.Modules.ComponentModel;

namespace PhiliaContacts.Presentation.Desktop.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    [ObservableProperty] private string _themeDescription = "Follow system";

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
    }

    [RelayCommand]
    private void SetTheme(string choice)
    {
        ThemeVariant variant = choice switch
        {
            "Light" => ThemeVariant.Light,
            "Dark" => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };

        Application.Current?.RequestedThemeVariant = variant;

        ThemeDescription = variant == ThemeVariant.Default ? "Follow system" : choice;
    }
}

using Microsoft.Toolkit.Mvvm.Input;
using PhiliaContacts.App.Base.Extensions;
using PhiliaContacts.App.Base.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;

namespace PhiliaContacts.App.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        public RelayCommand SelectFolderCommand { get; }

        private string _storageFolderPath = StorageLocationService.StorageFolderPath;
        public string StorageFolderPath
        {
            get => _storageFolderPath;
            set => SetProperty(ref _storageFolderPath, value);
        }

        private ElementTheme _elementTheme = ThemeSelectorService.Theme;
        public ElementTheme ElementTheme
        {
            get { return _elementTheme; }
            set { SetProperty(ref _elementTheme, value); }
        }

        private string _versionDescription;
        public string VersionDescription
        {
            get { return _versionDescription; }
            set { SetProperty(ref _versionDescription, value); }
        }

        private ICommand _switchThemeCommand;
        public ICommand SwitchThemeCommand
        {
            get
            {
                if (_switchThemeCommand == null)
                {
                    _switchThemeCommand = new RelayCommand<ElementTheme>(
                        async (param) =>
                        {
                            ElementTheme = param;
                            await ThemeSelectorService.SetThemeAsync(param);
                        });
                }

                return _switchThemeCommand;
            }
        }

        public SettingsViewModel()
        {
            SelectFolderCommand = new RelayCommand(async () => await SelectStorageFolder());
        }

        public async Task InitializeAsync()
        {
            VersionDescription = GetVersionDescription();
            await Task.CompletedTask;
        }

        private string GetVersionDescription()
        {
            var appName = "AppDisplayName".GetLocalized();
            var package = Package.Current;
            var packageId = package.Id;
            var version = packageId.Version;

            return $"{appName} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        private async Task SelectStorageFolder()
        {
            FolderPicker folderPicker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };

            folderPicker.FileTypeFilter.Add("*");

            Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                StorageFolderPath = folder.Path;

                await StorageLocationService.SaveStorageLocationInSettingsAsync(folder);
            }
        }
    }
}

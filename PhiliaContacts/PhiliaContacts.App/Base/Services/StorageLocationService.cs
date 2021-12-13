using Microsoft.Extensions.DependencyInjection;
using PhiliaContacts.App.Base.Extensions;
using PhiliaContacts.App.ViewModels;
using System.Threading.Tasks;
using Windows.Storage;

namespace PhiliaContacts.App.Base.Services
{
    public static class StorageLocationService
    {
        private const string SETTINGS_KEY = "AppStorageLocation";

        public static string StorageFolderPath { get; set; } = ApplicationData.Current.LocalFolder.Path;
        public static string StorageFolderToken => StorageFolderPath.GetHashString();

        public static async Task InitializeAsync()
        {
            StorageFolderPath = await LoadStorageLocationFromSettingsAsync();
        }

        private static async Task<string> LoadStorageLocationFromSettingsAsync()
        {
            string folderPath = await ApplicationData.Current.LocalSettings.ReadAsync<string>(SETTINGS_KEY);

            if (!string.IsNullOrEmpty(folderPath))
            {
                return folderPath;
            }
            else
            {
                await SaveStorageLocationInSettingsAsync(ApplicationData.Current.LocalFolder);
                return ApplicationData.Current.LocalFolder.Path;
            }
        }

        public static async Task SaveStorageLocationInSettingsAsync(StorageFolder folder)
        {
            StorageFolderPath = folder.Path;

            await ApplicationData.Current.LocalSettings.SaveAsync(SETTINGS_KEY, StorageFolderPath);

            Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList
                .AddOrReplace(StorageFolderToken, folder);

            App.Current.Services.GetService<ShellViewModel>().Manager.StorageFolderToken = StorageFolderToken;
        }
    }
}

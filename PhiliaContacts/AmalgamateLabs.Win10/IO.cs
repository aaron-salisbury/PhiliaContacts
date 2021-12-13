using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace AmalgamateLabs.Win10
{
    public static class IO
    {
        public static async Task WriteLocalDataFileAsync(string fileName, string contents, string folderToken = null)
        {
            // Requires references to the following:
            // 1) "Windows.Foundation.FoundationContract.winmd" probably stored at
            //        C:\Program Files (x86)\Windows Kits\10\References\{some version}\Windows.Foundation.FoundationContract\4.0.0.0
            // 2) "Windows.Foundation.UniversalApiContract.winmd" probably stored at
            //        C:\Program Files (x86)\Windows Kits\10\References\{some version}\Windows.Foundation.UniversalApiContract\10.0.0.0
            // 3) "System.Runtime.WindowsRuntime.dll" probably stored at
            //        C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5
            // 4) "Windows.WinMD" probably stored at
            //        C:\Program Files (x86)\Windows Kits\10\UnionMetadata\Facade

            StorageFolder folder = string.IsNullOrEmpty(folderToken)
                ? ApplicationData.Current.LocalFolder
                : await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(folderToken);

            StorageFile file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

            await FileIO.WriteTextAsync(file, contents);
        }

        public static async Task<string> ReadLocalDataFileAsync(string fileName, string folderToken = null)
        {
            try
            {
                StorageFolder folder = string.IsNullOrEmpty(folderToken)
                    ? ApplicationData.Current.LocalFolder
                    : await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(folderToken);

                StorageFile file = await folder.GetFileAsync(fileName);
                return await FileIO.ReadTextAsync(file);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        public static async Task DeleteLocalDataFileAsync(string fileName, string folderToken = null)
        {
            StorageFolder folder = string.IsNullOrEmpty(folderToken)
                ? ApplicationData.Current.LocalFolder
                : await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(folderToken);

            StorageFile fileToDelete = await folder.GetFileAsync(fileName);

            await fileToDelete.DeleteAsync();
        }
    }
}

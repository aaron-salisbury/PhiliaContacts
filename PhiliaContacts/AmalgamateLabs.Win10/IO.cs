using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace AmalgamateLabs.Win10
{
    public static class IO
    {
        public static async Task WriteLocalDataFileAsync(string fileName, string contents)
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

            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

            await FileIO.WriteTextAsync(file, contents);
        }

        public static async Task<string> ReadLocalDataFileAsync(string fileName)
        {
            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);
                return await FileIO.ReadTextAsync(file);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }
    }
}

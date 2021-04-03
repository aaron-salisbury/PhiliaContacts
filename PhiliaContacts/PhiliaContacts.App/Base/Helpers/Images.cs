using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace PhiliaContacts.App.Base.Helpers
{
    public static class Images
    {
        public static ImageSource UrlToImageSource(Uri imageUri)
        {
            return new BitmapImage(imageUri);
        }

        public static BitmapSource BytesToBitmapSource(byte[] rawImage)
        {
            using (InMemoryRandomAccessStream ms = new InMemoryRandomAccessStream())
            {
                using (DataWriter writer = new DataWriter(ms.GetOutputStreamAt(0)))
                {
                    writer.WriteBytes(rawImage);

                    writer.StoreAsync().GetResults();
                }

                BitmapImage image = new BitmapImage();
                image.SetSource(ms);

                return image;
            }
        }

        public static async Task<byte[]> StorageFileToBytesAsync(StorageFile file)
        {
            // http://windowsapptutorials.com/tips/convert-storage-file-to-byte-array-in-universal-windows-apps/

            byte[] fileBytes = null;

            if (file == null)
            {
                return null;
            }

            using (IRandomAccessStreamWithContentType stream = await file.OpenReadAsync())
            {
                fileBytes = new byte[stream.Size];

                using (var reader = new DataReader(stream))
                {
                    await reader.LoadAsync((uint)stream.Size);
                    reader.ReadBytes(fileBytes);
                }
            }

            return fileBytes;
        }
    }
}

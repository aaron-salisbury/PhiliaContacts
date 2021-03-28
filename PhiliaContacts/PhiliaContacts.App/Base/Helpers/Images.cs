using System;
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
    }
}

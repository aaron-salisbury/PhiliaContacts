using System.IO;
using System.Net;
using System.Reflection;

namespace PhiliaContacts.Core.Base.Helpers
{
    public static class Images
    {
        public static byte[] ImageUrlToBytes(string url)
        {
            using (WebClient webClient = new WebClient())
            {
                return webClient.DownloadData(url);
            }
        }

        public static byte[] EmbeddedImageToBytes(string path)
        {
            using (Stream resFilestream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path))
            {
                if (resFilestream != null)
                {
                    byte[] data = new byte[resFilestream.Length];
                    resFilestream.Read(data, 0, data.Length);

                    return data;
                }
            }

            return null;
        }
    }
}

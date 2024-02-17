using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;

namespace PhiliaContacts.Data
{
    public static class Access
    {
        private const string EMBEDDED_COUNTRIES_ABSOLUTE_FILEPATH = "PhiliaContacts.Data.Resources.Countries.xml";
        private const string EMBEDDED_CONTACT_IMAGE_PATH_PREFIX = "PhiliaContacts.Data.Resources.ContactImages.";
        private const string DEFAULT_CONTACT_IMAGE_FILENAME = "default-contact-image.png";

        public static IEnumerable<string> ReadCountryNames()
        {
            IEnumerable<string> countryNames = Enumerable.Empty<string>();

            try
            {
                string? xml = GetEmbeddedResourceText(EMBEDDED_COUNTRIES_ABSOLUTE_FILEPATH);

                if (!string.IsNullOrEmpty(xml))
                {
                    XDocument xDoc = new();
                    xDoc = XDocument.Parse(xml);

                    XElement? parentElement = xDoc.Element("Countries");

                    if (parentElement != null)
                    {
                        countryNames = parentElement.Descendants().Select(c => c.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to retrieve countries: {ex.Message}", "ERROR");
            }

            return countryNames;
        }

        public static byte[]? ReadContactImage(string imageName = DEFAULT_CONTACT_IMAGE_FILENAME)
        {
            byte[]? contactImage = null;

            try
            {
                contactImage = GetEmbeddedResourceImage($"{EMBEDDED_CONTACT_IMAGE_PATH_PREFIX}{imageName}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to retrieve contact image: {ex.Message}", "ERROR");
            }

            return contactImage;
        }

        private static string? GetEmbeddedResourceText(string absoluteFilepath)
        {
            string? result = null;

            using (Stream? manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(absoluteFilepath))
            {
                if (manifestResourceStream != null)
                {
                    using StreamReader streamReader = new(manifestResourceStream);
                    result = streamReader.ReadToEnd();
                }
            }

            return result;
        }

        private static byte[]? GetEmbeddedResourceImage(string absoluteFilepath)
        {
            byte[]? result = null;

            using (Stream? manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(absoluteFilepath))
            {
                if (manifestResourceStream != null)
                {
                    byte[] data = new byte[manifestResourceStream.Length];
                    manifestResourceStream.Read(data, 0, data.Length);
                    result = data;
                }
            }

            return result;
        }
    }
}

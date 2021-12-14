using PhiliaContacts.Core.Base.Helpers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PhiliaContacts.Core.Data
{
    public class CRUD
    {
        private const string DATA_FILE_NAME_V1 = "Contact.json"; // First public version of the app used this file name.
        private const string DATA_FILE_NAME = "PhiliaContacts.json";

        public static async Task<IEnumerable<T>> ReadDomainsAsync<T>(string folderToken = null)
        {
            string json = await AmalgamateLabs.Win10.IO.ReadLocalDataFileAsync(DATA_FILE_NAME, folderToken);

            if (!string.IsNullOrEmpty(json))
            {
                return await Json.ToObjectAsync<IEnumerable<T>>(json);
            }

            return null;
        }

        // Replace old version of data file with new version, if necessary.
        // Should only be called when user changes storage folder location. Save will always use the new name, but the new location could have an old file in it.
        public static async Task<IEnumerable<T>> ReadReplaceDomainsAsync<T>(string folderToken = null)
        {
            string json = await AmalgamateLabs.Win10.IO.ReadLocalDataFileAsync(DATA_FILE_NAME_V1, folderToken);

            if (!string.IsNullOrEmpty(json))
            {
                await DeleteDataAsync<T>(DATA_FILE_NAME_V1, folderToken);
                var domains = await Json.ToObjectAsync<IEnumerable<T>>(json);
                await UpdateDomainsAsync<T>((IEnumerable<object>)domains, folderToken);
                return domains;
            }

            json = await AmalgamateLabs.Win10.IO.ReadLocalDataFileAsync(DATA_FILE_NAME, folderToken);

            if (!string.IsNullOrEmpty(json))
            {
                return await Json.ToObjectAsync<IEnumerable<T>>(json);
            }

            return null;
        }

        public static async Task UpdateDomainsAsync<T>(IEnumerable<object> domains, string folderToken = null)
        {
            string json = await Json.StringifyAsync(domains);

            await AmalgamateLabs.Win10.IO.WriteLocalDataFileAsync(DATA_FILE_NAME, json, folderToken);
        }

        public static async Task DeleteDataAsync<T>(string fileName = null, string folderToken = null)
        {
            await AmalgamateLabs.Win10.IO.DeleteLocalDataFileAsync(fileName ?? DATA_FILE_NAME, folderToken);
        }

        public static List<string> ReadCountries()
        {
            XDocument xDoc = new XDocument();
            xDoc = XDocument.Parse(GetEmbeddedResourceText("PhiliaContacts.Core.Data.Countries.xml"));

            return xDoc.Element("Countries")
                .Descendants()
                .Select(c => c.Value )
                .ToList();
        }

        private static string GetEmbeddedResourceText(string filename)
        {
            string result = string.Empty;

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(filename))
            using (StreamReader streamReader = new StreamReader(stream))
            {
                result = streamReader.ReadToEnd();
            }

            return result;
        }
    }
}

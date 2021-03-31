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
        public static async Task<IEnumerable<T>> ReadDomainsAsync<T>()
        {
            string json = await AmalgamateLabs.Win10.IO.ReadLocalDataFileAsync(GetJsonFileNameForGivenType<T>());

            if (!string.IsNullOrEmpty(json))
            {
                return await Json.ToObjectAsync<IEnumerable<T>>(json);
            }

            return null;
        }

        public static async Task UpdateDomainsAsync<T>(IEnumerable<object> domains)
        {
            string json = await Json.StringifyAsync(domains);

            await AmalgamateLabs.Win10.IO.WriteLocalDataFileAsync(GetJsonFileNameForGivenType<T>(), json);
        }

        private static string GetJsonFileNameForGivenType<T>()
        {
            return $"{typeof(T).Name}.json";
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

using PhiliaContacts.Core.Base.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;

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
    }
}

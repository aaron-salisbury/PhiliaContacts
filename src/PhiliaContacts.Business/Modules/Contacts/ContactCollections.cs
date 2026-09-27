using System.Collections.Generic;
using System.Linq;

namespace PhiliaContacts.Business.Modules.Contacts;

internal static class ContactCollections
{
    internal static IEnumerable<string> WhereNotBlank(this IEnumerable<string?> values)
    {
        return values.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value!);
    }
}

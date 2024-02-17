using PhiliaContacts.Business.Models;
using PhiliaContacts.Data.Attributes;

namespace PhiliaContacts.Business.DTOs
{
    [Serializable, BaseName("PhiliaContacts")]
    internal class AppData
    {
        public string? Version { get; set; }
        public IEnumerable<Contact> Contacts { get; set; } = null!;
    }
}

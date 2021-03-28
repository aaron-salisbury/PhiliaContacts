using PhiliaContacts.Domains;
using System.Collections.Generic;

namespace PhiliaContacts.Core.Services
{
    public interface IVCFParserService
    {
        IEnumerable<Contact> GetContactsFromVCFContents(string vcfContents);
    }
}

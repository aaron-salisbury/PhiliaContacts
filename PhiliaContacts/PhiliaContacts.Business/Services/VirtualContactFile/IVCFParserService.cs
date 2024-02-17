using PhiliaContacts.Business.Models;

namespace PhiliaContacts.Business.Services.VirtualContactFile
{
    public interface IVCFParserService
    {
        IEnumerable<Contact> GetContactsFromVCFContents(string vcfContents);
    }
}

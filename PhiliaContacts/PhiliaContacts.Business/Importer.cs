using PhiliaContacts.Business.Models;
using PhiliaContacts.Business.Services.VirtualContactFile;
using System.Diagnostics;

namespace PhiliaContacts.Business
{
    public static class Importer
    {
        public static IEnumerable<Contact>? Import(string vCardsFileContent)
        {
            IEnumerable<Contact>? contacts = null;

            try
            {
                Debug.WriteLine("Beginning to import contacts.", "INFO");

                IVCFParserService vcfService = new EWSoftwareVCFService();

                contacts = vcfService.GetContactsFromVCFContents(vCardsFileContent);
                
                Debug.WriteLine("Successfully imported contacts.", "INFO");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Failed to import contacts: {e.Message}", "ERROR");
            }

            return contacts;
        }
    }
}

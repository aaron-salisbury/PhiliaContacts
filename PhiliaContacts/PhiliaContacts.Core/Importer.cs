using PhiliaContacts.Core.Services;
using PhiliaContacts.Domains;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace PhiliaContacts.Core
{
    public class Importer
    {
        public static bool Import(Manager manager, string vCardContents)
        {
            try
            {
                manager.Logger.Information("Beginning to import contacts.");

                IVCFParserService vcfService = new EWSoftwareVCFService(manager.Logger);

                manager.Contacts = new ObservableCollection<Contact>(
                    vcfService.GetContactsFromVCFContents(vCardContents)
                        .OrderBy(c => c.DisplayName)
                        .ThenBy(c => c.GivenName));

                manager.Logger.Information("Successfully imported contacts.");
                return true;
            }
            catch (Exception e)
            {
                manager.Logger.Error($"Failed to import contacts: {e.Message}");
                return false;
            }
        }
    }
}

using System.Collections.Generic;

namespace PhiliaContacts.Business.Modules.Integrations;

public interface IProviderRegistry
{
    IReadOnlyCollection<IProvider> Providers { get; }

    IProvider? Find(ProviderKey key);
}

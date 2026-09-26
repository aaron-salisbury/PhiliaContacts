using System;
using System.Collections.Generic;
using System.Linq;

namespace PhiliaContacts.Business.Modules.Integrations;

internal sealed class ProviderRegistry : IProviderRegistry
{
    private readonly IReadOnlyDictionary<ProviderKey, IProvider> _providers;

    public ProviderRegistry(IEnumerable<IProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        IProvider[] providerArray = [.. providers];
        _providers = providerArray.ToDictionary(provider => provider.Key);
        Providers = providerArray;
    }

    public IReadOnlyCollection<IProvider> Providers { get; }

    public IProvider? Find(ProviderKey key)
    {
        _providers.TryGetValue(key, out IProvider? provider);
        return provider;
    }
}

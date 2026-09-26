using System;

namespace PhiliaContacts.Business.Modules.Integrations;

public readonly record struct ProviderKey
{
    public ProviderKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    public string Value { get; }
}

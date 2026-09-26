using System;

namespace PhiliaContacts.Business.Modules.Integrations;

public readonly record struct CredentialReference
{
    public CredentialReference(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    public string Value { get; }

    public static CredentialReference New()
    {
        return new CredentialReference(Guid.NewGuid().ToString("N"));
    }
}

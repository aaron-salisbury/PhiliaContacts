using System;

namespace PhiliaContacts.Business.Modules.Integrations;

/// <summary>
/// Opaque credential material resolved from protected storage.
/// </summary>
public sealed record CredentialMaterial
{
    public CredentialMaterial(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }
}

using System;

namespace PhiliaContacts.Business.Modules.Integrations;

/// <summary>
/// Stable identity used to describe an application capability a Provider can potentially satisfy.
/// </summary>
public readonly record struct CapabilityKey
{
    public CapabilityKey(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    public string Value { get; }
}

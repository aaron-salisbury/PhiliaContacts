using System;
using System.Text.Json;

namespace PhiliaContacts.Business.Modules.Integrations;

public sealed record ProviderConfiguration
{
    public ProviderConfiguration(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        using JsonDocument document = JsonDocument.Parse(json);
        Json = json;
    }

    public string Json { get; }

    public static ProviderConfiguration Empty { get; } = new("{}");
}

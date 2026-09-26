using PhiliaContacts.Business.Modules.Integrations;
using System;
using System.Text.Json;

namespace PhiliaContacts.Integrations.WebDav;

internal sealed record WebDavConfiguration(Uri Endpoint)
{
    internal static bool TryParse(ProviderConfiguration configuration, out WebDavConfiguration webDavConfiguration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        JsonSerializerOptions webSerializerOptions = new(JsonSerializerDefaults.Web);

        try
        {
            ConfigurationDto? dto = JsonSerializer.Deserialize<ConfigurationDto>(configuration.Json, webSerializerOptions);

            if (dto is null
                || !Uri.TryCreate(dto.Endpoint, UriKind.Absolute, out Uri? endpoint)
                || (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
            {
                webDavConfiguration = null!;
                return false;
            }

            webDavConfiguration = new WebDavConfiguration(endpoint);
            return true;
        }
        catch (JsonException)
        {
            webDavConfiguration = null!;
            return false;
        }
    }

    private sealed record ConfigurationDto(string Endpoint);
}

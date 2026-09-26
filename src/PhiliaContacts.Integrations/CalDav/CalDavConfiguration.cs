using PhiliaContacts.Business.Modules.Integrations;
using System;
using System.Text.Json;

namespace PhiliaContacts.Integrations.CalDav;

internal sealed record CalDavConfiguration(Uri Endpoint, GoogleOAuthConfiguration? GoogleOAuth)
{
    internal static bool TryParse(ProviderConfiguration configuration, out CalDavConfiguration calDavConfiguration)
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
                calDavConfiguration = null!;
                return false;
            }

            calDavConfiguration = new CalDavConfiguration(endpoint, dto.GoogleOAuth is null ? null : new GoogleOAuthConfiguration(dto.GoogleOAuth.ClientId));
            return true;
        }
        catch (JsonException)
        {
            calDavConfiguration = null!;
            return false;
        }
    }

    private sealed record ConfigurationDto(string Endpoint, GoogleOAuthDto? GoogleOAuth);

    private sealed record GoogleOAuthDto(string ClientId);
}

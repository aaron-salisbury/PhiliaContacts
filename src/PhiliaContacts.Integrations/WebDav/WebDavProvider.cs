using PhiliaContacts.Business.Modules.Integrations;
using RunnethOverStudio.AppToolkit.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PhiliaContacts.Integrations.WebDav;

/// <summary>
/// Provides hierarchical resource browsing through WebDAV as defined by RFC 4918.
/// </summary>
/// <remarks>
/// The implementation uses PROPFIND with explicit Depth values and DAV:resourcetype
/// rather than inferring collections from URL shape, following RFC 4918 sections 5.2,
/// 9.1, and 15.9. Credential material is resolved from protected storage only when a
/// request is made and is never persisted in ProviderConfiguration.
/// </remarks>
internal sealed class WebDavProvider : IResourceBrowserProvider
{
    private const string CAPABILITY_KEY = "resource-browse";
    private const string PROVIDER_KEY = "webdav";

    private static readonly HttpMethod PropFindMethod = new("PROPFIND");
    private static readonly XNamespace DavNamespace = "DAV:";

    private readonly ICredentialStore _credentialStore;
    private readonly IHttpClientFactory _httpClientFactory;

    public WebDavProvider(IHttpClientFactory httpClientFactory, ICredentialStore credentialStore)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _credentialStore = credentialStore ?? throw new ArgumentNullException(nameof(credentialStore));
    }

    public IReadOnlyCollection<CapabilityKey> Capabilities { get; } = [new(CAPABILITY_KEY)];

    public string DisplayName => "WebDAV";

    public ProviderKey Key => new(PROVIDER_KEY);

    public async Task<IntegrationHealthResult> CheckHealthAsync(Integration integration, CancellationToken cancellationToken = default)
    {
        if (!WebDavConfiguration.TryParse(integration.Configuration, out WebDavConfiguration configuration))
        {
            return new IntegrationHealthResult(IntegrationHealth.InvalidConfiguration, "A valid HTTP or HTTPS WebDAV endpoint is required.");
        }

        CredentialResolution credentials = await ResolveCredentialsAsync(integration, configuration.Endpoint, cancellationToken);

        if (credentials.Error is not null)
        {
            return new IntegrationHealthResult(credentials.Error.Value, credentials.Diagnostic);
        }

        try
        {
            using HttpRequestMessage request = CreatePropFindRequest(configuration.Endpoint, 0, credentials.Authorization);
            HttpClient httpClient = _httpClientFactory.CreateClient();
            using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            return response.StatusCode switch
            {
                HttpStatusCode.MultiStatus => IntegrationHealthResult.Healthy(),
                HttpStatusCode.Unauthorized => new IntegrationHealthResult(IntegrationHealth.AuthenticationFailed, "The WebDAV server rejected the configured credentials."),
                HttpStatusCode.Forbidden => new IntegrationHealthResult(IntegrationHealth.AuthenticationFailed, "The WebDAV server rejected the configured access."),
                _ => new IntegrationHealthResult(IntegrationHealth.Unavailable, $"The WebDAV server returned HTTP {(int)response.StatusCode}.")
            };
        }
        catch (HttpRequestException exception)
        {
            return new IntegrationHealthResult(IntegrationHealth.Unavailable, exception.Message);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new IntegrationHealthResult(IntegrationHealth.Unavailable, "The WebDAV request timed out.");
        }
    }

    public async Task<ProcessResult<IReadOnlyList<ResourceItem>, ResourceBrowseError>> BrowseAsync(Integration integration, ResourceReference? collection = null, CancellationToken cancellationToken = default)
    {
        if (!WebDavConfiguration.TryParse(integration.Configuration, out WebDavConfiguration configuration))
        {
            return ProcessResult<IReadOnlyList<ResourceItem>, ResourceBrowseError>.Failure(ResourceBrowseError.InvalidConfiguration);
        }

        if (collection is not null && collection.Kind != ResourceKind.Collection)
        {
            return ProcessResult<IReadOnlyList<ResourceItem>, ResourceBrowseError>.Failure(ResourceBrowseError.InvalidResource);
        }

        CredentialResolution credentials = await ResolveCredentialsAsync(integration, configuration.Endpoint, cancellationToken);

        if (credentials.Error is not null)
        {
            ResourceBrowseError error = credentials.Error == IntegrationHealth.InvalidConfiguration
                ? ResourceBrowseError.InvalidConfiguration
                : ResourceBrowseError.AuthenticationFailed;

            return ProcessResult<IReadOnlyList<ResourceItem>, ResourceBrowseError>.Failure(error);
        }

        Uri requestUri = collection is null
            ? configuration.Endpoint
            : ResolveExternalId(configuration.Endpoint, collection.ExternalId);

        try
        {
            using HttpRequestMessage request = CreatePropFindRequest(requestUri, 1, credentials.Authorization);
            HttpClient httpClient = _httpClientFactory.CreateClient();
            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

            ResourceBrowseError? responseError = MapResponseError(response.StatusCode);

            if (responseError is not null)
            {
                return ProcessResult<IReadOnlyList<ResourceItem>, ResourceBrowseError>.Failure(responseError.Value);
            }

            string xml = await response.Content.ReadAsStringAsync(cancellationToken);
            IReadOnlyList<ResourceItem> items = ParseItems(integration.Id, configuration.Endpoint, requestUri, xml);
            return ProcessResult<IReadOnlyList<ResourceItem>, ResourceBrowseError>.Success(items);
        }
        catch (HttpRequestException)
        {
            return ProcessResult<IReadOnlyList<ResourceItem>, ResourceBrowseError>.Failure(ResourceBrowseError.Unavailable);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ProcessResult<IReadOnlyList<ResourceItem>, ResourceBrowseError>.Failure(ResourceBrowseError.Unavailable);
        }
        catch (System.Xml.XmlException)
        {
            return ProcessResult<IReadOnlyList<ResourceItem>, ResourceBrowseError>.Failure(ResourceBrowseError.InvalidResponse);
        }
        catch (InvalidOperationException)
        {
            return ProcessResult<IReadOnlyList<ResourceItem>, ResourceBrowseError>.Failure(ResourceBrowseError.InvalidResponse);
        }
    }

    private async Task<CredentialResolution> ResolveCredentialsAsync(Integration integration, Uri endpoint, CancellationToken cancellationToken)
    {
        if (integration.CredentialReference is null)
        {
            return CredentialResolution.Anonymous;
        }

        if (endpoint.Scheme != Uri.UriSchemeHttps)
        {
            return new CredentialResolution(
                null,
                IntegrationHealth.InvalidConfiguration,
                "Authenticated WebDAV requires HTTPS.");
        }

        CredentialMaterial? material = await _credentialStore.GetAsync(integration.CredentialReference.Value, cancellationToken);

        if (material is null)
        {
            return new CredentialResolution(
                null,
                IntegrationHealth.AuthenticationFailed,
                "The configured credential could not be found in protected storage.");
        }

        try
        {
            WebDavCredential? credential = JsonSerializer.Deserialize<WebDavCredential>(
                material.Value,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            if (credential is null
                || string.IsNullOrWhiteSpace(credential.Username)
                || string.IsNullOrEmpty(credential.Password))
            {
                return new CredentialResolution(
                    null,
                    IntegrationHealth.AuthenticationFailed,
                    "The stored WebDAV credential is invalid.");
            }

            string token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{credential.Username}:{credential.Password}"));
            return new CredentialResolution(new AuthenticationHeaderValue("Basic", token), null, string.Empty);
        }
        catch (JsonException)
        {
            return new CredentialResolution(
                null,
                IntegrationHealth.AuthenticationFailed,
                "The stored WebDAV credential is invalid.");
        }
    }

    private static HttpRequestMessage CreatePropFindRequest(Uri uri, int depth, AuthenticationHeaderValue? authorization)
    {
        const string body = """
            <?xml version="1.0" encoding="utf-8" ?>
            <D:propfind xmlns:D="DAV:">
              <D:prop>
                <D:displayname />
                <D:resourcetype />
                <D:getcontenttype />
                <D:getcontentlength />
                <D:getlastmodified />
              </D:prop>
            </D:propfind>
            """;

        HttpRequestMessage request = new(PropFindMethod, uri)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/xml")
        };

        request.Headers.Add("Depth", depth.ToString(CultureInfo.InvariantCulture));
        request.Headers.Authorization = authorization;
        return request;
    }

    private static ResourceBrowseError? MapResponseError(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.MultiStatus => null,
            HttpStatusCode.Unauthorized => ResourceBrowseError.AuthenticationFailed,
            HttpStatusCode.Forbidden => ResourceBrowseError.PermissionDenied,
            HttpStatusCode.NotFound => ResourceBrowseError.InvalidResource,
            _ when (int)statusCode >= 500 => ResourceBrowseError.Unavailable,
            _ => ResourceBrowseError.InvalidResponse
        };
    }

    private static List<ResourceItem> ParseItems(IntegrationId integrationId, Uri endpoint, Uri requestedUri, string xml)
    {
        XDocument document = XDocument.Parse(xml);
        List<ResourceItem> items = [];

        foreach (XElement response in document.Descendants(DavNamespace + "response"))
        {
            string? href = response.Element(DavNamespace + "href")?.Value;

            if (string.IsNullOrWhiteSpace(href))
            {
                continue;
            }

            Uri resourceUri = new(endpoint, href);

            if (!HasSameOrigin(endpoint, resourceUri))
            {
                continue;
            }

            if (Uri.Compare(resourceUri, requestedUri, UriComponents.PathAndQuery, UriFormat.SafeUnescaped, StringComparison.Ordinal) == 0)
            {
                continue;
            }

            XElement? properties = response
                .Elements(DavNamespace + "propstat")
                .Where(IsSuccessfulPropStat)
                .Select(propStat => propStat.Element(DavNamespace + "prop"))
                .FirstOrDefault(prop => prop is not null);

            if (properties is null)
            {
                continue;
            }

            bool isCollection = properties
                .Element(DavNamespace + "resourcetype")?
                .Elements(DavNamespace + "collection")
                .Any() == true;

            string? name = properties.Element(DavNamespace + "displayname")?.Value;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = Uri.UnescapeDataString(resourceUri.Segments.LastOrDefault()?.TrimEnd('/') ?? resourceUri.AbsolutePath);
            }

            string mediaType = properties.Element(DavNamespace + "getcontenttype")?.Value ?? string.Empty;
            long? contentLength = ParseLong(properties.Element(DavNamespace + "getcontentlength")?.Value);
            DateTimeOffset? lastModified = ParseDate(properties.Element(DavNamespace + "getlastmodified")?.Value);
            ResourceKind kind = isCollection ? ResourceKind.Collection : ResourceKind.File;
            ResourceReference reference = new(integrationId, resourceUri.PathAndQuery, kind);

            items.Add(new ResourceItem(reference, name, isCollection, mediaType, contentLength, lastModified));
        }

        return items;
    }

    private static bool IsSuccessfulPropStat(XElement propStat)
    {
        string status = propStat.Element(DavNamespace + "status")?.Value ?? string.Empty;
        return status.Contains(" 200 ", StringComparison.Ordinal);
    }

    private static Uri ResolveExternalId(Uri endpoint, string externalId)
    {
        Uri resolved = new(endpoint, externalId);

        if (!HasSameOrigin(endpoint, resolved))
        {
            throw new InvalidOperationException("The resource does not belong to the configured WebDAV endpoint.");
        }

        return resolved;
    }

    private static bool HasSameOrigin(Uri left, Uri right)
    {
        return string.Equals(left.Scheme, right.Scheme, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.Host, right.Host, StringComparison.OrdinalIgnoreCase)
            && left.Port == right.Port;
    }

    private static long? ParseLong(string? value)
    {
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long result)
            ? result
            : null;
    }

    private static DateTimeOffset? ParseDate(string? value)
    {
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out DateTimeOffset result)
            ? result
            : null;
    }

    private sealed record CredentialResolution(
        AuthenticationHeaderValue? Authorization,
        IntegrationHealth? Error,
        string Diagnostic)
    {
        public static CredentialResolution Anonymous { get; } = new(null, null, string.Empty);
    }

    private sealed record WebDavCredential(string Username, string Password);
}

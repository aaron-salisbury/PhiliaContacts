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
using System.Xml;
using System.Xml.Linq;

namespace PhiliaContacts.Integrations.CalDav;

/// <summary>
/// Reads calendar events from a CalDAV calendar collection as defined by RFC 4791.
/// </summary>
/// <remarks>
/// Calendar queries use the RFC 4791 calendar-query REPORT with a time-range filter and
/// CALDAV:expand. Recurrence expansion is deliberately delegated to the server so PhiliaContacts can
/// consume individual event instances without implementing recurrence rules or VTIMEZONE.
/// </remarks>
internal sealed class CalDavProvider : IAuthorizationProvider, ICalendarReaderProvider
{
    private const string AUTHORIZATION_CAPABILITY_KEY = "authorize";
    private const string CALENDAR_CAPABILITY_KEY = "calendar-read";
    private const string PROVIDER_KEY = "caldav";
    private static readonly HttpMethod PropFindMethod = new("PROPFIND");
    private static readonly HttpMethod ReportMethod = new("REPORT");
    private static readonly XNamespace DavNamespace = "DAV:";
    private static readonly XNamespace CalDavNamespace = "urn:ietf:params:xml:ns:caldav";
    private readonly ICredentialStore _credentialStore;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GoogleOAuthAuthorization? _googleOAuthAuthorization;

    public CalDavProvider(IHttpClientFactory httpClientFactory, ICredentialStore credentialStore, GoogleOAuthAuthorization? googleOAuthAuthorization = null)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _googleOAuthAuthorization = googleOAuthAuthorization;
        _credentialStore = credentialStore ?? throw new ArgumentNullException(nameof(credentialStore));
    }

    public IReadOnlyCollection<CapabilityKey> Capabilities { get; } = [new(AUTHORIZATION_CAPABILITY_KEY), new(CALENDAR_CAPABILITY_KEY)];
    public string DisplayName => "CalDAV";
    public ProviderKey Key => new(PROVIDER_KEY);

    public Task<ProcessResult<CredentialReference, AuthorizationError>> AuthorizeAsync(Integration integration, CancellationToken cancellationToken = default)
    {
        if (!CalDavConfiguration.TryParse(integration.Configuration, out CalDavConfiguration configuration)
            || configuration.GoogleOAuth is null
            || _googleOAuthAuthorization is null)
        {
            return Task.FromResult(ProcessResult<CredentialReference, AuthorizationError>.Failure(AuthorizationError.InvalidConfiguration));
        }

        return _googleOAuthAuthorization.AuthorizeAsync(configuration.GoogleOAuth, cancellationToken);
    }

    public async Task<IntegrationHealthResult> CheckHealthAsync(Integration integration, CancellationToken cancellationToken = default)
    {
        if (!CalDavConfiguration.TryParse(integration.Configuration, out CalDavConfiguration configuration))
        {
            return new IntegrationHealthResult(IntegrationHealth.InvalidConfiguration, "A valid HTTP or HTTPS CalDAV calendar endpoint is required.");
        }

        CredentialResolution credentials = await ResolveCredentialsAsync(integration, configuration, cancellationToken);
        if (credentials.Error is not null)
        {
            return new IntegrationHealthResult(credentials.Error.Value, credentials.Diagnostic);
        }

        try
        {
            using HttpRequestMessage request = CreateHealthRequest(configuration.Endpoint, credentials.Authorization);
            HttpClient httpClient = _httpClientFactory.CreateClient();
            using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return response.StatusCode switch
            {
                HttpStatusCode.MultiStatus => IntegrationHealthResult.Healthy(),
                HttpStatusCode.Unauthorized => new IntegrationHealthResult(IntegrationHealth.AuthenticationFailed, "The CalDAV server rejected the configured credentials."),
                HttpStatusCode.Forbidden => new IntegrationHealthResult(IntegrationHealth.AuthenticationFailed, "The CalDAV server rejected the configured access."),
                _ => new IntegrationHealthResult(IntegrationHealth.Unavailable, $"The CalDAV server returned HTTP {(int)response.StatusCode}.")
            };
        }
        catch (HttpRequestException exception)
        {
            return new IntegrationHealthResult(IntegrationHealth.Unavailable, exception.Message);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new IntegrationHealthResult(IntegrationHealth.Unavailable, "The CalDAV request timed out.");
        }
    }

    public async Task<CalendarReadResult> GetEventsAsync(Integration integration, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        if (end <= start)
        {
            return CalendarReadResult.Failure(CalendarReadError.InvalidRange);
        }

        if (!CalDavConfiguration.TryParse(integration.Configuration, out CalDavConfiguration configuration))
        {
            return CalendarReadResult.Failure(CalendarReadError.InvalidConfiguration);
        }

        CredentialResolution credentials = await ResolveCredentialsAsync(integration, configuration, cancellationToken);
        if (credentials.Error is not null)
        {
            CalendarReadError error = credentials.Error == IntegrationHealth.InvalidConfiguration ? CalendarReadError.InvalidConfiguration : CalendarReadError.AuthenticationFailed;
            return CalendarReadResult.Failure(error);
        }

        try
        {
            using HttpRequestMessage request = CreateCalendarQueryRequest(configuration.Endpoint, start, end, credentials.Authorization);
            HttpClient httpClient = _httpClientFactory.CreateClient();
            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
            CalendarReadError? responseError = MapResponseError(response.StatusCode);
            if (responseError is not null)
            {
                string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                string diagnostic = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                if (!string.IsNullOrWhiteSpace(responseBody))
                {
                    diagnostic += $": {responseBody}";
                }
                return CalendarReadResult.Failure(responseError.Value, diagnostic);
            }

            string xml = await response.Content.ReadAsStringAsync(cancellationToken);
            IReadOnlyList<CalendarEventItem> events = ParseEvents(integration.Id, configuration.Endpoint, xml);
            return CalendarReadResult.Success(events);
        }
        catch (HttpRequestException)
        {
            return CalendarReadResult.Failure(CalendarReadError.Unavailable);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return CalendarReadResult.Failure(CalendarReadError.Unavailable);
        }
        catch (Exception exception) when (exception is XmlException or FormatException or InvalidOperationException)
        {
            return CalendarReadResult.Failure(CalendarReadError.InvalidResponse);
        }
    }

    private async Task<CredentialResolution> ResolveCredentialsAsync(Integration integration, CalDavConfiguration configuration, CancellationToken cancellationToken)
    {
        Uri endpoint = configuration.Endpoint;

        if (integration.CredentialReference is null)
        {
            return CredentialResolution.Anonymous;
        }

        if (endpoint.Scheme != Uri.UriSchemeHttps)
        {
            return new CredentialResolution(null, IntegrationHealth.InvalidConfiguration, "Authenticated CalDAV requires HTTPS.");
        }

        if (configuration.GoogleOAuth is not null)
        {
            if (_googleOAuthAuthorization is null)
            {
                return new CredentialResolution(null, IntegrationHealth.AuthenticationFailed, "Google OAuth authorization is unavailable.");
            }

            string? accessToken = await _googleOAuthAuthorization.GetAccessTokenAsync(integration.CredentialReference.Value, configuration.GoogleOAuth, cancellationToken);
            return accessToken is null
                ? new CredentialResolution(null, IntegrationHealth.AuthenticationFailed, "The Google OAuth credential could not be refreshed.")
                : new CredentialResolution(new AuthenticationHeaderValue("Bearer", accessToken), null, string.Empty);
        }

        CredentialMaterial? material = await _credentialStore.GetAsync(integration.CredentialReference.Value, cancellationToken);
        if (material is null)
        {
            return new CredentialResolution(null, IntegrationHealth.AuthenticationFailed, "The configured credential could not be found in protected storage.");
        }

        try
        {
            CalDavCredential? credential = JsonSerializer.Deserialize<CalDavCredential>(material.Value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            if (credential is null || string.IsNullOrWhiteSpace(credential.Username) || string.IsNullOrEmpty(credential.Password))
            {
                return new CredentialResolution(null, IntegrationHealth.AuthenticationFailed, "The stored CalDAV credential is invalid.");
            }

            string token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{credential.Username}:{credential.Password}"));
            return new CredentialResolution(new AuthenticationHeaderValue("Basic", token), null, string.Empty);
        }
        catch (JsonException)
        {
            return new CredentialResolution(null, IntegrationHealth.AuthenticationFailed, "The stored CalDAV credential is invalid.");
        }
    }

    private static HttpRequestMessage CreateHealthRequest(Uri endpoint, AuthenticationHeaderValue? authorization)
    {
        const string body = """
            <?xml version="1.0" encoding="utf-8" ?>
            <D:propfind xmlns:D="DAV:" xmlns:C="urn:ietf:params:xml:ns:caldav">
              <D:prop><D:resourcetype /><C:supported-calendar-component-set /></D:prop>
            </D:propfind>
            """;
        HttpRequestMessage request = new(PropFindMethod, endpoint) { Content = new StringContent(body, Encoding.UTF8, "application/xml") };
        request.Headers.Add("Depth", "0");
        request.Headers.Authorization = authorization;
        return request;
    }

    private static HttpRequestMessage CreateCalendarQueryRequest(Uri endpoint, DateTimeOffset start, DateTimeOffset end, AuthenticationHeaderValue? authorization)
    {
        string startValue = FormatUtc(start);
        string endValue = FormatUtc(end);
        string body = $"""
            <?xml version="1.0" encoding="utf-8" ?>
            <C:calendar-query xmlns:D="DAV:" xmlns:C="urn:ietf:params:xml:ns:caldav">
              <D:prop>
                <D:getetag />
                <C:calendar-data><C:expand start="{startValue}" end="{endValue}" /></C:calendar-data>
              </D:prop>
              <C:filter>
                <C:comp-filter name="VCALENDAR">
                  <C:comp-filter name="VEVENT"><C:time-range start="{startValue}" end="{endValue}" /></C:comp-filter>
                </C:comp-filter>
              </C:filter>
            </C:calendar-query>
            """;
        HttpRequestMessage request = new(ReportMethod, endpoint) { Content = new StringContent(body, Encoding.UTF8, "application/xml") };
        request.Headers.Add("Depth", "1");
        request.Headers.Authorization = authorization;
        return request;
    }

    private static CalendarReadError? MapResponseError(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.MultiStatus => null,
            HttpStatusCode.Unauthorized => CalendarReadError.AuthenticationFailed,
            HttpStatusCode.Forbidden => CalendarReadError.PermissionDenied,
            HttpStatusCode.NotFound => CalendarReadError.InvalidConfiguration,
            _ when (int)statusCode >= 500 => CalendarReadError.Unavailable,
            _ => CalendarReadError.InvalidResponse
        };
    }

    private static List<CalendarEventItem> ParseEvents(IntegrationId integrationId, Uri endpoint, string xml)
    {
        XDocument document = XDocument.Parse(xml);
        List<CalendarEventItem> events = [];
        foreach (XElement response in document.Descendants(DavNamespace + "response"))
        {
            string? href = response.Element(DavNamespace + "href")?.Value;
            if (string.IsNullOrWhiteSpace(href))
            {
                continue;
            }

            Uri resourceUri = new(endpoint, href);
            XElement? properties = response.Elements(DavNamespace + "propstat").Where(IsSuccessfulPropStat).Select(x => x.Element(DavNamespace + "prop")).FirstOrDefault(x => x is not null);
            string? calendarData = properties?.Element(CalDavNamespace + "calendar-data")?.Value;
            if (string.IsNullOrWhiteSpace(calendarData))
            {
                continue;
            }

            foreach (IReadOnlyDictionary<string, string> component in ParseEventComponents(calendarData))
            {
                if (!component.TryGetValue("DTSTART", out string? startValue) || !TryParseCalendarDate(startValue, out DateTimeOffset eventStart, out bool isAllDay))
                {
                    continue;
                }

                DateTimeOffset eventEnd = eventStart;
                if (component.TryGetValue("DTEND", out string? endValue) && TryParseCalendarDate(endValue, out DateTimeOffset parsedEnd, out _))
                {
                    eventEnd = parsedEnd;
                }
                else if (component.TryGetValue("DURATION", out string? durationValue))
                {
                    eventEnd = eventStart + XmlConvert.ToTimeSpan(durationValue);
                }
                else if (isAllDay)
                {
                    eventEnd = eventStart.AddDays(1);
                }

                string uid = component.GetValueOrDefault("UID", resourceUri.PathAndQuery);
                string summary = UnescapeText(component.GetValueOrDefault("SUMMARY", "(Untitled event)"));
                string location = UnescapeText(component.GetValueOrDefault("LOCATION", string.Empty));
                ResourceReference reference = new(integrationId, resourceUri.PathAndQuery, ResourceKind.CalendarEvent);
                events.Add(new CalendarEventItem(reference, uid, summary, location, eventStart, eventEnd, isAllDay));
            }
        }

        return events.OrderBy(x => x.Start).ThenBy(x => x.Summary, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<IReadOnlyDictionary<string, string>> ParseEventComponents(string calendarData)
    {
        string normalized = calendarData.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        string[] physicalLines = normalized.Split('\n');
        List<string> lines = [];
        foreach (string line in physicalLines)
        {
            if ((line.StartsWith(' ') || line.StartsWith('\t')) && lines.Count > 0)
            {
                lines[^1] += line[1..];
            }
            else
            {
                lines.Add(line);
            }
        }

        List<IReadOnlyDictionary<string, string>> components = [];
        Dictionary<string, string>? current = null;
        foreach (string line in lines)
        {
            if (string.Equals(line, "BEGIN:VEVENT", StringComparison.OrdinalIgnoreCase))
            {
                current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                continue;
            }

            if (string.Equals(line, "END:VEVENT", StringComparison.OrdinalIgnoreCase))
            {
                if (current is not null)
                {
                    components.Add(current);
                    current = null;
                }
                continue;
            }

            if (current is null)
            {
                continue;
            }

            int separator = line.IndexOf(':');
            if (separator <= 0)
            {
                continue;
            }

            string property = line[..separator].Split(';')[0];
            current[property] = line[(separator + 1)..];
        }

        return components;
    }

    private static bool TryParseCalendarDate(string value, out DateTimeOffset result, out bool isAllDay)
    {
        isAllDay = value.Length == 8;
        if (isAllDay && DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
        {
            result = new DateTimeOffset(date, TimeSpan.Zero);
            return true;
        }

        return DateTimeOffset.TryParseExact(value, "yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out result);
    }

    private static bool IsSuccessfulPropStat(XElement propStat)
    {
        string status = propStat.Element(DavNamespace + "status")?.Value ?? string.Empty;
        return status.Contains(" 200 ", StringComparison.Ordinal);
    }

    private static string FormatUtc(DateTimeOffset value) => value.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    private static string UnescapeText(string value)
    {
        return value.Replace("\\n", "\n", StringComparison.OrdinalIgnoreCase).Replace("\\,", ",", StringComparison.Ordinal).Replace("\\;", ";", StringComparison.Ordinal).Replace("\\\\", "\\", StringComparison.Ordinal);
    }

    private sealed record CredentialResolution(AuthenticationHeaderValue? Authorization, IntegrationHealth? Error, string Diagnostic)
    {
        public static CredentialResolution Anonymous { get; } = new(null, null, string.Empty);
    }

    private sealed record CalDavCredential(string Username, string Password);
}

using PhiliaContacts.Business.Modules.Integrations;
using RunnethOverStudio.AppToolkit.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Integrations.CalDav;

/// <summary>
/// Performs Google's OAuth 2.0 installed-application authorization flow using PKCE and a loopback redirect.
/// </summary>
internal sealed class GoogleOAuthAuthorization
{
    private const string AUTHORIZATION_ENDPOINT = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string CALENDAR_READ_ONLY_SCOPE = "https://www.googleapis.com/auth/calendar.readonly";
    private const string TOKEN_ENDPOINT = "https://oauth2.googleapis.com/token";
    private readonly ICredentialStore _credentialStore;
    private readonly IHttpClientFactory _httpClientFactory;

    public GoogleOAuthAuthorization(IHttpClientFactory httpClientFactory, ICredentialStore credentialStore)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _credentialStore = credentialStore ?? throw new ArgumentNullException(nameof(credentialStore));
    }

    public async Task<ProcessResult<CredentialReference, AuthorizationError>> AuthorizeAsync(GoogleOAuthConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(configuration.ClientId))
        {
            return ProcessResult<CredentialReference, AuthorizationError>.Failure(AuthorizationError.InvalidConfiguration);
        }

        string verifier = Base64Url(RandomNumberGenerator.GetBytes(64));
        string challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        string state = Base64Url(RandomNumberGenerator.GetBytes(32));
        int port = GetAvailablePort();
        string redirectUri = $"http://127.0.0.1:{port}/";

        using HttpListener listener = new();
        listener.Prefixes.Add(redirectUri);
        listener.Start();

        string authorizationUrl = AUTHORIZATION_ENDPOINT + "?" + BuildQuery(new Dictionary<string, string>
        {
            ["access_type"] = "offline",
            ["client_id"] = configuration.ClientId,
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
            ["prompt"] = "consent",
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = CALENDAR_READ_ONLY_SCOPE,
            ["state"] = state
        });

        Process.Start(new ProcessStartInfo(authorizationUrl) { UseShellExecute = true });

        HttpListenerContext context;
        try
        {
            context = await listener.GetContextAsync().WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return ProcessResult<CredentialReference, AuthorizationError>.Failure(AuthorizationError.Cancelled);
        }

        string? code = context.Request.QueryString["code"];
        string? returnedState = context.Request.QueryString["state"];
        bool valid = !string.IsNullOrWhiteSpace(code) && string.Equals(state, returnedState, StringComparison.Ordinal);
        await CompleteBrowserResponseAsync(context.Response, valid, cancellationToken);

        if (!valid)
        {
            return ProcessResult<CredentialReference, AuthorizationError>.Failure(AuthorizationError.Failed);
        }

        OAuthTokenResponse? token = await RequestTokenAsync(new Dictionary<string, string>
        {
            ["client_id"] = configuration.ClientId,
            ["code"] = code!,
            ["code_verifier"] = verifier,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = redirectUri
        }, cancellationToken);

        if (token is null || string.IsNullOrWhiteSpace(token.RefreshToken) || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            return ProcessResult<CredentialReference, AuthorizationError>.Failure(AuthorizationError.Failed);
        }

        CredentialReference reference = CredentialReference.New();
        GoogleOAuthCredential credential = new(token.RefreshToken, token.AccessToken, DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn));
        await _credentialStore.StoreAsync(reference, new CredentialMaterial(JsonSerializer.Serialize(credential)), cancellationToken);
        return ProcessResult<CredentialReference, AuthorizationError>.Success(reference);
    }

    public async Task<string?> GetAccessTokenAsync(CredentialReference reference, GoogleOAuthConfiguration configuration, CancellationToken cancellationToken = default)
    {
        CredentialMaterial? material = await _credentialStore.GetAsync(reference, cancellationToken);
        if (material is null)
        {
            return null;
        }

        GoogleOAuthCredential? credential;
        try
        {
            credential = JsonSerializer.Deserialize<GoogleOAuthCredential>(material.Value);
        }
        catch (JsonException)
        {
            return null;
        }

        if (credential is null || string.IsNullOrWhiteSpace(credential.RefreshToken))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(credential.AccessToken) && credential.ExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return credential.AccessToken;
        }

        OAuthTokenResponse? token = await RequestTokenAsync(new Dictionary<string, string>
        {
            ["client_id"] = configuration.ClientId,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = credential.RefreshToken
        }, cancellationToken);

        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
        {
            return null;
        }

        GoogleOAuthCredential updated = credential with { AccessToken = token.AccessToken, ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn) };
        await _credentialStore.StoreAsync(reference, new CredentialMaterial(JsonSerializer.Serialize(updated)), cancellationToken);
        return updated.AccessToken;
    }

    private async Task<OAuthTokenResponse?> RequestTokenAsync(IReadOnlyDictionary<string, string> values, CancellationToken cancellationToken)
    {
        HttpClient httpClient = _httpClientFactory.CreateClient();
        using FormUrlEncodedContent content = new(values);
        using HttpResponseMessage response = await httpClient.PostAsync(TOKEN_ENDPOINT, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<OAuthTokenResponse>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private static async Task CompleteBrowserResponseAsync(HttpListenerResponse response, bool success, CancellationToken cancellationToken)
    {
        string message = success ? "PhiliaContacts authorization received. You can return to PhiliaContacts." : "PhiliaContacts could not complete authorization. You can return to PhiliaContacts.";
        byte[] bytes = Encoding.UTF8.GetBytes(message);
        response.ContentType = "text/plain; charset=utf-8";
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes, cancellationToken);
        response.Close();
    }

    private static int GetAvailablePort()
    {
        TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static string BuildQuery(IReadOnlyDictionary<string, string> values)
    {
        List<string> parts = [];
        foreach ((string key, string value) in values)
        {
            parts.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}");
        }

        return string.Join("&", parts);
    }

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed record OAuthTokenResponse(string AccessToken, string? RefreshToken, int ExpiresIn);
}

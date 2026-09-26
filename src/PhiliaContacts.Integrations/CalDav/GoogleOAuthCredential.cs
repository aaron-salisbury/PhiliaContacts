using System;

namespace PhiliaContacts.Integrations.CalDav;

internal sealed record GoogleOAuthCredential(string RefreshToken, string AccessToken, DateTimeOffset ExpiresAt);

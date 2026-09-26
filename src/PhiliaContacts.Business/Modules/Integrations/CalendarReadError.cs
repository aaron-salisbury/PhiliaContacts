namespace PhiliaContacts.Business.Modules.Integrations;

public enum CalendarReadError
{
    None = 0,
    IntegrationNotFound,
    IntegrationDisabled,
    ProviderNotAvailable,
    CapabilityNotSupported,
    InvalidRange,
    InvalidConfiguration,
    AuthenticationFailed,
    PermissionDenied,
    Unavailable,
    InvalidResponse
}

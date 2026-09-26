namespace PhiliaContacts.Business.Modules.Integrations;

public enum ResourceBrowseError
{
    None,
    IntegrationNotFound,
    IntegrationDisabled,
    ProviderNotAvailable,
    CapabilityNotSupported,
    InvalidResource,
    InvalidConfiguration,
    AuthenticationFailed,
    PermissionDenied,
    Unavailable,
    InvalidResponse
}

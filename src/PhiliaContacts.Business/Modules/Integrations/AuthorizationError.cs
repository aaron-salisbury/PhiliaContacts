namespace PhiliaContacts.Business.Modules.Integrations;

public enum AuthorizationError
{
    None = 0,
    IntegrationNotFound,
    IntegrationDisabled,
    ProviderNotAvailable,
    AuthorizationNotSupported,
    InvalidConfiguration,
    Cancelled,
    Failed
}

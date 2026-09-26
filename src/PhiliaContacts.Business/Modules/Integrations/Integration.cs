namespace PhiliaContacts.Business.Modules.Integrations;

public sealed record Integration(
    IntegrationId Id,
    ProviderKey ProviderKey,
    string Name,
    IntegrationState State,
    ProviderConfiguration Configuration,
    CredentialReference? CredentialReference);

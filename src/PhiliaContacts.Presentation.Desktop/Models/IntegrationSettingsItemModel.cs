using PhiliaContacts.Business.Modules.Integrations;

namespace PhiliaContacts.Presentation.Desktop.Models;

public sealed record IntegrationSettingsItemModel(
    Integration Integration,
    string ProviderDisplayName,
    IntegrationHealth Health,
    string Diagnostic);

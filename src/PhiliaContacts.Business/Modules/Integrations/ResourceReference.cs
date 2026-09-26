using System;

namespace PhiliaContacts.Business.Modules.Integrations;

public sealed record ResourceReference
{
    public ResourceReference(IntegrationId integrationId, string externalId, ResourceKind kind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalId);

        IntegrationId = integrationId;
        ExternalId = externalId.Trim();
        Kind = kind;
    }

    public IntegrationId IntegrationId { get; }

    public string ExternalId { get; }

    public ResourceKind Kind { get; }
}

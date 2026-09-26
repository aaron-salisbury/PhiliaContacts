using System;

namespace PhiliaContacts.Business.Modules.Integrations;

public sealed record ResourceItem(
    ResourceReference Reference,
    string Name,
    bool IsCollection,
    string MediaType,
    long? ContentLength,
    DateTimeOffset? LastModified);

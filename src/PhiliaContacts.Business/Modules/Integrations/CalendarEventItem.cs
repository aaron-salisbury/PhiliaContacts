using System;

namespace PhiliaContacts.Business.Modules.Integrations;

public sealed record CalendarEventItem(
    ResourceReference Reference,
    string UniqueId,
    string Summary,
    string Location,
    DateTimeOffset Start,
    DateTimeOffset End,
    bool IsAllDay);

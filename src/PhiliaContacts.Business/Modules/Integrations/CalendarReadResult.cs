using System;
using System.Collections.Generic;

namespace PhiliaContacts.Business.Modules.Integrations;

public sealed record CalendarReadResult(IReadOnlyList<CalendarEventItem> Events, CalendarReadError Error, string Diagnostic)
{
    public bool IsSuccessful => Error == CalendarReadError.None;

    public static CalendarReadResult Failure(CalendarReadError error, string diagnostic = "")
    {
        if (error == CalendarReadError.None)
        {
            throw new ArgumentOutOfRangeException(nameof(error));
        }

        return new CalendarReadResult([], error, diagnostic);
    }

    public static CalendarReadResult Success(IReadOnlyList<CalendarEventItem> events)
    {
        return new CalendarReadResult(events, CalendarReadError.None, string.Empty);
    }
}

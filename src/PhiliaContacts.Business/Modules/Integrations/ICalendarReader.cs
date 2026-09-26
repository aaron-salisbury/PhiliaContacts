using RunnethOverStudio.AppToolkit.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Integrations;

public interface ICalendarReader
{
    Task<CalendarReadResult> GetEventsAsync(IntegrationId integrationId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default);
}

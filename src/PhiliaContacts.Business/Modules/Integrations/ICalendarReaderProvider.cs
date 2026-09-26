using RunnethOverStudio.AppToolkit.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Integrations;

public interface ICalendarReaderProvider : IProvider
{
    Task<CalendarReadResult> GetEventsAsync(Integration integration, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default);
}

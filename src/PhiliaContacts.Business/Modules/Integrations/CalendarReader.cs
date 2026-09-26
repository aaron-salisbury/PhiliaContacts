using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Integrations;

internal sealed class CalendarReader : ICalendarReader
{
    private readonly IIntegrationStore _integrationStore;
    private readonly IProviderRegistry _providerRegistry;

    public CalendarReader(IIntegrationStore integrationStore, IProviderRegistry providerRegistry)
    {
        _integrationStore = integrationStore ?? throw new ArgumentNullException(nameof(integrationStore));
        _providerRegistry = providerRegistry ?? throw new ArgumentNullException(nameof(providerRegistry));
    }

    public async Task<CalendarReadResult> GetEventsAsync(IntegrationId integrationId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        if (end <= start)
        {
            return CalendarReadResult.Failure(CalendarReadError.InvalidRange);
        }

        Integration? integration = await _integrationStore.GetByIdAsync(integrationId, cancellationToken);
        if (integration is null)
        {
            return CalendarReadResult.Failure(CalendarReadError.IntegrationNotFound);
        }

        if (integration.State != IntegrationState.Enabled)
        {
            return CalendarReadResult.Failure(CalendarReadError.IntegrationDisabled);
        }

        IProvider? provider = _providerRegistry.Find(integration.ProviderKey);
        if (provider is null)
        {
            return CalendarReadResult.Failure(CalendarReadError.ProviderNotAvailable);
        }

        if (provider is not ICalendarReaderProvider calendarProvider)
        {
            return CalendarReadResult.Failure(CalendarReadError.CapabilityNotSupported);
        }

        return await calendarProvider.GetEventsAsync(integration, start, end, cancellationToken);
    }
}

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Data.Database;

public static class PhiliaContactsDataInitialization
{
    public static Task InitializePhiliaContactsDataAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return serviceProvider
            .GetRequiredService<PhiliaContactsDatabaseInitializer>()
            .InitializeAsync(cancellationToken);
    }
}

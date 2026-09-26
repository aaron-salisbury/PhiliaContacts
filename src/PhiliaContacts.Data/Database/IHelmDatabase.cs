using Microsoft.Data.Sqlite;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Data.Database;

internal interface IPhiliaContactsDatabase
{
    Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}

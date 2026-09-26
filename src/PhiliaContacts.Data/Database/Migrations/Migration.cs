using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Data.Database.Migrations;

internal abstract class Migration
{
    internal abstract long Version { get; }

    internal abstract string Description { get; }

    internal async Task ApplyAsync(SqliteConnection connection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        await using SqliteTransaction transaction = connection.BeginTransaction();

        try
        {
            await ExecuteAsync(connection, transaction, cancellationToken);

            const string recordMigrationSql = """
                INSERT INTO SchemaMigration (Version, Description, AppliedAtUtc)
                VALUES (@Version, @Description, CURRENT_TIMESTAMP);
                """;

            CommandDefinition command = new(
                recordMigrationSql,
                new { Version, Description },
                transaction,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(command);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    protected abstract Task ExecuteAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken cancellationToken);
}

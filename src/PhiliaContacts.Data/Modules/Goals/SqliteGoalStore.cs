using Dapper;
using PhiliaContacts.Business.Modules.Goals;
using PhiliaContacts.Business.Modules.LifeDomains;
using PhiliaContacts.Data.Database;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Data.Modules.Goals;

internal sealed class SqliteGoalStore : IGoalStore
{
    private readonly IPhiliaContactsDatabase _database;

    public SqliteGoalStore(IPhiliaContactsDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public async Task AddAsync(Goal goal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(goal);

        const string sql = """
            INSERT INTO Goal (Id, LifeDomainId, Name, State, IsArchived)
            VALUES (@Id, @LifeDomainId, @Name, @State, @IsArchived);
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(CreateCommand(sql, goal, cancellationToken));
    }

    public async Task<IReadOnlyList<Goal>> GetAsync(LifeDomainId? lifeDomainId = null, bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, LifeDomainId, Name, State, IsArchived
            FROM Goal
            WHERE (@LifeDomainId IS NULL OR LifeDomainId = @LifeDomainId)
              AND (@IncludeArchived = 1 OR IsArchived = 0)
            ORDER BY Name COLLATE NOCASE, Id;
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        CommandDefinition command = new(
            sql,
            new
            {
                LifeDomainId = lifeDomainId?.Value.ToByteArray(),
                IncludeArchived = includeArchived
            },
            cancellationToken: cancellationToken);

        IEnumerable<GoalRow> rows = await connection.QueryAsync<GoalRow>(command);
        return [.. rows.Select(Map)];
    }

    public async Task<Goal?> GetByIdAsync(GoalId id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, LifeDomainId, Name, State, IsArchived
            FROM Goal
            WHERE Id = @Id;
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        GoalRow? row = await connection.QuerySingleOrDefaultAsync<GoalRow>(
            new CommandDefinition(sql, new { Id = id.Value.ToByteArray() }, cancellationToken: cancellationToken));

        return row is null ? null : Map(row);
    }

    public async Task UpdateAsync(Goal goal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(goal);

        const string sql = """
            UPDATE Goal
            SET LifeDomainId = @LifeDomainId,
                Name = @Name,
                State = @State,
                IsArchived = @IsArchived
            WHERE Id = @Id;
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(CreateCommand(sql, goal, cancellationToken));
    }

    private static CommandDefinition CreateCommand(string sql, Goal goal, CancellationToken cancellationToken)
    {
        return new CommandDefinition(
            sql,
            new
            {
                Id = goal.Id.Value.ToByteArray(),
                LifeDomainId = goal.LifeDomainId.Value.ToByteArray(),
                goal.Name,
                State = (int)goal.State,
                goal.IsArchived
            },
            cancellationToken: cancellationToken);
    }

    private static Goal Map(GoalRow row)
    {
        return new Goal(
            new GoalId(new Guid(row.Id)),
            new LifeDomainId(new Guid(row.LifeDomainId)),
            row.Name,
            (GoalState)row.State,
            row.IsArchived);
    }

    private sealed class GoalRow
    {
        public byte[] Id { get; init; } = [];

        public bool IsArchived { get; init; }

        public byte[] LifeDomainId { get; init; } = [];

        public string Name { get; init; } = string.Empty;

        public int State { get; init; }
    }
}

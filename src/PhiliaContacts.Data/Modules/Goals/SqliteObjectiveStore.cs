using Dapper;
using PhiliaContacts.Business.Modules.Goals;
using PhiliaContacts.Data.Database;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Data.Modules.Goals;

internal sealed class SqliteObjectiveStore : IObjectiveStore
{
    private readonly IPhiliaContactsDatabase _database;

    public SqliteObjectiveStore(IPhiliaContactsDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public async Task AddAsync(Objective objective, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(objective);

        const string sql = """
            INSERT INTO Objective (Id, GoalId, Name, State, IsArchived)
            VALUES (@Id, @GoalId, @Name, @State, @IsArchived);
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(CreateCommand(sql, objective, cancellationToken));
    }

    public async Task<IReadOnlyList<Objective>> GetAsync(GoalId? goalId = null, bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, GoalId, Name, State, IsArchived
            FROM Objective
            WHERE (@GoalId IS NULL OR GoalId = @GoalId)
              AND (@IncludeArchived = 1 OR IsArchived = 0)
            ORDER BY Name COLLATE NOCASE, Id;
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        CommandDefinition command = new(
            sql,
            new
            {
                GoalId = goalId?.Value.ToByteArray(),
                IncludeArchived = includeArchived
            },
            cancellationToken: cancellationToken);

        IEnumerable<ObjectiveRow> rows = await connection.QueryAsync<ObjectiveRow>(command);
        return [.. rows.Select(Map)];
    }

    public async Task<Objective?> GetByIdAsync(ObjectiveId id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, GoalId, Name, State, IsArchived
            FROM Objective
            WHERE Id = @Id;
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        ObjectiveRow? row = await connection.QuerySingleOrDefaultAsync<ObjectiveRow>(
            new CommandDefinition(sql, new { Id = id.Value.ToByteArray() }, cancellationToken: cancellationToken));

        return row is null ? null : Map(row);
    }

    public async Task UpdateAsync(Objective objective, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(objective);

        const string sql = """
            UPDATE Objective
            SET GoalId = @GoalId,
                Name = @Name,
                State = @State,
                IsArchived = @IsArchived
            WHERE Id = @Id;
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(CreateCommand(sql, objective, cancellationToken));
    }

    private static CommandDefinition CreateCommand(string sql, Objective objective, CancellationToken cancellationToken)
    {
        return new CommandDefinition(
            sql,
            new
            {
                Id = objective.Id.Value.ToByteArray(),
                GoalId = objective.GoalId.Value.ToByteArray(),
                objective.Name,
                State = (int)objective.State,
                objective.IsArchived
            },
            cancellationToken: cancellationToken);
    }

    private static Objective Map(ObjectiveRow row)
    {
        return new Objective(
            new ObjectiveId(new Guid(row.Id)),
            new GoalId(new Guid(row.GoalId)),
            row.Name,
            (ObjectiveState)row.State,
            row.IsArchived);
    }

    private sealed class ObjectiveRow
    {
        public byte[] GoalId { get; init; } = [];

        public byte[] Id { get; init; } = [];

        public bool IsArchived { get; init; }

        public string Name { get; init; } = string.Empty;

        public int State { get; init; }
    }
}

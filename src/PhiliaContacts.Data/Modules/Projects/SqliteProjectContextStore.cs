using Dapper;
using PhiliaContacts.Business.Modules.Projects;
using PhiliaContacts.Data.Database;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Data.Modules.Projects;

internal sealed class SqliteProjectContextStore : IProjectContextStore
{
    private readonly IPhiliaContactsDatabase _database;

    public SqliteProjectContextStore(IPhiliaContactsDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public async Task AddAsync(ProjectContext projectContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projectContext);

        const string sql = """
            INSERT INTO ProjectContext (Id, Name, State, IsArchived)
            VALUES (@Id, @Name, @State, @IsArchived);
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(CreateCommand(sql, projectContext, cancellationToken));
    }

    public async Task<IReadOnlyList<ProjectContext>> GetAsync(bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, Name, State, IsArchived
            FROM ProjectContext
            WHERE (@IncludeArchived = 1 OR IsArchived = 0)
            ORDER BY Name COLLATE NOCASE, Id;
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        CommandDefinition command = new(sql, new { IncludeArchived = includeArchived }, cancellationToken: cancellationToken);
        IEnumerable<ProjectContextRow> rows = await connection.QueryAsync<ProjectContextRow>(command);
        return [.. rows.Select(Map)];
    }

    public async Task<ProjectContext?> GetByIdAsync(ProjectContextId id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, Name, State, IsArchived
            FROM ProjectContext
            WHERE Id = @Id;
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        ProjectContextRow? row = await connection.QuerySingleOrDefaultAsync<ProjectContextRow>(
            new CommandDefinition(sql, new { Id = id.Value.ToByteArray() }, cancellationToken: cancellationToken));

        return row is null ? null : Map(row);
    }

    public async Task UpdateAsync(ProjectContext projectContext, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projectContext);

        const string sql = """
            UPDATE ProjectContext
            SET Name = @Name,
                State = @State,
                IsArchived = @IsArchived
            WHERE Id = @Id;
            """;

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(CreateCommand(sql, projectContext, cancellationToken));
    }

    private static CommandDefinition CreateCommand(string sql, ProjectContext projectContext, CancellationToken cancellationToken)
    {
        return new CommandDefinition(
            sql,
            new
            {
                Id = projectContext.Id.Value.ToByteArray(),
                projectContext.Name,
                State = (int)projectContext.State,
                projectContext.IsArchived
            },
            cancellationToken: cancellationToken);
    }

    private static ProjectContext Map(ProjectContextRow row)
    {
        return new ProjectContext(
            new ProjectContextId(new Guid(row.Id)),
            row.Name,
            (ProjectContextState)row.State,
            row.IsArchived);
    }

    private sealed class ProjectContextRow
    {
        public byte[] Id { get; init; } = [];

        public bool IsArchived { get; init; }

        public string Name { get; init; } = string.Empty;

        public int State { get; init; }
    }
}

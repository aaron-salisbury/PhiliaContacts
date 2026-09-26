using Dapper;
using Microsoft.Data.Sqlite;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Data.Database.Migrations;

internal sealed class CreateGoalsMigration : Migration
{
    internal override long Version => 2;

    internal override string Description => "Create Goal and Objective tables";

    protected override async Task ExecuteAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE Goal (
                Id BLOB NOT NULL PRIMARY KEY CHECK(length(Id) = 16),
                LifeDomainId BLOB NOT NULL CHECK(length(LifeDomainId) = 16),
                Name TEXT NOT NULL CHECK(length(trim(Name)) > 0),
                State INTEGER NOT NULL CHECK(State IN (0, 1, 2)),
                IsArchived INTEGER NOT NULL DEFAULT 0 CHECK(IsArchived IN (0, 1)),
                FOREIGN KEY (LifeDomainId) REFERENCES LifeDomain(Id)
            );

            CREATE INDEX IX_Goal_LifeDomainId ON Goal(LifeDomainId);

            CREATE TABLE Objective (
                Id BLOB NOT NULL PRIMARY KEY CHECK(length(Id) = 16),
                GoalId BLOB NOT NULL CHECK(length(GoalId) = 16),
                Name TEXT NOT NULL CHECK(length(trim(Name)) > 0),
                State INTEGER NOT NULL CHECK(State IN (0, 1, 2)),
                IsArchived INTEGER NOT NULL DEFAULT 0 CHECK(IsArchived IN (0, 1)),
                FOREIGN KEY (GoalId) REFERENCES Goal(Id)
            );

            CREATE INDEX IX_Objective_GoalId ON Objective(GoalId);
            """;

        CommandDefinition command = new(sql, transaction: transaction, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }
}

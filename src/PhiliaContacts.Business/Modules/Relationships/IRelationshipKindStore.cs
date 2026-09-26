using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Relationships;

public interface IRelationshipKindStore
{
    Task AddAsync(RelationshipKind relationshipKind, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RelationshipKind>> GetAsync(CancellationToken cancellationToken = default);

    Task<RelationshipKind?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);

    Task UpdateAsync(RelationshipKind relationshipKind, CancellationToken cancellationToken = default);
}

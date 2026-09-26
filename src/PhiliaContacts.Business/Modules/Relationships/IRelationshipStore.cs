using PhiliaContacts.Business.Modules.Integrations;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Relationships;

public interface IRelationshipStore
{
    Task AddAsync(Relationship relationship, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(ResourceLocator source, string relationshipKindKey, ResourceLocator target, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Relationship>> GetAsync(ResourceLocator resource, CancellationToken cancellationToken = default);

    Task<Relationship?> GetByIdAsync(RelationshipId id, CancellationToken cancellationToken = default);

    Task<bool> HasExternalReferencesAsync(IntegrationId integrationId, CancellationToken cancellationToken = default);

    Task RemoveAsync(RelationshipId id, CancellationToken cancellationToken = default);
}

using RunnethOverStudio.AppToolkit.Core;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Relationships;

public interface IRelationshipService
{
    Task<ProcessResult<Relationship, RelationshipOperationError>> CreateAsync(CreateRelationshipRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Relationship>> GetAsync(ResourceLocator resource, CancellationToken cancellationToken = default);

    Task<ProcessResult<Relationship, RelationshipOperationError>> RemoveAsync(RelationshipId id, CancellationToken cancellationToken = default);
}

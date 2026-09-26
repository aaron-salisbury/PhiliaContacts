using FluentValidation;
using FluentValidation.Results;
using RunnethOverStudio.AppToolkit.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.Relationships;

internal sealed class RelationshipService : IRelationshipService
{
    private readonly IRelationshipStore _relationshipStore;
    private readonly IValidator<CreateRelationshipRequest> _validator;

    public RelationshipService(IRelationshipStore relationshipStore, IValidator<CreateRelationshipRequest> validator)
    {
        _relationshipStore = relationshipStore ?? throw new ArgumentNullException(nameof(relationshipStore));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<ProcessResult<Relationship, RelationshipOperationError>> CreateAsync(CreateRelationshipRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidationResult validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ProcessResult<Relationship, RelationshipOperationError>.Failure(RelationshipOperationError.InvalidRelationship);
        }

        Relationship relationship = new(RelationshipId.New(), request.Source, request.RelationshipKindKey, request.Target);
        await _relationshipStore.AddAsync(relationship, cancellationToken);

        return ProcessResult<Relationship, RelationshipOperationError>.Success(relationship);
    }

    public Task<IReadOnlyList<Relationship>> GetAsync(ResourceLocator resource, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resource);
        return _relationshipStore.GetAsync(resource, cancellationToken);
    }

    public async Task<ProcessResult<Relationship, RelationshipOperationError>> RemoveAsync(RelationshipId id, CancellationToken cancellationToken = default)
    {
        Relationship? relationship = await _relationshipStore.GetByIdAsync(id, cancellationToken);
        if (relationship is null)
        {
            return ProcessResult<Relationship, RelationshipOperationError>.Failure(RelationshipOperationError.RelationshipNotFound);
        }

        await _relationshipStore.RemoveAsync(id, cancellationToken);
        return ProcessResult<Relationship, RelationshipOperationError>.Success(relationship);
    }
}

using FluentValidation;
using System;

namespace PhiliaContacts.Business.Modules.Relationships;

internal sealed class CreateRelationshipRequestValidator : AbstractValidator<CreateRelationshipRequest>
{
    public CreateRelationshipRequestValidator(IResourceExistenceService resourceExistenceService, IRelationshipKindStore relationshipKindStore, IRelationshipStore relationshipStore)
    {
        ArgumentNullException.ThrowIfNull(resourceExistenceService);
        ArgumentNullException.ThrowIfNull(relationshipKindStore);
        ArgumentNullException.ThrowIfNull(relationshipStore);

        RuleFor(request => request.Source)
            .NotNull()
            .MustAsync(resourceExistenceService.ExistsAsync)
            .WithMessage("The source resource does not exist.");

        RuleFor(request => request.RelationshipKindKey)
            .NotEmpty()
            .MustAsync(relationshipKindStore.ExistsAsync)
            .WithMessage("The relationship kind does not exist.");

        RuleFor(request => request.Target)
            .NotNull()
            .MustAsync(resourceExistenceService.ExistsAsync)
            .WithMessage("The target resource does not exist.");

        RuleFor(request => request)
            .Must(request => request.Source != request.Target)
            .WithMessage("A resource cannot be related to itself.");

        RuleFor(request => request)
            .MustAsync(async (request, cancellationToken) =>
                !await relationshipStore.ExistsAsync(request.Source, request.RelationshipKindKey, request.Target, cancellationToken))
            .WithMessage("The relationship already exists.");
    }
}

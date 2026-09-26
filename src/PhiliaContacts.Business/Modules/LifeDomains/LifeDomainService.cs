using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhiliaContacts.Business.Modules.LifeDomains;

internal sealed class LifeDomainService : ILifeDomainService
{
    private readonly ILifeDomainStore _store;

    public LifeDomainService(ILifeDomainStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public async Task<bool> ArchiveAsync(LifeDomainId id, CancellationToken cancellationToken = default)
    {
        return await SetArchivedAsync(id, true, cancellationToken);
    }

    public async Task<LifeDomain> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        LifeDomain lifeDomain = new(
            LifeDomainId.New(),
            NormalizeName(name),
            false);

        await _store.AddAsync(lifeDomain, cancellationToken);
        return lifeDomain;
    }

    public Task<IReadOnlyList<LifeDomain>> GetAsync(bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        return _store.GetAsync(includeArchived, cancellationToken);
    }

    public async Task<bool> RenameAsync(LifeDomainId id, string name, CancellationToken cancellationToken = default)
    {
        LifeDomain? lifeDomain = await _store.GetByIdAsync(id, cancellationToken);

        if (lifeDomain is null)
        {
            return false;
        }

        LifeDomain renamedLifeDomain = lifeDomain with { Name = NormalizeName(name) };
        await _store.UpdateAsync(renamedLifeDomain, cancellationToken);

        return true;
    }

    public async Task<bool> RestoreAsync(LifeDomainId id, CancellationToken cancellationToken = default)
    {
        return await SetArchivedAsync(id, false, cancellationToken);
    }

    private async Task<bool> SetArchivedAsync(LifeDomainId id, bool isArchived, CancellationToken cancellationToken)
    {
        LifeDomain? lifeDomain = await _store.GetByIdAsync(id, cancellationToken);

        if (lifeDomain is null)
        {
            return false;
        }

        if (lifeDomain.IsArchived != isArchived)
        {
            LifeDomain updatedLifeDomain = lifeDomain with { IsArchived = isArchived };
            await _store.UpdateAsync(updatedLifeDomain, cancellationToken);
        }

        return true;
    }

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return name.Trim();
    }
}

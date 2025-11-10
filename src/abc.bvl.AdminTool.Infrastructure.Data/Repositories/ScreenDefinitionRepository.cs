using abc.bvl.AdminTool.Application.Common.Interfaces;
using abc.bvl.AdminTool.Application.Common.Models;
using abc.bvl.AdminTool.Contracts.Common;
using abc.bvl.AdminTool.Domain.Entities;
using abc.bvl.AdminTool.Infrastructure.Data.Context;
using abc.bvl.AdminTool.Infrastructure.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace abc.bvl.AdminTool.Infrastructure.Data.Repositories;

public class ScreenDefinitionRepository : IScreenDefinitionRepository
{
    private readonly ICurrentDbContextProvider _contextProvider;
    private AdminDbContext Context => _contextProvider.GetContext();

    public ScreenDefinitionRepository(ICurrentDbContextProvider contextProvider)
    {
        _contextProvider = contextProvider;
    }

    public async Task<IEnumerable<ScreenDefnDto>> GetAllAsync(byte? status = null, CancellationToken cancellationToken = default)
    {
        var query = Context.ScreenDefinitions.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(s => s.StatusId == status.Value);
        }

        var results = await query
            .Select(s => new ScreenDefnDto
            {
                ScreenGk = s.ScreenGk,
                ScreenName = s.ScreenName,
                StatusId = s.StatusId,
                CreatedDt = s.CreatedDt,
                CreatedBy = s.CreatedBy,
                UpdatedDt = s.UpdatedDt,
                UpdatedBy = s.UpdatedBy
            })
            .ToListAsync(cancellationToken);

        return results;
    }

    public async Task<ScreenDefnDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await Context.ScreenDefinitions
            .AsNoTracking()
            .Where(s => s.ScreenGk == id)
            .Select(s => new ScreenDefnDto
            {
                ScreenGk = s.ScreenGk,
                ScreenName = s.ScreenName,
                StatusId = s.StatusId,
                CreatedDt = s.CreatedDt,
                CreatedBy = s.CreatedBy,
                UpdatedDt = s.UpdatedDt,
                UpdatedBy = s.UpdatedBy
            })
            .FirstOrDefaultAsync(cancellationToken);

        return result;
    }

    public async Task<ScreenDefinition?> GetEntityByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await Context.ScreenDefinitions
            .FirstOrDefaultAsync(s => s.ScreenGk == id, cancellationToken);
    }

    public async Task<ScreenDefinition> CreateAsync(ScreenDefinition screenDefinition, CancellationToken cancellationToken = default)
    {
        Context.ScreenDefinitions.Add(screenDefinition);
        await Context.SaveChangesAsync(cancellationToken);
        return screenDefinition;
    }

    public async Task<ScreenDefinition> UpdateAsync(ScreenDefinition screenDefinition, CancellationToken cancellationToken = default)
    {
        Context.ScreenDefinitions.Update(screenDefinition);
        await Context.SaveChangesAsync(cancellationToken);
        return screenDefinition;
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await Context.ScreenDefinitions.FindAsync(id, cancellationToken);
        if (entity != null)
        {
            Context.ScreenDefinitions.Remove(entity);
            await Context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IEnumerable<ScreenDefnDto>> GetPagedAsync(
        byte? status, 
        string? searchTerm, 
        PaginationRequest pagination, 
        CancellationToken cancellationToken = default)
    {
        var query = Context.ScreenDefinitions.AsNoTracking();

        // Apply status filter
        if (status.HasValue)
        {
            query = query.Where(s => s.StatusId == status.Value);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.Trim().ToLower();
            query = query.Where(s => s.ScreenName.ToLower().Contains(search));
        }

        // Apply pagination with ordering for consistent results
        var results = await query
            .OrderBy(s => s.ScreenGk)
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .Select(s => new ScreenDefnDto
            {
                ScreenGk = s.ScreenGk,
                ScreenName = s.ScreenName,
                StatusId = s.StatusId,
                CreatedDt = s.CreatedDt,
                CreatedBy = s.CreatedBy,
                UpdatedDt = s.UpdatedDt,
                UpdatedBy = s.UpdatedBy
            })
            .ToListAsync(cancellationToken);

        return results;
    }

    public async Task<int> GetCountAsync(
        byte? status, 
        string? searchTerm, 
        CancellationToken cancellationToken = default)
    {
        var query = Context.ScreenDefinitions.AsNoTracking();

        // Apply status filter
        if (status.HasValue)
        {
            query = query.Where(s => s.StatusId == status.Value);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.Trim().ToLower();
            query = query.Where(s => s.ScreenName.ToLower().Contains(search));
        }

        return await query.CountAsync(cancellationToken);
    }
}

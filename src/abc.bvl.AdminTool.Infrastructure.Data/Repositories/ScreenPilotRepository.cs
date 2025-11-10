using abc.bvl.AdminTool.Application.Common.Interfaces;
using abc.bvl.AdminTool.Application.Common.Models;
using abc.bvl.AdminTool.Domain.Entities;
using abc.bvl.AdminTool.Infrastructure.Data.Context;
using abc.bvl.AdminTool.Infrastructure.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace abc.bvl.AdminTool.Infrastructure.Data.Repositories;

public class ScreenPilotRepository : IScreenPilotRepository
{
    private readonly ICurrentDbContextProvider _contextProvider;
    private AdminDbContext Context => _contextProvider.GetContext();

    public ScreenPilotRepository(ICurrentDbContextProvider contextProvider)
    {
        _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
    }

    public async Task<IEnumerable<ScreenPilotDto>> GetByUserIdAsync(long nbUserGk, CancellationToken cancellationToken = default)
    {
        return await Context.ScreenPilots
            .AsNoTracking()
            .Where(sp => sp.NbUserGk == nbUserGk && sp.StatusId == 1)
            .Select(sp => new ScreenPilotDto
            {
                ScreenPilotGk = sp.ScreenPilotGk,
                NbUserGk = sp.NbUserGk,
                ScreenGk = sp.ScreenGk,
                StatusId = sp.StatusId,
                DualMode = sp.DualMode,
                CreatedDt = sp.CreatedDt,
                CreatedBy = sp.CreatedBy,
                UpdatedDt = sp.UpdatedDt,
                UpdatedBy = sp.UpdatedBy
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ScreenPilotDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await Context.ScreenPilots
            .AsNoTracking()
            .Where(sp => sp.ScreenPilotGk == id)
            .Select(sp => new ScreenPilotDto
            {
                ScreenPilotGk = sp.ScreenPilotGk,
                NbUserGk = sp.NbUserGk,
                ScreenGk = sp.ScreenGk,
                StatusId = sp.StatusId,
                DualMode = sp.DualMode,
                CreatedDt = sp.CreatedDt,
                CreatedBy = sp.CreatedBy,
                UpdatedDt = sp.UpdatedDt,
                UpdatedBy = sp.UpdatedBy
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<ScreenPilotDto>> GetByScreenGkAsync(long screenGk, CancellationToken cancellationToken = default)
    {
        return await Context.ScreenPilots
            .AsNoTracking()
            .Where(sp => sp.ScreenGk == screenGk)
            .Select(sp => new ScreenPilotDto
            {
                ScreenPilotGk = sp.ScreenPilotGk,
                NbUserGk = sp.NbUserGk,
                ScreenGk = sp.ScreenGk,
                StatusId = sp.StatusId,
                DualMode = sp.DualMode,
                CreatedDt = sp.CreatedDt,
                CreatedBy = sp.CreatedBy,
                UpdatedDt = sp.UpdatedDt,
                UpdatedBy = sp.UpdatedBy
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ScreenPilotDto>> GetAllAsync(int? statusId = null, CancellationToken cancellationToken = default)
    {
        var query = Context.ScreenPilots.AsNoTracking();
        if (statusId.HasValue)
            query = query.Where(sp => sp.StatusId == statusId.Value);
        return await query.Select(sp => new ScreenPilotDto
        {
            ScreenPilotGk = sp.ScreenPilotGk,
            NbUserGk = sp.NbUserGk,
            ScreenGk = sp.ScreenGk,
            StatusId = sp.StatusId,
            DualMode = sp.DualMode,
            CreatedDt = sp.CreatedDt,
            CreatedBy = sp.CreatedBy,
            UpdatedDt = sp.UpdatedDt,
            UpdatedBy = sp.UpdatedBy
        }).ToListAsync(cancellationToken);
    }

    public IQueryable<ScreenPilotDto> GetAllQueryable(int? statusId = null)
    {
        var query = Context.ScreenPilots.AsNoTracking();
        if (statusId.HasValue)
            query = query.Where(sp => sp.StatusId == statusId.Value);
        return query.Select(sp => new ScreenPilotDto
        {
            ScreenPilotGk = sp.ScreenPilotGk,
            NbUserGk = sp.NbUserGk,
            ScreenGk = sp.ScreenGk,
            StatusId = sp.StatusId,
            DualMode = sp.DualMode,
            CreatedDt = sp.CreatedDt,
            CreatedBy = sp.CreatedBy,
            UpdatedDt = sp.UpdatedDt,
            UpdatedBy = sp.UpdatedBy
        });
    }

    public async Task<ScreenPilot?> GetEntityByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await Context.ScreenPilots.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<ScreenPilot> CreateAsync(ScreenPilot entity, CancellationToken cancellationToken = default)
    {
        Context.ScreenPilots.Add(entity);
        return entity;
    }

    public async Task<ScreenPilot> UpdateAsync(ScreenPilot entity, CancellationToken cancellationToken = default)
    {
        Context.ScreenPilots.Update(entity);
        await Task.CompletedTask;
        return entity;
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityByIdAsync(id, cancellationToken);
        if (entity != null)
            Context.ScreenPilots.Remove(entity);
    }
}

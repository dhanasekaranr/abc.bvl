using abc.bvl.AdminTool.Application.Common.Interfaces;
using abc.bvl.AdminTool.Application.Common.Models;
using abc.bvl.AdminTool.Domain.Entities;
using abc.bvl.AdminTool.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace abc.bvl.AdminTool.Infrastructure.Data.Repositories;

public class ScreenPilotRepository : IScreenPilotRepository
{
    private readonly AdminDbContext _context;

    public ScreenPilotRepository(AdminDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IEnumerable<ScreenPilotDto>> GetByUserIdAsync(long nbUserGk, CancellationToken cancellationToken = default)
    {
        return await _context.ScreenPilots
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
        return await _context.ScreenPilots
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
        return await _context.ScreenPilots
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
        var query = _context.ScreenPilots.AsNoTracking();
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
        var query = _context.ScreenPilots.AsNoTracking();
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
        return await _context.ScreenPilots.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<ScreenPilot> CreateAsync(ScreenPilot entity, CancellationToken cancellationToken = default)
    {
        _context.ScreenPilots.Add(entity);
        return entity;
    }

    public async Task<ScreenPilot> UpdateAsync(ScreenPilot entity, CancellationToken cancellationToken = default)
    {
        _context.ScreenPilots.Update(entity);
        await Task.CompletedTask;
        return entity;
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await GetEntityByIdAsync(id, cancellationToken);
        if (entity != null)
            _context.ScreenPilots.Remove(entity);
    }
}

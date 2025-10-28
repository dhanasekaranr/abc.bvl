using abc.bvl.AdminTool.Application.Common.Interfaces;
using abc.bvl.AdminTool.Contracts.ScreenPilot;
using abc.bvl.AdminTool.Domain.Entities;
using abc.bvl.AdminTool.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace abc.bvl.AdminTool.Infrastructure.Data.Repositories;

/// <summary>
/// Repository for ScreenPilot entity operations
/// </summary>
public class ScreenPilotRepository : IScreenPilotRepository
{
    private readonly AdminDbContext _context;

    public ScreenPilotRepository(AdminDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IEnumerable<ScreenPilotDto>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var results = await _context.ScreenPilots
            .AsNoTracking()
            .Include(sp => sp.ScreenDefinition)
            .Where(sp => sp.UserId == userId && sp.Status == 1)
            .Select(sp => new ScreenPilotDto(
                sp.Id,
                sp.ScreenDefnId,
                sp.UserId,
                sp.Status,
                new DateTimeOffset(sp.UpdatedAt, TimeSpan.Zero),
                sp.UpdatedBy,
                null, // RowVersion - not used in EF Core yet
                sp.ScreenDefinition != null ? sp.ScreenDefinition.Name : string.Empty
            ))
            .ToListAsync(cancellationToken);

        return results;
    }

    public async Task<ScreenPilotDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var result = await _context.ScreenPilots
            .AsNoTracking()
            .Include(sp => sp.ScreenDefinition)
            .Where(sp => sp.Id == id)
            .Select(sp => new ScreenPilotDto(
                sp.Id,
                sp.ScreenDefnId,
                sp.UserId,
                sp.Status,
                new DateTimeOffset(sp.UpdatedAt, TimeSpan.Zero),
                sp.UpdatedBy,
                null, // RowVersion - not used in EF Core yet
                sp.ScreenDefinition != null ? sp.ScreenDefinition.Name : string.Empty
            ))
            .FirstOrDefaultAsync(cancellationToken);

        return result;
    }

    public async Task<ScreenPilot?> GetEntityByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.ScreenPilots
            .FirstOrDefaultAsync(sp => sp.Id == id, cancellationToken);
    }

    public Task<ScreenPilot> CreateAsync(ScreenPilot screenPilot, CancellationToken cancellationToken = default)
    {
        _context.ScreenPilots.Add(screenPilot);
        // Note: SaveChanges will be called by UnitOfWork
        return Task.FromResult(screenPilot);
    }

    public Task<ScreenPilot> UpdateAsync(ScreenPilot screenPilot, CancellationToken cancellationToken = default)
    {
        _context.ScreenPilots.Update(screenPilot);
        // Note: SaveChanges will be called by UnitOfWork
        return Task.FromResult(screenPilot);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ScreenPilots.FindAsync(new object[] { id }, cancellationToken);
        if (entity != null)
        {
            _context.ScreenPilots.Remove(entity);
            // Note: SaveChanges will be called by UnitOfWork
        }
    }
}

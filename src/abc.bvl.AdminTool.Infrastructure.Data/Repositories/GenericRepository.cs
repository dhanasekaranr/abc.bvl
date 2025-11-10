using abc.bvl.AdminTool.Domain.Entities.Base;
using abc.bvl.AdminTool.Infrastructure.Data.Context;
using abc.bvl.AdminTool.Infrastructure.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace abc.bvl.AdminTool.Infrastructure.Data.Repositories;

/// <summary>
/// High-performance generic repository for admin entities
/// Implements compiled queries and optimizations for 100+ table scale
/// </summary>
public interface IGenericRepository<T> where T : BaseAdminEntity
{
    // Basic CRUD operations
    Task<T?> GetByIdAsync(long id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> GetByStatusAsync(byte status);
    Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize, Expression<Func<T, bool>>? filter = null);
    
    // Search operations (for BaseLookupEntity types)
    Task<IEnumerable<T>> SearchAsync(string searchTerm, int maxResults = 50);
    
    // Write operations
    Task<T> AddAsync(T entity);
    Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);
    Task<T> UpdateAsync(T entity);
    Task<BulkResult> BulkUpsertAsync(IEnumerable<T> entities);
    Task<bool> DeleteAsync(long id, string deletedBy);
    Task<int> BulkDeleteAsync(Expression<Func<T, bool>> predicate, string deletedBy);
    
    // Performance operations  
    Task<int> CountAsync(Expression<Func<T, bool>>? filter = null);
    Task<bool> ExistsAsync(long id);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
    
    // Projection queries for performance
    Task<IEnumerable<TResult>> SelectAsync<TResult>(Expression<Func<T, TResult>> selector);
    Task<IEnumerable<TResult>> SelectAsync<TResult>(Expression<Func<T, bool>> filter, Expression<Func<T, TResult>> selector);
}

/// <summary>
/// Implementation of generic repository with EF Core optimizations
/// </summary>
public class GenericRepository<T> : IGenericRepository<T> where T : BaseAdminEntity, new()
{
    private readonly ICurrentDbContextProvider ContextProvider;
    private AdminDbContext Context => ContextProvider.GetContext();
    private DbSet<T> DbSet => Context.Set<T>();
    private readonly ILogger<GenericRepository<T>> _logger;

    public GenericRepository(ICurrentDbContextProvider contextProvider, ILogger<GenericRepository<T>> logger)
    {
        ContextProvider = contextProvider;
        _logger = logger;
    }

    #region Basic CRUD Operations

    public virtual async Task<T?> GetByIdAsync(long id)
    {
        // Use compiled query for better performance
        return await CompiledQueries<T>.GetById(Context, id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await DbSet.Where(x => x.Status == 1).ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> GetByStatusAsync(byte status)
    {
        return await DbSet.Where(x => x.Status == status).ToListAsync();
    }

    public virtual async Task<PagedResult<T>> GetPagedAsync(
        int pageNumber, 
        int pageSize, 
        Expression<Func<T, bool>>? filter = null)
    {
        var query = DbSet.AsQueryable();
        
        if (filter != null)
        {
            query = query.Where(filter);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(x => x.Id) // Ensure consistent ordering
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<T>(
            Items: items,
            TotalCount: totalCount,
            PageNumber: pageNumber,
            PageSize: pageSize,
            TotalPages: (int)Math.Ceiling(totalCount / (double)pageSize)
        );
    }

    #endregion

    #region Search Operations

    public virtual async Task<IEnumerable<T>> SearchAsync(string searchTerm, int maxResults = 50)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Enumerable.Empty<T>();

        // Only works for BaseLookupEntity types
        if (typeof(T).IsAssignableTo(typeof(BaseLookupEntity)))
        {
            return await DbSet
                .Cast<BaseLookupEntity>()
                .Where(x => x.Status == 1 && 
                           (x.Code.Contains(searchTerm) || x.Name.Contains(searchTerm)))
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Take(maxResults)
                .Cast<T>()
                .ToListAsync();
        }

        // Fallback for non-lookup entities - search by Id
        if (long.TryParse(searchTerm, out var id))
        {
            var entity = await GetByIdAsync(id);
            return entity != null ? new[] { entity } : Enumerable.Empty<T>();
        }

        return Enumerable.Empty<T>();
    }

    public virtual async Task<T?> GetByCodeAsync(string code)
    {
        // Only works if T is BaseLookupEntity
        if (typeof(T).IsAssignableTo(typeof(BaseLookupEntity)))
        {
            return await DbSet
                .Cast<BaseLookupEntity>()
                .Where(x => x.Code == code && x.Status == 1)
                .Cast<T>()
                .FirstOrDefaultAsync();
        }
        
        return null;
    }

    #endregion

    #region Write Operations

    public virtual async Task<T> AddAsync(T entity)
    {
        entity.UpdateAuditFields("system"); // TODO: Get from current user context
        
        await DbSet.AddAsync(entity);
        await Context.SaveChangesAsync();
        
        _logger.LogInformation("Created {EntityType} with ID {Id}", typeof(T).Name, entity.Id);
        return entity;
    }

    public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
    {
        var entityList = entities.ToList();
        var currentUser = "system"; // TODO: Get from current user context
        
        foreach (var entity in entityList)
        {
            entity.UpdateAuditFields(currentUser);
        }

        await DbSet.AddRangeAsync(entityList);
        await Context.SaveChangesAsync();
        
        _logger.LogInformation("Created {Count} {EntityType} entities", entityList.Count, typeof(T).Name);
        return entityList;
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        entity.UpdateAuditFields("system"); // TODO: Get from current user context
        
        DbSet.Update(entity);
        await Context.SaveChangesAsync();
        
        _logger.LogInformation("Updated {EntityType} with ID {Id}", typeof(T).Name, entity.Id);
        return entity;
    }

    public virtual async Task<BulkResult> BulkUpsertAsync(IEnumerable<T> entities)
    {
        var entityList = entities.ToList();
        var currentUser = "system"; // TODO: Get from current user context
        
        var created = 0;
        var updated = 0;
        var errors = new List<string>();

        foreach (var entity in entityList)
        {
            try
            {
                entity.UpdateAuditFields(currentUser);
                
                if (entity.Id == 0)
                {
                    await DbSet.AddAsync(entity);
                    created++;
                }
                else
                {
                    DbSet.Update(entity);
                    updated++;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Error processing entity ID {entity.Id}: {ex.Message}");
                _logger.LogError(ex, "Error in bulk upsert for {EntityType} ID {Id}", typeof(T).Name, entity.Id);
            }
        }

        if (created > 0 || updated > 0)
        {
            await Context.SaveChangesAsync();
        }

        _logger.LogInformation("Bulk upsert completed: {Created} created, {Updated} updated, {Errors} errors", 
                              created, updated, errors.Count);

        return new BulkResult(created, updated, errors.Count, errors);
    }

    public virtual async Task<bool> DeleteAsync(long id, string deletedBy)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
            return false;

        entity.MarkDeleted(deletedBy);
        await Context.SaveChangesAsync();
        
        _logger.LogInformation("Soft deleted {EntityType} with ID {Id}", typeof(T).Name, id);
        return true;
    }

    public virtual async Task<int> BulkDeleteAsync(Expression<Func<T, bool>> predicate, string deletedBy)
    {
        var entities = await DbSet.Where(predicate).ToListAsync();
        
        foreach (var entity in entities)
        {
            entity.MarkDeleted(deletedBy);
        }

        await Context.SaveChangesAsync();
        
        _logger.LogInformation("Bulk soft deleted {Count} {EntityType} entities", entities.Count, typeof(T).Name);
        return entities.Count;
    }

    #endregion

    #region Performance Operations

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? filter = null)
    {
        var query = DbSet.AsQueryable();
        
        if (filter != null)
        {
            query = query.Where(filter);
        }

        return await query.CountAsync();
    }

    public virtual async Task<bool> ExistsAsync(long id)
    {
        return await DbSet.AnyAsync(x => x.Id == id);
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.AnyAsync(predicate);
    }

    public virtual async Task<IEnumerable<TResult>> SelectAsync<TResult>(Expression<Func<T, TResult>> selector)
    {
        return await DbSet.Select(selector).ToListAsync();
    }

    public virtual async Task<IEnumerable<TResult>> SelectAsync<TResult>(
        Expression<Func<T, bool>> filter, 
        Expression<Func<T, TResult>> selector)
    {
        return await DbSet.Where(filter).Select(selector).ToListAsync();
    }

    #endregion
}

/// <summary>
/// Compiled queries for maximum performance
/// These are compiled once and reused for all operations
/// </summary>
public static class CompiledQueries<T> where T : BaseAdminEntity
{
    public static readonly Func<AdminDbContext, long, Task<T?>> GetById =
        EF.CompileAsyncQuery((AdminDbContext ctx, long id) => 
            ctx.Set<T>().FirstOrDefault(x => x.Id == id));

    public static readonly Func<AdminDbContext, Task<List<T>>> GetAll =
        EF.CompileAsyncQuery((AdminDbContext ctx) =>
            ctx.Set<T>().Where(x => x.Status == 1).ToList());

    public static readonly Func<AdminDbContext, byte, Task<List<T>>> GetByStatus =
        EF.CompileAsyncQuery((AdminDbContext ctx, byte status) =>
            ctx.Set<T>().Where(x => x.Status == status).ToList());
}

/// <summary>
/// Result types for operations
/// </summary>
public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);

public record BulkResult(
    int Created,
    int Updated,
    int Errors,
    IEnumerable<string> ErrorMessages);

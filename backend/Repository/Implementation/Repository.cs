using System.Linq.Expressions;
using Domain.Common;
using Domain.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Repository.Interface;

namespace Repository.Implementation;

public class Repository<T>(ApplicationDbContext context) 
    : IRepository<T> where T : BaseEntity
{
    private readonly DbSet<T> _entities = context.Set<T>();

    public async Task InsertAsync(T entity)
    {
        context.Add(entity);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        context.Update(entity);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(T entity)
    {
        context.Remove(entity);
        await context.SaveChangesAsync();
    }
    
    public async Task<T?> GetByIdAsync(Guid id)
    {
        return await context.Set<T>()
            .Where(e => e.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        return await _entities.AnyAsync(predicate);
    }

    public async Task<List<TR>> GetAllAsync<TR>(
        Expression<Func<T, TR>> selector, 
        Expression<Func<T, bool>>? predicate = null, 
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
        bool asNoTracking = false)
    {
        IQueryable<T> query = _entities;
        if (predicate != null)
            query = query.Where(predicate);

        if (include != null)
            query = include(query);

        if (orderBy != null)
            query = orderBy(query);

        if (asNoTracking)
            query = query.AsNoTracking();

        return await query.Select(selector).ToListAsync();
    }

    public async Task<PaginatedResult<TR>> GetAllPagedAsync<TR>(Expression<Func<T, TR>> selector, int pageNumber, int pageSize, Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null, bool asNoTracking = false)
    {
        IQueryable<T> query = _entities;

        if (asNoTracking)
            query = query.AsNoTracking();

        if (include != null)
            query = include(query);
        
        if (predicate != null)
            query = query.Where(predicate);

        var totalCount = await query.CountAsync();

        if (orderBy != null)
            query = orderBy(query);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(selector)
            .ToListAsync();

        return new PaginatedResult<TR>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<List<TResult>> AggregateAsync<TKey, TResult>(Expression<Func<T, TKey>> groupBy, Expression<Func<IGrouping<TKey, T>, TResult>> selector, Expression<Func<T, bool>>? predicate = null,
        bool asNoTracking = false)
    {
        IQueryable<T> query = _entities;

        if (asNoTracking)
            query = query.AsNoTracking();
        

        if (predicate != null)
            query = query.Where(predicate);

        return await query
            .GroupBy(groupBy)
            .Select(selector)
            .ToListAsync();
    }
}
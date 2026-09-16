using System.Linq.Expressions;
using Domain.Common;
using Domain.Dto;
using Microsoft.EntityFrameworkCore.Query;

namespace Repository.Interface;

public interface IRepository<T> where T : BaseEntity 
{
    Task InsertAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
    Task<T?> GetByIdAsync(Guid id);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

    Task<List<TR>> GetAllAsync<TR>(Expression<Func<T, TR>> selector,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
        bool asNoTracking = false);

    Task<PaginatedResult<TR>> GetAllPagedAsync<TR>(
        Expression<Func<T, TR>> selector,
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<IQueryable<T>, IIncludableQueryable<T, object>>? include = null,
        bool asNoTracking = false);

    Task<List<TResult>> AggregateAsync<TKey, TResult>(
        Expression<Func<T, TKey>> groupBy,
        Expression<Func<IGrouping<TKey, T>, TResult>> selector,
        Expression<Func<T, bool>>? predicate = null,
        bool asNoTracking = false);

}
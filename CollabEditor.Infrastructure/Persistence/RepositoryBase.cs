using Microsoft.EntityFrameworkCore;

namespace CollabEditor.Infrastructure.Persistence;

public abstract class RepositoryBase<TEntity>(IDbContextFactory<CollabEditorDbContext> contextFactory)
    where TEntity : class, new()
{
    protected async Task<T> QueryAsync<T>(Func<IQueryable<TEntity>, Task<T>> query)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await query(context.Set<TEntity>().AsNoTracking());
    }
    
    protected async Task ExecuteAsync(Func<DbSet<TEntity>, Task> execute)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        await execute(context.Set<TEntity>());
        await context.SaveChangesAsync();
    }
    
    protected async Task<T> QueryAsync<T>(
        CollabEditorDbContext context,
        Func<IQueryable<TEntity>, Task<T>> query)
    {
        return await query(context.Set<TEntity>().AsNoTracking());
    }
    
    protected async Task ExecuteAsync(
        CollabEditorDbContext context,
        Func<DbSet<TEntity>, Task> execute)
    {
        await execute(context.Set<TEntity>());
        // Don't save here - let Unit of Work or caller handle it!
    }
    
    protected async Task<CollabEditorDbContext> CreateContextAsync() => await contextFactory.CreateDbContextAsync();
}
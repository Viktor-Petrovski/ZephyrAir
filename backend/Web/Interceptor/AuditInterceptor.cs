using Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Service.Interface;

namespace Web.Interceptor;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;

    public AuditInterceptor(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAudit(DbContext? context)
    {
        if (context is null) return;

        var now = DateTime.UtcNow;
        var userId = _currentUser.GetUserId() ?? "system";

        // TKey is string for every auditable entity, so match on the closed type.
        foreach (var entry in context.ChangeTracker.Entries<BaseAuditableEntity<string>>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedById = userId;
                entry.Entity.LastModifiedAt = now;
                entry.Entity.LastModifiedById = userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastModifiedAt = now;
                entry.Entity.LastModifiedById = userId;
            }
        }
    }
}

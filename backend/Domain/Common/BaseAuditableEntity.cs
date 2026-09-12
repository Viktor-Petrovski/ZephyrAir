namespace Domain.Common;

// TKey is the type of the audit user id (ApplicationUser.Id — a string IdentityUser key).
// The interceptor fills these in; no FK/navigation is declared (audit only).
public class BaseAuditableEntity<TKey> : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public TKey? CreatedById { get; set; }

    public DateTime LastModifiedAt { get; set; }
    public TKey? LastModifiedById { get; set; }
}

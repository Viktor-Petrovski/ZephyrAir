using Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Domain.Models;

// UserName / Email / etc. are inherited from IdentityUser
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public UserRole Role { get; set; }

    public virtual ICollection<AlertSubscription> AlertSubscriptions { get; set; } = [];
}

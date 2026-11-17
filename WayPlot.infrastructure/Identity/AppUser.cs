using Microsoft.AspNetCore.Identity;

namespace WayPlot.infrastructure.Identity;

/// <summary>Login account in the Master database. TenantId tells the app which tenant database to open.</summary>
public class AppUser : IdentityUser<Guid>
{
    public Guid? TenantId { get; set; }
    public Guid? BranchId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

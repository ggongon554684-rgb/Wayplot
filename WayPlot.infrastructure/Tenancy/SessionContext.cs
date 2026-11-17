using WayPlot.domain.Common;
using WayPlot.domain.Interfaces;

namespace WayPlot.infrastructure.Tenancy;

/// <summary>Holds the signed-in user. The desktop app fills it at login.</summary>
public sealed class SessionContext : ICurrentUser
{
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = "anonymous";
    public Guid? TenantId { get; set; }
    public Guid? BranchId { get; set; }
    public string Role { get; set; } = string.Empty;
    public Tier? Tier { get; set; }
    public bool IsAuthenticated => UserId is not null;

    /// <summary>Used by migrations and the seeder.</summary>
    public static SessionContext SystemUser { get; } = new() { UserName = "system", Role = "System" };
}

using WayPlot.domain.Common;

namespace WayPlot.domain.Interfaces;

public interface IClock
{
    DateTime UtcNow { get; }
}

/// <summary>The signed-in session. Services read tenant, branch and role from here, never from a screen.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string UserName { get; }
    Guid? TenantId { get; }
    Guid? BranchId { get; }
    string Role { get; }
    Tier? Tier { get; }
    bool IsAuthenticated { get; }
}

/// <summary>One place that says which modules the tenant's plan includes.</summary>
public interface ITenantFeatures
{
    bool DataCollection { get; }
    bool MainTransaction { get; }
    bool DealsPayments { get; }
    bool BusinessIntelligence { get; }
    bool Actions { get; }
    bool Branching { get; }
}

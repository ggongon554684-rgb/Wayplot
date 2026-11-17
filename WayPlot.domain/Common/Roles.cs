namespace WayPlot.domain.Common;

public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string TenantAdmin = "TenantAdmin";
    public const string BranchManager = "BranchManager";
    public const string Agent = "Agent";

    public static readonly string[] All = [SuperAdmin, TenantAdmin, BranchManager, Agent];
}

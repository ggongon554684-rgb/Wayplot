using WayPlot.domain.Common;
using WayPlot.domain.Entities;
using WayPlot.domain.Interfaces;

namespace WayPlot.domain.Services.DataCollection;

/// <summary>Describes the SQL scope a tenant repository must apply before returning data.</summary>
public enum TenantDataScopeKind { Tenant, Agent, Branch }

public readonly record struct TenantDataScope(TenantDataScopeKind Kind, Guid? StaffId = null, Guid? BranchId = null);

/// <summary>
/// The single authorization policy for data collection. Services use it before reads and writes;
/// repositories receive its scope so records are never loaded and filtered in memory.
/// </summary>
public sealed class TenantAccessPolicy(ICurrentUser user)
{
    public bool CanMutateData => (user.Role is Roles.Agent or Roles.TenantAdmin) && user.UserId is not null;
    public bool CanReadParcels =>
        (user.Role is Roles.Agent or Roles.TenantAdmin)
        || user.Role == Roles.BranchManager && user.BranchId is not null;

    public string MutationUnavailableReason => user.Role switch
    {
        Roles.BranchManager => "Branch Managers have read-only access.",
        Roles.SuperAdmin => "Super Admin cannot access tenant records.",
        _ => "Only Agents and Tenant Admins can make changes."
    };

    public ServiceResult<TenantDataScope> GetDataReadScope(string resource)
    {
        if (user.Role == Roles.TenantAdmin)
            return ServiceResult<TenantDataScope>.Success(new TenantDataScope(TenantDataScopeKind.Tenant));

        if (user.Role == Roles.Agent)
        {
            if (user.UserId is not { } staffId)
                return ServiceResult<TenantDataScope>.Fail($"Cannot view {resource}: your staff account could not be identified. Sign in again.");
            return ServiceResult<TenantDataScope>.Success(new TenantDataScope(TenantDataScopeKind.Agent, StaffId: staffId));
        }

        if (user.Role == Roles.BranchManager)
        {
            if (user.BranchId is not { } branchId)
                return ServiceResult<TenantDataScope>.Fail($"Cannot view {resource}: your staff account has no assigned branch. Ask your Tenant Admin.");
            return ServiceResult<TenantDataScope>.Success(new TenantDataScope(TenantDataScopeKind.Branch, BranchId: branchId));
        }

        return ServiceResult<TenantDataScope>.Fail(user.Role == Roles.SuperAdmin
            ? $"Super Admin cannot access tenant {resource}."
            : $"Your role cannot access tenant {resource}.");
    }

    public RuleResult RequireDataMutation(string action)
    {
        if (user.Role == Roles.Agent || user.Role == Roles.TenantAdmin)
            return user.UserId is not null
                ? RuleResult.Success()
                : RuleResult.Fail($"Cannot {action}: your staff account could not be identified. Sign in again.");

        return RuleResult.Fail(user.Role switch
        {
            Roles.BranchManager => $"Cannot {action}: Branch Managers have read-only access.",
            Roles.SuperAdmin => $"Cannot {action}: Super Admin cannot access tenant records.",
            _ => $"Cannot {action}: only Agents and Tenant Admins can make changes."
        });
    }

    public RuleResult RequireLeadMutation(Lead lead, string action)
    {
        var mutation = RequireDataMutation(action);
        if (!mutation.Ok)
            return mutation;

        if (user.Role == Roles.Agent && lead.AssignedStaffId != user.UserId)
            return RuleResult.Fail($"Cannot {action}: Agents may only change leads assigned to them.");

        return RuleResult.Success();
    }

    public RuleResult RequireParcelRead()
    {
        if (CanReadParcels)
            return RuleResult.Success();

        if (user.Role == Roles.BranchManager)
            return RuleResult.Fail("Cannot view parcels: your staff account has no assigned branch. Ask your Tenant Admin.");

        return RuleResult.Fail(user.Role == Roles.SuperAdmin
            ? "Super Admin cannot access tenant parcels."
            : "Your role cannot access tenant parcels.");
    }

    public RuleResult CanPlotParcel()
    {
        if (user.Role is Roles.Agent or Roles.TenantAdmin)
            return user.UserId is not null
                ? RuleResult.Success()
                : RuleResult.Fail("Cannot plot a parcel: your staff account could not be identified. Sign in again.");

        return ParcelMutationRefusal("plot parcels");
    }

    public RuleResult CanEditParcel(Parcel parcel)
        => CanEditParcel(parcel.AssignedStaffId);

    public RuleResult CanEditParcel(Guid? assignedStaffId)
    {
        if (user.Role == Roles.TenantAdmin)
            return user.UserId is not null
                ? RuleResult.Success()
                : RuleResult.Fail("Cannot edit this parcel: your staff account could not be identified. Sign in again.");

        if (user.Role == Roles.Agent)
        {
            if (user.UserId is not { } staffId)
                return RuleResult.Fail("Cannot edit this parcel: your staff account could not be identified. Sign in again.");
            return assignedStaffId == staffId
                ? RuleResult.Success()
                : RuleResult.Fail("Cannot edit this parcel: Agents may edit only parcels assigned to them.");
        }

        return ParcelMutationRefusal("edit parcels");
    }

    public RuleResult CanChangeParcelPrice() => RequireParcelAdminAction("change parcel prices");

    public RuleResult CanReassignParcel() => RequireParcelAdminAction("reassign parcels");

    public RuleResult CanArchiveParcel() => RequireParcelAdminAction("archive parcels");

    public bool IsAgentAssignedToAnotherStaff(Guid? assignedStaffId) =>
        user.Role == Roles.Agent && assignedStaffId is { } staffId && staffId != user.UserId;

    public Guid? CurrentStaffId => user.UserId;
    public Guid? CurrentBranchId => user.BranchId;
    public bool IsAgent => user.Role == Roles.Agent;

    private RuleResult ParcelMutationRefusal(string action) =>
        RuleResult.Fail(user.Role switch
        {
            Roles.BranchManager => $"Cannot {action}: Branch Managers have read-only access.",
            Roles.SuperAdmin => $"Cannot {action}: Super Admin cannot access tenant records.",
            _ => $"Cannot {action}: only Agents and Tenant Admins can make changes."
        });

    private RuleResult RequireParcelAdminAction(string action)
    {
        if (user.Role == Roles.TenantAdmin)
            return user.UserId is not null
                ? RuleResult.Success()
                : RuleResult.Fail($"Cannot {action}: your staff account could not be identified. Sign in again.");

        return RuleResult.Fail(user.Role switch
        {
            Roles.Agent => $"Cannot {action}: only the Tenant Admin can do this.",
            Roles.BranchManager => $"Cannot {action}: Branch Managers have read-only access.",
            Roles.SuperAdmin => $"Cannot {action}: Super Admin cannot access tenant records.",
            _ => $"Cannot {action}: only the Tenant Admin can do this."
        });
    }
}

using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Events;
using WayPlot.domain.Interfaces;
using WayPlot.domain.Repositories;
using WayPlot.domain.Services.DataCollection;

namespace WayPlot.domain.Services.Admin;

public sealed class StaffService(
    ITenantUnitOfWorkFactory uowFactory,
    ICurrentUser user,
    TenantAccessPolicy access,
    ITenantFeatures features,
    IClock clock,
    IDomainEventDispatcher events)
{
    public async Task<ServiceResult<IReadOnlyList<StaffListItem>>> ListStaffAsync()
    {
        if (user.Role is not (Roles.TenantAdmin or Roles.BranchManager))
            return ServiceResult<IReadOnlyList<StaffListItem>>.Fail(
                user.Role == Roles.SuperAdmin
                    ? "Super Admin cannot access tenant staff."
                    : "Only Tenant Admins and Branch Managers can view staff.");

        var scope = access.GetDataReadScope("staff");
        if (!scope.Ok)
            return ServiceResult<IReadOnlyList<StaffListItem>>.Fail(scope.Message);

        await using var uow = uowFactory.Create();
        var staff = await uow.Staff.ListAsync(scope.Value);
        var leads = await uow.Leads.ListAsync(null, null, scope: scope.Value);
        var parcels = await uow.Parcels.ListVisibleToAsync(scope.Value);
        var items = staff.Select(member => new StaffListItem(
            member.Id,
            member.Username,
            member.FullName,
            member.Role,
            member.BranchId,
            member.IsActive,
            leads.Count(lead => lead.AssignedStaffId == member.Id
                && lead.Stage is not (LeadStage.Closed or LeadStage.Lost)),
            parcels.Count(parcel => parcel.AssignedStaffId == member.Id),
            parcels.Count(parcel => parcel.AssignedStaffId == member.Id
                && parcel.Status == ParcelStatus.Available),
            member.Version)).ToList();
        return ServiceResult<IReadOnlyList<StaffListItem>>.Success(items);
    }

    public async Task<ServiceResult<IReadOnlyList<BranchListItem>>> ListBranchesAsync()
    {
        var scope = access.GetDataReadScope("branches");
        if (!scope.Ok)
            return ServiceResult<IReadOnlyList<BranchListItem>>.Fail(scope.Message);
        if (user.Role != Roles.TenantAdmin)
            return ServiceResult<IReadOnlyList<BranchListItem>>.Fail("Only a Tenant Admin can view branches.");
        if (!features.Branching)
            return ServiceResult<IReadOnlyList<BranchListItem>>.Fail("Branch assignment is available only in Tier C.");

        await using var uow = uowFactory.Create();
        var branches = await uow.Branches.ListAsync();
        return ServiceResult<IReadOnlyList<BranchListItem>>.Success(
            branches.Select(branch => new BranchListItem(branch.Id, branch.Name)).ToList());
    }

    public Task<ServiceResult<bool>> DeactivateAsync(Guid staffId, int expectedVersion) =>
        SetActiveAsync(staffId, expectedVersion, false);

    public Task<ServiceResult<bool>> ReactivateAsync(Guid staffId, int expectedVersion) =>
        SetActiveAsync(staffId, expectedVersion, true);

    public async Task<ServiceResult<bool>> ChangeBranchAsync(
        Guid staffId, Guid branchId, int expectedVersion)
    {
        var permission = RequireTenantAdmin("change a staff member's branch");
        if (!permission.Ok)
            return ServiceResult<bool>.Fail(permission.Message);
        if (!features.Branching)
            return ServiceResult<bool>.Fail("Branch assignment is available only in Tier C.");

        await using var uow = uowFactory.Create();
        var member = await uow.Staff.GetAsync(staffId);
        if (member is null)
            return ServiceResult<bool>.Fail("That staff account was not found.");
        if (member.Version != expectedVersion)
            return ServiceResult<bool>.Fail("The staff account changed concurrently. Reload and try again.");
        if (!await uow.Branches.ExistsAsync(branchId))
            return ServiceResult<bool>.Fail("Choose an existing branch.");
        if (member.BranchId == branchId)
            return ServiceResult<bool>.Success(true, "The staff member is already assigned to that branch.");

        member.BranchId = branchId;
        try
        {
            await uow.ExecuteInTransactionAsync(async () => await uow.SaveAsync());
        }
        catch (StaffConcurrencyConflictException)
        {
            return ServiceResult<bool>.Fail("The staff account changed concurrently. Reload and try again.");
        }
        await events.DispatchAsync(new StaffChanged(member.Id, clock.UtcNow));
        return ServiceResult<bool>.Success(true, "Staff branch changed.");
    }

    private async Task<ServiceResult<bool>> SetActiveAsync(
        Guid staffId, int expectedVersion, bool isActive)
    {
        var action = isActive ? "reactivate a staff account" : "deactivate a staff account";
        var permission = RequireTenantAdmin(action);
        if (!permission.Ok)
            return ServiceResult<bool>.Fail(permission.Message);

        await using var uow = uowFactory.Create();
        var member = await uow.Staff.GetAsync(staffId);
        if (member is null)
            return ServiceResult<bool>.Fail("That staff account was not found.");
        if (member.Version != expectedVersion)
            return ServiceResult<bool>.Fail("The staff account changed concurrently. Reload and try again.");
        if (member.IsActive == isActive)
            return ServiceResult<bool>.Success(true, isActive
                ? "The staff account is already active."
                : "The staff account is already inactive.");
        if (!isActive && member.Id == user.UserId)
            return ServiceResult<bool>.Fail("You cannot deactivate your own account.");
        if (!isActive && member.Role == Roles.TenantAdmin
            && await uow.Staff.CountActiveTenantAdminsAsync() <= 1)
            return ServiceResult<bool>.Fail("The last active Tenant Admin cannot be deactivated.");

        if (!isActive && member.Role == Roles.Agent)
        {
            var openLeads = (await uow.Leads.ListAsync(null, null, member.Id))
                .Count(lead => lead.Stage is not (LeadStage.Closed or LeadStage.Lost));
            var availableParcels = (await uow.Parcels.ListAsync())
                .Count(parcel => parcel.AssignedStaffId == member.Id
                    && parcel.Status == ParcelStatus.Available);
            if (openLeads > 0 || availableParcels > 0)
                return ServiceResult<bool>.Fail(
                    $"Reassign this Agent's work first: {openLeads} open lead(s) and {availableParcels} available parcel(s). Use Reassign.");
        }

        member.IsActive = isActive;
        try
        {
            await uow.ExecuteInTransactionAsync(async () => await uow.SaveAsync());
        }
        catch (StaffConcurrencyConflictException)
        {
            return ServiceResult<bool>.Fail("The staff account changed concurrently. Reload and try again.");
        }
        await events.DispatchAsync(new StaffChanged(member.Id, clock.UtcNow));
        return ServiceResult<bool>.Success(true, isActive ? "Staff account reactivated." : "Staff account deactivated.");
    }

    private RuleResult RequireTenantAdmin(string action)
    {
        var permission = access.RequireDataMutation(action);
        if (!permission.Ok)
            return permission;
        return user.Role == Roles.TenantAdmin
            ? RuleResult.Success()
            : RuleResult.Fail("Only a Tenant Admin can manage staff accounts.");
    }
}

using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Interfaces;
using WayPlot.domain.Repositories;
using WayPlot.domain.Services.DataCollection;

namespace WayPlot.domain.Services.Admin;

public sealed class StaffService(
    ITenantUnitOfWorkFactory uowFactory,
    ICurrentUser user,
    TenantAccessPolicy access)
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
        var ids = staff.Select(member => member.Id).ToArray();
        var leads = await uow.Leads.ListAsync(null, null);
        var parcels = await uow.Parcels.ListAsync();
        var items = staff.Select(member => new StaffListItem(
            member.Id,
            member.Username,
            member.FullName,
            member.Role,
            member.BranchId,
            member.IsActive,
            leads.Count(lead => lead.AssignedStaffId == member.Id
                && lead.Stage is not (LeadStage.Closed or LeadStage.Lost)),
            parcels.Count(parcel => parcel.AssignedStaffId == member.Id))).ToList();
        return ServiceResult<IReadOnlyList<StaffListItem>>.Success(items);
    }
}

using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Interfaces;
using WayPlot.domain.Services.Admin;
using WayPlot.domain.Services.DataCollection;
using WayPlot.domain.Services.MainTransaction;

namespace WayPlot.Presenters;

public interface ICommissionsView
{
    bool IsDisposed { get; }
    void ShowPayouts(IReadOnlyList<PayoutListItem> payouts);
    void ShowStatus(string text, bool isError);
}

public sealed record CommissionActionAvailability(bool Visible, bool Enabled, string? DisabledReason);

public sealed record CommissionActionStates(
    CommissionActionAvailability ManageRules,
    CommissionActionAvailability Approve,
    CommissionActionAvailability MarkPaid);

public sealed class CommissionsPresenter(
    ICommissionsView view,
    CommissionService commissions,
    ICurrentUser user,
    ITenantFeatures features,
    CompanyProfileService companyProfile,
    StaffService staff,
    ParcelService parcels)
{
    private int _latest;

    public bool HasBranching => features.Branching;
    public bool CanReadCompanyProfile =>
        user.Role is Roles.TenantAdmin or Roles.BranchManager or Roles.Agent;
    public bool CanEditCompanyProfile => user.Role == Roles.TenantAdmin;

    public async Task<ServiceResult<IReadOnlyList<CommissionOverrideTarget>>> GetOverrideTargetsAsync()
    {
        try
        {
            var staffResult = await staff.ListStaffAsync();
            if (!staffResult.Ok || staffResult.Value is null)
                return ServiceResult<IReadOnlyList<CommissionOverrideTarget>>.Fail(staffResult.Message);
            var parcelResult = await parcels.ListAsync();
            if (!parcelResult.Ok || parcelResult.Value is null)
                return ServiceResult<IReadOnlyList<CommissionOverrideTarget>>.Fail(parcelResult.Message);

            var targets = staffResult.Value
                .Where(member => member.IsActive && member.Role == Roles.Agent)
                .Select(member => new CommissionOverrideTarget(
                    CommissionOverrideScope.Agent, member.Id, member.DisplayName))
                .Concat(parcelResult.Value.Select(parcel => new CommissionOverrideTarget(
                    CommissionOverrideScope.Parcel, parcel.Id, parcel.Label)))
                .ToList();
            return ServiceResult<IReadOnlyList<CommissionOverrideTarget>>.Success(targets);
        }
        catch (Exception)
        {
            return ServiceResult<IReadOnlyList<CommissionOverrideTarget>>.Fail(
                "Commission override choices could not be loaded. Reload and try again.");
        }
    }

    public async Task<ServiceResult<CompanyProfileSnapshot>> GetCompanyProfileAsync()
    {
        try
        {
            return await companyProfile.GetAsync();
        }
        catch (Exception)
        {
            return ServiceResult<CompanyProfileSnapshot>.Fail(
                "The company profile could not be loaded. Check the connection and try again.");
        }
    }

    public async Task<ServiceResult<CompanyProfileSnapshot>> SaveCompanyProfileAsync(CompanyProfileRequest request)
    {
        try
        {
            return await companyProfile.SaveAsync(request);
        }
        catch (Exception)
        {
            return ServiceResult<CompanyProfileSnapshot>.Fail(
                "The company profile could not be saved. No changes were made; reload and try again.");
        }
    }

    public CommissionActionStates GetActionStates(PayoutListItem? payout)
    {
        var isAdmin = user.Role == Roles.TenantAdmin;
        var approveReason = payout is null
            ? "Select a payout first."
            : payout.Status != CommissionStatus.Pending
                ? "Only a pending payout can be approved."
                : payout.ApprovalBlockedReason;
        var paidReason = payout is null
            ? "Select a payout first."
            : payout.Status != CommissionStatus.Approved
                ? "Only an approved payout can be marked Paid."
                : payout.PaidBlockedReason;

        return new CommissionActionStates(
            new CommissionActionAvailability(isAdmin, isAdmin, null),
            new CommissionActionAvailability(isAdmin, isAdmin && approveReason is null, approveReason),
            new CommissionActionAvailability(isAdmin, isAdmin && payout?.CanMarkPaid == true, paidReason));
    }

    public async Task LoadAsync(string? notice = null)
    {
        var ticket = ++_latest;
        if (view.IsDisposed) return;
        view.ShowStatus("Loading commissions...", false);

        try
        {
            var result = await commissions.ListAsync();
            if (ticket != _latest || view.IsDisposed) return;
            if (!result.Ok || result.Value is null)
            {
                view.ShowStatus(result.Message, true);
                return;
            }

            view.ShowPayouts(result.Value);
            view.ShowStatus(notice ?? Summary(result.Value.Count), false);
        }
        catch (Exception)
        {
            if (ticket == _latest && !view.IsDisposed)
                view.ShowStatus("Could not load commissions. Check the database connection and try again.", true);
        }
    }

    public async Task<ServiceResult<CommissionRulesSnapshot>> GetRulesAsync()
    {
        try
        {
            return await commissions.GetRulesAsync();
        }
        catch (Exception)
        {
            return ServiceResult<CommissionRulesSnapshot>.Fail(
                "Could not load commission rules. Check the database connection and try again.");
        }
    }

    public async Task<ServiceResult<CommissionRulesSnapshot>> SaveRulesAsync(CommissionRulesRequest request)
    {
        try
        {
            return await commissions.SaveRulesAsync(request);
        }
        catch (Exception)
        {
            return ServiceResult<CommissionRulesSnapshot>.Fail(
                "Commission rules could not be saved. No changes were made; reload and try again.");
        }
    }

    public Task<ServiceResult<bool>> ApproveAsync(PayoutListItem payout) =>
        ChangeAsync(() => commissions.ApproveAsync(payout.Kind, payout.Id), "The payout could not be approved. Reload and try again.");

    public Task<ServiceResult<bool>> MarkPaidAsync(PayoutListItem payout) =>
        ChangeAsync(() => commissions.MarkPaidAsync(payout.Kind, payout.Id), "The payout could not be marked Paid. Reload and try again.");

    private async Task<ServiceResult<bool>> ChangeAsync(
        Func<Task<ServiceResult<bool>>> operation, string failureMessage)
    {
        try
        {
            return await operation();
        }
        catch (Exception)
        {
            return ServiceResult<bool>.Fail(failureMessage);
        }
    }

    private static string Summary(int count) =>
        count == 0 ? "No commissions or referral bonuses yet."
        : count == 1 ? "1 payout"
        : $"{count} payouts";
}

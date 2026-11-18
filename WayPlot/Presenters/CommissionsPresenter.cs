using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Interfaces;
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
    ITenantFeatures features)
{
    private int _latest;

    public bool HasBranching => features.Branching;

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

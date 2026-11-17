using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Interfaces;
using WayPlot.domain.Services.MainTransaction;

namespace WayPlot.Presenters;

public interface IDealsView
{
    bool IsDisposed { get; }
    void ShowDeals(IReadOnlyList<DealListItem> deals);
    void ShowPayments(IReadOnlyList<PaymentListItem> payments);
    void ShowStatus(string text, bool isError);
    void ShowPaymentStatus(string text, bool isError);
}

public sealed record ActionAvailability(bool Visible, bool Enabled, string? DisabledReason);

public sealed record DealActionStates(
    ActionAvailability RequestClosed,
    ActionAvailability ConfirmClosed,
    ActionAvailability CancelDeal,
    ActionAvailability RecordPayment,
    ActionAvailability ConfirmPayment,
    ActionAvailability ReturnPayment);

public sealed class DealsPresenter(IDealsView view, DealService deals, ICurrentUser user)
{
    private int _latest;
    private int _latestPayments;

    public DealActionStates GetActionStates(DealListItem? deal, PaymentListItem? payment)
    {
        var isAgent = user.Role == Roles.Agent;
        var isTenantAdmin = user.Role == Roles.TenantAdmin;

        return new DealActionStates(
            ForRole(isAgent, null),
            ForRole(isTenantAdmin, ConfirmClosedDisabledReason(deal)),
            ForRole(isTenantAdmin, CancelDealDisabledReason(deal)),
            ForRole(isAgent, RecordPaymentDisabledReason(deal)),
            ForRole(isTenantAdmin, ConfirmPaymentDisabledReason(deal, payment)),
            ForRole(isTenantAdmin, ReturnPaymentDisabledReason(deal, payment)));
    }

    public async Task LoadAsync(string? notice = null)
    {
        var ticket = ++_latest;
        if (view.IsDisposed) return;
        view.ShowStatus("Loading deals...", false);

        try
        {
            var result = await deals.ListAsync();
            if (ticket != _latest || view.IsDisposed) return;
            if (!result.Ok || result.Value is null)
            {
                view.ShowStatus(result.Message, true);
                return;
            }

            view.ShowDeals(result.Value);
            view.ShowStatus(notice ?? Summary(result.Value.Count), false);
        }
        catch (Exception)
        {
            if (ticket == _latest && !view.IsDisposed)
                view.ShowStatus("Could not load deals. Check the database connection and try again.", true);
        }
    }

    public async Task<ServiceResult<IReadOnlyList<DealCandidateItem>>> GetCandidatesAsync()
    {
        try
        {
            return await deals.ListCandidatesAsync();
        }
        catch (Exception)
        {
            return ServiceResult<IReadOnlyList<DealCandidateItem>>.Fail(
                "Could not load Negotiation leads. Check the database connection and try again.");
        }
    }

    public async Task<ServiceResult<DealListItem>> CreateAsync(CreateDealRequest request)
    {
        try
        {
            return await deals.CreateAsync(request);
        }
        catch (Exception)
        {
            return ServiceResult<DealListItem>.Fail(
                "The Closed request could not be submitted. Nothing was confirmed; reload the list and try again.");
        }
    }

    public async Task<ServiceResult<DealListItem>> ConfirmAsync(Guid dealId)
    {
        try
        {
            return await deals.ConfirmAsync(dealId);
        }
        catch (Exception)
        {
            return ServiceResult<DealListItem>.Fail(
                "The deal could not be confirmed. Reload the list and try again.");
        }
    }

    public async Task<ServiceResult<DealListItem>> CancelAsync(Guid dealId)
    {
        try
        {
            return await deals.CancelAsync(dealId);
        }
        catch (Exception)
        {
            return ServiceResult<DealListItem>.Fail(
                "The deal could not be canceled. Reload the list and try again.");
        }
    }

    public async Task LoadPaymentsAsync(Guid dealId)
    {
        var ticket = ++_latestPayments;
        if (view.IsDisposed) return;
        view.ShowPaymentStatus("Loading payments...", false);

        try
        {
            var result = await deals.ListPaymentsAsync(dealId);
            if (ticket != _latestPayments || view.IsDisposed) return;
            if (!result.Ok || result.Value is null)
            {
                view.ShowPaymentStatus(result.Message, true);
                return;
            }

            view.ShowPayments(result.Value);
            view.ShowPaymentStatus(
                result.Value.Count == 0
                    ? "No payments recorded for this deal."
                    : result.Value.Count == 1 ? "1 payment" : $"{result.Value.Count} payments",
                false);
        }
        catch (Exception)
        {
            if (ticket == _latestPayments && !view.IsDisposed)
                view.ShowPaymentStatus("Could not load payments. Check the database connection and try again.", true);
        }
    }

    public void ClearPayments(string message)
    {
        _latestPayments++;
        if (view.IsDisposed) return;
        view.ShowPayments(Array.Empty<PaymentListItem>());
        view.ShowPaymentStatus(message, false);
    }

    public async Task<ServiceResult<PaymentListItem>> RecordPaymentAsync(RecordPaymentRequest request)
    {
        try
        {
            return await deals.RecordPaymentAsync(request);
        }
        catch (Exception)
        {
            return ServiceResult<PaymentListItem>.Fail(
                "The payment could not be recorded. Reload and try again.");
        }
    }

    public async Task<ServiceResult<PaymentListItem>> ConfirmPaymentAsync(Guid paymentId)
    {
        try
        {
            return await deals.ConfirmPaymentAsync(paymentId);
        }
        catch (Exception)
        {
            return ServiceResult<PaymentListItem>.Fail(
                "The payment could not be confirmed. Reload and try again.");
        }
    }

    public async Task<ServiceResult<PaymentListItem>> ReturnPaymentAsync(Guid paymentId, string? note)
    {
        try
        {
            return await deals.ReturnPaymentAsync(paymentId, note);
        }
        catch (Exception)
        {
            return ServiceResult<PaymentListItem>.Fail(
                "The payment could not be returned. Reload and try again.");
        }
    }

    private static string Summary(int count) =>
        count == 0 ? "No deals yet." : count == 1 ? "1 deal" : $"{count} deals";

    private static ActionAvailability ForRole(bool visible, string? disabledReason) =>
        new(visible, visible && disabledReason is null, disabledReason);

    private static string? ConfirmClosedDisabledReason(DealListItem? deal)
    {
        if (deal is null) return "Select a deal first.";
        if (deal.Status == DealStatus.Cancelled) return "This deal is cancelled.";
        if (deal.Status == DealStatus.FullyPaid) return "This deal is already fully paid.";
        if (deal.Status == DealStatus.Defaulted) return "A defaulted deal cannot be confirmed.";
        if (deal.IsConfirmed) return "This deal is already confirmed.";
        if (!deal.IsActive) return "This deal is no longer active.";
        if (deal.Status != DealStatus.Current) return "Only current deals can be confirmed.";
        if (!deal.CloseRequested) return "Closed has not been requested by an Agent.";
        if (deal.LeadStage != LeadStage.Negotiation) return "Only a Negotiation lead can be confirmed as Closed.";
        return null;
    }

    private static string? CancelDealDisabledReason(DealListItem? deal)
    {
        if (deal is null) return "Select a deal first.";
        if (deal.Status == DealStatus.Cancelled) return "This deal is already cancelled.";
        if (deal.Status == DealStatus.FullyPaid) return "A fully paid deal cannot be cancelled.";
        if (deal.Status == DealStatus.Defaulted) return "A defaulted deal cannot be cancelled.";
        if (deal.IsConfirmed) return "A confirmed deal cannot be cancelled.";
        if (!deal.IsActive || deal.Status != DealStatus.Current) return "Only an active, unconfirmed deal can be cancelled.";
        if (deal.HasPaymentActivity) return "This deal has payment activity and cannot be cancelled.";
        return null;
    }

    private static string? RecordPaymentDisabledReason(DealListItem? deal)
    {
        if (deal is null) return "Select a deal first.";
        if (deal.Status == DealStatus.FullyPaid) return "This deal is already fully paid.";
        if (deal.Status == DealStatus.Cancelled) return "This deal is cancelled.";
        if (deal.Status == DealStatus.Defaulted) return "A defaulted deal cannot receive payments.";
        if (!deal.IsActive || deal.Status != DealStatus.Current) return "Only an active, current deal can receive payments.";
        if (!deal.IsConfirmed) return "A Tenant Admin must confirm this deal before payments can be recorded.";
        if (deal.ParcelStatus is ParcelStatus.Sold or ParcelStatus.Leased)
            return $"Parcel is {deal.ParcelStatus}; no further payment can be recorded.";
        if (deal.RemainingBalance <= 0) return "Payments already cover the deal price; wait for Tenant Admin review.";
        return null;
    }

    private static string? ConfirmPaymentDisabledReason(DealListItem? deal, PaymentListItem? payment)
    {
        if (deal is null) return "Select a deal first.";
        if (payment is null) return "Select a payment first.";
        if (payment.DealId != deal.Id) return "Select a payment from this deal.";
        if (payment.Status == PaymentStatus.Confirmed) return "This payment is already confirmed.";
        if (payment.Status == PaymentStatus.Returned) return "This payment was returned.";
        if (deal.Status == DealStatus.FullyPaid) return "This deal is already fully paid.";
        if (deal.Status == DealStatus.Cancelled) return "This deal is cancelled.";
        if (deal.Status == DealStatus.Defaulted) return "A defaulted deal cannot accept payment confirmation.";
        if (deal.Status != DealStatus.Current || !deal.IsActive || !deal.IsConfirmed)
            return "The payment's deal is not active and confirmed.";
        if (deal.ParcelStatus is ParcelStatus.Sold or ParcelStatus.Leased)
            return $"Parcel is {deal.ParcelStatus}; this payment cannot be confirmed.";
        return null;
    }

    private static string? ReturnPaymentDisabledReason(DealListItem? deal, PaymentListItem? payment)
    {
        if (deal is null) return "Select a deal first.";
        if (payment is null) return "Select a payment first.";
        if (payment.DealId != deal.Id) return "Select a payment from this deal.";
        if (payment.Status == PaymentStatus.Confirmed) return "Confirmed payments cannot be returned here.";
        if (payment.Status == PaymentStatus.Returned) return "This payment was already returned.";
        if (payment.Status != PaymentStatus.AwaitingAdmin) return "Only a payment awaiting review can be returned.";
        return null;
    }
}

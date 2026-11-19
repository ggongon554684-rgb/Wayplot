using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Services.MainTransaction;

namespace WayPlot.Presenters;

public sealed record AdminReviewData(
    IReadOnlyList<(DealListItem Deal, DealSummaryDto Summary)> ClosedRequests,
    IReadOnlyList<ReviewPaymentListItem> PendingPayments,
    IReadOnlyList<PayoutListItem> UnpaidPayouts);

public sealed class AdminReviewPresenter(
    DealService deals,
    CommissionService commissions,
    IPipelineStageProvider pipelineStages)
{
    public async Task<ServiceResult<AdminReviewData>> LoadAsync()
    {
        try
        {
            await pipelineStages.EnsureLoadedAsync();
            var queue = await deals.GetAdminReviewQueueAsync();
            if (!queue.Ok || queue.Value is null)
                return ServiceResult<AdminReviewData>.Fail(queue.Message);
            var payoutResult = await commissions.ListAsync();
            if (!payoutResult.Ok || payoutResult.Value is null)
                return ServiceResult<AdminReviewData>.Fail(payoutResult.Message);

            var closed = new List<(DealListItem Deal, DealSummaryDto Summary)>();
            foreach (var deal in queue.Value.ClosedRequests)
            {
                var summary = await deals.GetDealSummaryAsync(deal.Id);
                if (!summary.Ok || summary.Value is null)
                    return ServiceResult<AdminReviewData>.Fail(summary.Message);
                closed.Add((deal, summary.Value));
            }

            var payouts = payoutResult.Value
                .Where(item => item.Status is CommissionStatus.Pending or CommissionStatus.Approved)
                .ToList();
            return ServiceResult<AdminReviewData>.Success(new AdminReviewData(
                closed, queue.Value.PaymentsAwaitingConfirmation, payouts));
        }
        catch (Exception)
        {
            return ServiceResult<AdminReviewData>.Fail(
                "The review queue could not be loaded. Check the connection and try again.");
        }
    }

    public async Task<ServiceResult<DealListItem>> ConfirmClosedAsync(Guid dealId, ParcelStatus status)
    {
        try { return await deals.ConfirmClosedAsync(dealId, status); }
        catch (Exception) { return ServiceResult<DealListItem>.Fail("The request could not be confirmed. Reload and try again."); }
    }

    public async Task<ServiceResult<DealListItem>> ReturnClosedAsync(Guid dealId)
    {
        try { return await deals.ReturnRequestAsync(dealId); }
        catch (Exception) { return ServiceResult<DealListItem>.Fail("The request could not be returned. Reload and try again."); }
    }

    public async Task<ServiceResult<PaymentListItem>> ConfirmPaymentAsync(Guid paymentId)
    {
        try { return await deals.ConfirmPaymentAsync(paymentId); }
        catch (Exception) { return ServiceResult<PaymentListItem>.Fail("The payment could not be confirmed. Reload and try again."); }
    }

    public async Task<ServiceResult<PaymentListItem>> ReturnPaymentAsync(Guid paymentId, string? note)
    {
        try { return await deals.ReturnPaymentAsync(paymentId, note); }
        catch (Exception) { return ServiceResult<PaymentListItem>.Fail("The payment could not be returned. Reload and try again."); }
    }

    public async Task<ServiceResult<bool>> ApproveAsync(PayoutListItem payout)
    {
        try { return await commissions.ApproveAsync(payout.Kind, payout.Id); }
        catch (Exception) { return ServiceResult<bool>.Fail("The payout could not be approved. Reload and try again."); }
    }

    public async Task<ServiceResult<bool>> MarkPaidAsync(PayoutListItem payout)
    {
        try { return await commissions.MarkPaidAsync(payout.Kind, payout.Id); }
        catch (Exception) { return ServiceResult<bool>.Fail("The payout could not be marked Paid. Reload and try again."); }
    }

    public async Task<ServiceResult<IReadOnlyList<DealPaymentDto>>> GetPaymentsAsync(Guid dealId)
    {
        try { return await deals.GetPaymentsAsync(dealId); }
        catch (Exception) { return ServiceResult<IReadOnlyList<DealPaymentDto>>.Fail("Payments could not be loaded."); }
    }

    public string StageDisplayName(LeadStage stage) => pipelineStages.GetDisplayName(stage);
}

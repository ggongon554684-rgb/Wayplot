using WayPlot.domain.Common;

namespace WayPlot.domain.Contracts;

public sealed record DealCandidateItem(
    Guid LeadId,
    string ContactName,
    string ParcelLabel,
    decimal? OfferAmount,
    decimal ParcelPrice,
    decimal ReservationFee);

public sealed record CreateDealRequest(
    Guid LeadId,
    decimal? Price,
    decimal Discount = 0,
    decimal? ReservationFee = null,
    decimal DownPayment = 0,
    bool IsInstallment = false,
    int InstallmentCount = 0,
    int DueDay = 1);

public sealed record DealListItem(
    Guid Id,
    Guid LeadId,
    string ContactName,
    string ParcelLabel,
    LeadStage LeadStage,
    bool CloseRequested,
    decimal Price,
    decimal Discount,
    decimal DownPayment,
    decimal ReservationFee,
    decimal ReservationFeeDue,
    decimal RemainingBalance,
    DealStatus Status,
    bool IsConfirmed,
    bool IsActive,
    DateTime CreatedAtUtc,
    ParcelStatus ParcelStatus,
    bool HasPaymentActivity,
    bool HasAwaitingPayment);

public sealed record DealTerminationSummary(
    decimal ConfirmedPaymentsTotal,
    int UnpaidPayoutCount,
    int PaidPayoutCount);

public sealed record RecordPaymentRequest(
    Guid DealId,
    decimal Amount,
    DateTime PaidAtUtc,
    PaymentMethod Method,
    string ReceiptNo);

public sealed record PaymentListItem(
    Guid Id,
    Guid DealId,
    DateTime PaidAtUtc,
    decimal Amount,
    PaymentMethod Method,
    string ReceiptNo,
    PaymentStatus Status,
    string? ReviewNote);

public sealed record ReviewPaymentListItem(
    PaymentListItem Payment,
    string ContactName,
    string ParcelLabel);

public sealed record AdminReviewQueue(
    IReadOnlyList<DealListItem> ClosedRequests,
    IReadOnlyList<ReviewPaymentListItem> PaymentsAwaitingConfirmation);

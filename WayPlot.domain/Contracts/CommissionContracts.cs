using WayPlot.domain.Common;

namespace WayPlot.domain.Contracts;

public sealed record CommissionRulesRequest(
    CommissionCalculationType CommissionType,
    decimal CommissionAmount,
    decimal AgentSharePercent,
    decimal CompanySharePercent,
    decimal BranchManagerSharePercent,
    CommissionCalculationType ReferralType,
    decimal ReferralAmount,
    CommissionPayableTiming PayableTiming);

public sealed record CommissionRulesVersion(
    int Version,
    DateTime ChangedAtUtc,
    string ChangedBy,
    CommissionCalculationType CommissionType,
    decimal CommissionAmount,
    decimal AgentSharePercent,
    decimal CompanySharePercent,
    decimal BranchManagerSharePercent,
    CommissionCalculationType ReferralType,
    decimal ReferralAmount,
    CommissionPayableTiming PayableTiming);

public sealed record CommissionRulesSnapshot(
    CommissionRulesVersion? Current,
    IReadOnlyList<CommissionRulesVersion> History);

public sealed record PayoutListItem(
    Guid Id,
    Guid DealId,
    PayoutKind Kind,
    string ContactName,
    string ParcelLabel,
    string RecipientName,
    string RecipientRole,
    decimal Amount,
    CommissionStatus Status,
    DateTime CreatedAtUtc,
    DateTime? PaidAtUtc,
    bool CanApprove,
    string? ApprovalBlockedReason,
    bool CanMarkPaid,
    Guid? BranchId);

public sealed record ReferralChoice(Guid Id, string Display)
{
    public override string ToString() => Display;
}

public sealed record ContactReferralOptions(
    IReadOnlyList<ReferralChoice> Contacts,
    IReadOnlyList<ReferralChoice> Staff);

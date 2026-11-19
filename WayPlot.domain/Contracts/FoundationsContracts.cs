using WayPlot.domain.Common;

namespace WayPlot.domain.Contracts;

public sealed record PersonListItem(
    Guid Id,
    PersonKind Kind,
    string FullName,
    string? Phone,
    string? Email,
    int ParcelCount,
    Guid? ContactId,
    Guid? OwnerId);

public sealed record PeopleFilter(
    IReadOnlySet<PersonKind> Kinds,
    string? SearchText = null,
    Guid? AssignedStaffId = null,
    int Limit = 200);

public sealed record PeopleListItemDto(
    PersonKind Kind,
    Guid Id,
    string Name,
    string? Phone,
    string? Email,
    string? AssignedAgentName,
    int OpenLeadCount,
    int DealCount,
    decimal? BalanceTotal,
    int ParcelCount,
    Guid? OwnerId = null,
    bool HasOverdueDeal = false);

public sealed record ContactLeadCardDto(
    Guid LeadId,
    Guid ParcelId,
    string ParcelLabel,
    LeadStage Stage,
    string StageName,
    string AgentName,
    DateTime LastActivityAtUtc);

public sealed record OwnerPeopleRecord(
    Guid Id,
    string Name,
    string Phone,
    string? Email,
    int ParcelCount);

public sealed record PipelineLeadCard(
    Guid Id,
    string ContactName,
    string ParcelLabel,
    LeadStage Stage,
    decimal? OfferAmount,
    DateTime LastActivityAtUtc,
    Guid? AssignedStaffId);

public sealed record PipelineStageColumn(
    LeadStage Stage,
    int Count,
    IReadOnlyList<PipelineLeadCard> Leads);

public sealed record PipelineBoard(IReadOnlyList<PipelineStageColumn> Stages);

public sealed record PipelineStageInfo(string Key, string Name, int SortOrder, bool IsSystem);

public interface IPipelineStageProvider
{
    Task EnsureLoadedAsync();
    IReadOnlyList<PipelineStageInfo> GetStages();
    IReadOnlyList<PipelineStageSetting> GetConfiguration();
    string GetDisplayName(LeadStage stage);
    LeadStage ClosingStage { get; }
    Task<IReadOnlyList<PipelineStageAdminInfo>> GetAllForAdminAsync();
    void Invalidate();
    void ClearCache();
}

public sealed class DefaultPipelineStageProvider : IPipelineStageProvider
{
    private readonly IReadOnlyList<PipelineStageSetting> _configuration = PipelineStageDefinition.Defaults;

    public Task EnsureLoadedAsync() => Task.CompletedTask;

    public IReadOnlyList<PipelineStageInfo> GetStages()
        => PipelineStageDefinition.GetStages(_configuration);

    public IReadOnlyList<PipelineStageSetting> GetConfiguration() => _configuration;

    public string GetDisplayName(LeadStage stage) =>
        PipelineStageDefinition.GetDisplayName(stage, _configuration);

    public LeadStage ClosingStage => PipelineStageDefinition.ClosingStage(_configuration);

    public Task<IReadOnlyList<PipelineStageAdminInfo>> GetAllForAdminAsync() =>
        Task.FromResult(PipelineStageDefinition.GetAllForAdmin(_configuration, new Dictionary<LeadStage, int>()));

    public void Invalidate() { }
    public void ClearCache() { }
}

public sealed record PipelineFilter(
    Guid? ParcelId = null,
    Guid? AssignedStaffId = null,
    string? SearchText = null,
    bool IncludeAllClosedAndLost = false);

public sealed record PipelineColumnDto(
    string StageKey,
    string StageName,
    int TotalCount,
    IReadOnlyList<LeadCardDto> Cards);

public sealed record PipelineBoardDto(IReadOnlyList<PipelineColumnDto> Columns);

public sealed record LeadCardDto(
    Guid LeadId,
    Guid ContactId,
    string ContactName,
    Guid ParcelId,
    string ParcelLabel,
    string? AgentName,
    LeadStage Stage,
    string StageName,
    int DaysInStage,
    DateTime LastActivityAt,
    bool HasPendingClosedRequest,
    DealStatus? DealStatus);

public sealed record LeadMoveOption(
    LeadStage ToStage,
    bool IsAllowed,
    string? BlockedReason,
    bool RequiresNote,
    bool RequiresLostReason);

public sealed record LeadMoveOptionDto(
    LeadStage TargetStage,
    bool Allowed,
    string? Reason,
    bool RequiresNote,
    bool OpensClosedRequest);

public enum TimelineItemKind
{
    StageChanged,
    Interaction,
    DealRequestCreated,
    DealRequestReturned,
    DealConfirmed,
    DealTerminated
}

public sealed record LeadTimelineItem(
    DateTime OccurredAtLocal,
    TimelineItemKind Kind,
    string Title,
    string? Detail,
    string? ByStaffName,
    DealType? DealType = null);

public enum DealTimelineKind { StageChange, Reassignment, Interaction, DealEvent }

public sealed record TimelineItemDto(
    DateTime When,
    DealTimelineKind Kind,
    string Title,
    string? Detail,
    string? ByStaffName);

public sealed record DealScheduleItem(
    int Sequence,
    string Description,
    DateTime DueAtLocal,
    decimal Amount,
    decimal PaidAmount,
    decimal RemainingAmount,
    bool IsOverdue);

public sealed record DealSummary(
    Guid Id,
    Guid LeadId,
    Guid ContactId,
    string ContactName,
    Guid ParcelId,
    string ParcelLabel,
    DealType? DealType,
    DealStatus Status,
    decimal Price,
    decimal Discount,
    decimal NetPrice,
    decimal ReservationFee,
    decimal DownPayment,
    bool IsInstallment,
    int InstallmentCount,
    decimal InstallmentAmount,
    decimal ConfirmedPaidTotal,
    decimal RemainingBalance,
    DateTime? NextDueAtLocal,
    decimal NextDueAmount,
    IReadOnlyList<DealScheduleItem> Schedule);

public sealed record DealSummaryDto(
    Guid DealId,
    Guid ContactId,
    Guid ParcelId,
    string ParcelLabel,
    DealType? DealType,
    DealStatus Status,
    decimal NetPrice,
    decimal ConfirmedPaid,
    decimal PendingPaymentsTotal,
    decimal Balance,
    decimal PercentPaid,
    DateTime? NextDueDate,
    decimal? NextDueAmount,
    decimal OverdueAmount,
    int DaysOverdue,
    int InstallmentsPaid,
    int InstallmentsTotal,
    decimal LatePenaltyPercentPerMonth,
    bool IsPendingConfirmation,
    string? AgentName,
    string? TerminationReason,
    DealMoneyOutcome? TerminationMoneyOutcome,
    decimal? RefundedAmount,
    DateTime? TerminatedAtUtc);

public sealed record DealPaymentDto(
    Guid Id,
    Guid DealId,
    DateTime PaidAtUtc,
    decimal Amount,
    PaymentMethod Method,
    string ReceiptNo,
    PaymentStatus Status,
    string? RecordedBy,
    string? ReviewNote);

public sealed record ReassignLeadRequest(Guid LeadId, Guid AssignedStaffId);
public sealed record ReassignContactRequest(Guid ContactId, Guid AssignedStaffId);
public sealed record ReassignAllRequest(Guid FromStaffId, Guid ToStaffId);

public sealed record ReassignmentSummary(
    int ContactsReassigned,
    int LeadsReassigned,
    int ParcelsReassigned);

public sealed record StaffListItem(
    Guid Id,
    string Username,
    string DisplayName,
    string Role,
    Guid? BranchId,
    bool IsActive,
    int OpenLeadCount,
    int ParcelCount,
    int AvailableParcelCount = 0,
    int Version = 1)
{
    public override string ToString() => DisplayName;
}

public sealed record BranchListItem(Guid Id, string Name)
{
    public override string ToString() => Name;
}

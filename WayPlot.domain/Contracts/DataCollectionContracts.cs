using WayPlot.domain.Common;

namespace WayPlot.domain.Contracts;

// ---- Contacts ----
public sealed record CaptureContactRequest(
    string FullName,
    string? Phone,
    string? Email,
    LeadSource Source,
    ContactType Type,
    bool ConsentSms,
    bool ConsentEmail,
    string? InquiryNote = null,
    Guid? ParcelId = null,
    Guid? AssignedStaffId = null,
    Guid? ReferrerContactId = null,
    Guid? ReferrerStaffId = null,
    string? ReferrerName = null,
    string? ReferrerPhone = null);

/// <summary>MatchedExisting: a contact with the same phone or email already existed, so no duplicate was created.
/// InquiryRecorded: the inquiry was written to that contact's history.</summary>
public sealed record CaptureContactResult(Guid ContactId, string FullName, bool MatchedExisting, bool InquiryRecorded);

public sealed record ImportCsvColumn(int Index, string Header);

public sealed record ContactImportMapping(
    int NameColumn,
    int? PhoneColumn,
    int? EmailColumn,
    int SourceColumn,
    int? ConsentColumn,
    int? ParcelColumn,
    int? AssignedAgentColumn);

public enum ContactImportRowStatus { Valid, Duplicate, Invalid }

public sealed record ContactImportRow(
    int RowNumber,
    ContactImportRowStatus Status,
    string Name,
    string? Phone,
    string? Email,
    string Source,
    string? Parcel,
    string? Reason,
    int? DuplicateOfRow,
    bool WillCreateLead,
    bool LeadCreated,
    string? Warning,
    IReadOnlyList<string> Fields);

public sealed record ContactImportPreview(
    IReadOnlyList<ImportCsvColumn> Columns,
    IReadOnlyList<ContactImportRow> Rows);

public sealed record ContactImportSummary(
    int ContactsCreated,
    int LeadsCreated,
    int Duplicates,
    int Rejected,
    int Warnings,
    IReadOnlyList<ImportCsvColumn> Columns,
    IReadOnlyList<ContactImportRow> Rows);

// ---- Owners and parcels ----
public sealed record CreateOwnerRequest(string Name, string? Phone, string? Email, bool ListingAgreementOnFile);

public sealed record OwnerDto(Guid Id, string Name, string Phone, string? Email, bool ListingAgreementOnFile);

public sealed record CreateParcelRequest(
    string Label,
    string Description,
    decimal AreaSqm,
    string Zoning,
    decimal Price,
    decimal ReservationFee,
    string? GeoJson,
    double? Latitude,
    double? Longitude,
    string TaxDeclarationNo,
    string TitleNo,
    string Utilities,
    string RoadAccess,
    Guid OwnerId,
    ParcelStatus Status = ParcelStatus.Available,
    Guid? AssignedStaffId = null);

public sealed record UpdateParcelRequest(
    Guid ParcelId,
    string Label,
    string Description,
    decimal AreaSqm,
    string Zoning,
    string? GeoJson,
    double? Latitude,
    double? Longitude,
    string TaxDeclarationNo,
    string TitleNo,
    string Utilities,
    string RoadAccess,
    Guid OwnerId);

public sealed record ChangeParcelPriceRequest(Guid ParcelId, decimal Price, decimal ReservationFee);

public sealed record ReassignParcelRequest(Guid ParcelId, Guid AssignedStaffId);

public sealed record ParcelAgentOption(Guid Id, string FullName)
{
    public override string ToString() => FullName;
}

/// <summary>PricePerSqm and ReservationPercent are computed by ParcelService, never by a screen.</summary>
public sealed record ParcelDto(
    Guid Id,
    string Label,
    string Description,
    decimal AreaSqm,
    string Zoning,
    decimal Price,
    decimal PricePerSqm,
    decimal ReservationFee,
    decimal ReservationPercent,
    ParcelStatus Status,
    string? GeoJson,
    double? Latitude,
    double? Longitude,
    string TaxDeclarationNo,
    string TitleNo,
    string Utilities,
    string RoadAccess,
    Guid OwnerId,
    string OwnerName,
    string OwnerPhone = "",
    string? OwnerEmail = null,
    bool OwnerListingAgreementOnFile = false,
    Guid? AssignedStaffId = null);

public static class ParcelPhotoLimits
{
    public const int MaxPhotosPerParcel = 10;
    public const long MaxSourceFileBytes = 15L * 1024 * 1024;
    public const int MaxProcessedBytes = 2 * 1024 * 1024;
    public static IReadOnlyList<string> SupportedExtensions { get; } =
        Array.AsReadOnly(new[] { ".jpg", ".jpeg", ".png" });

    public static bool SupportsFileName(string fileName) =>
        SupportedExtensions.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase);
}

public sealed record ParcelPhotoThumbnailDto(Guid Id, string FileName, int SizeBytes, byte[] ThumbnailBytes);

public sealed record ParcelPhotoImageDto(string FileName, string ContentType, byte[] ImageBytes);

// ---- Leads ----
public sealed record AttachLeadRequest(Guid ContactId, Guid ParcelId, Guid? AssignedStaffId = null);

public sealed record MoveStageRequest(
    Guid LeadId,
    LeadStage ToStage,
    string? Note = null,
    string? LostReason = null,
    decimal? OfferAmount = null);

public sealed record LeadDto(
    Guid Id,
    Guid ContactId,
    string ContactName,
    string? ContactPhone,
    string? ContactEmail,
    LeadSource Source,
    bool ConsentSms,
    bool ConsentEmail,
    Guid ParcelId,
    string ParcelLabel,
    LeadStage Stage,
    string StageName,
    bool CloseRequested,
    decimal? OfferAmount,
    string? LostReason,
    DateTime LastActivityAtUtc);

public sealed record LeadListItem(
    Guid Id,
    string ContactName,
    string ParcelLabel,
    LeadStage Stage,
    string StageName,
    decimal? OfferAmount,
    DateTime LastActivityAtUtc);

public sealed record LeadFileDto(
    Guid Id,
    string ContactName,
    string? ContactPhone,
    string? ContactEmail,
    string ParcelLabel,
    LeadStage Stage,
    string StageName,
    decimal? OfferAmount,
    IReadOnlyList<PipelineStageInfo> Stages,
    IReadOnlyList<LeadStageHistoryRow> History,
    IReadOnlyList<LeadInteractionRow> Interactions);

public sealed record LeadStageHistoryRow(
    LeadStage? FromStage,
    LeadStage ToStage,
    DateTime ChangedAtUtc,
    string? Note,
    string? FromStageName,
    string ToStageName);

public sealed record LeadInteractionRow(
    InteractionType Type,
    InteractionChannel Channel,
    DateTime OccurredAtUtc,
    string Note);

public sealed record LogInteractionRequest(
    Guid LeadId,
    string Kind,
    InteractionChannel Channel,
    DateTime OccurredAtUtc,
    string Note,
    int? Rating);

public sealed record InteractionListItem(
    Guid Id,
    Guid LeadId,
    DateTime OccurredAtUtc,
    string ContactName,
    string ParcelLabel,
    InteractionType Type,
    InteractionChannel Channel,
    string Note,
    int? Rating,
    ComplaintStatus? ComplaintStatus);

public static class InteractionLogKinds
{
    public const string Call = "Call";
    public const string Text = "Text";
    public const string Viewing = "Viewing";
    public const string Note = "Note";
    public const string Email = "Email";
    public const string Complaint = "Complaint";
    public const string Feedback = "Feedback";

    public static IReadOnlyList<string> All { get; } =
        [Call, Text, Viewing, Email, Note, Complaint, Feedback];
}
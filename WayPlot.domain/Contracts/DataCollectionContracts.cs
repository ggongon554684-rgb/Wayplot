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
    string? InquiryNote = null);

/// <summary>MatchedExisting: a contact with the same phone or email already existed, so no duplicate was created.
/// InquiryRecorded: the inquiry was written to that contact's history.</summary>
public sealed record CaptureContactResult(Guid ContactId, string FullName, bool MatchedExisting, bool InquiryRecorded);

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
    ParcelStatus Status = ParcelStatus.Available);

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
    string OwnerName);

// ---- Leads ----
public sealed record AttachLeadRequest(Guid ContactId, Guid ParcelId);

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
    bool CloseRequested,
    decimal? OfferAmount,
    string? LostReason,
    DateTime LastActivityAtUtc);

public sealed record LeadListItem(
    Guid Id,
    string ContactName,
    string ParcelLabel,
    LeadStage Stage,
    decimal? OfferAmount,
    DateTime LastActivityAtUtc);

public sealed record LeadFileDto(
    Guid Id,
    string ContactName,
    string? ContactPhone,
    string? ContactEmail,
    string ParcelLabel,
    LeadStage Stage,
    decimal? OfferAmount,
    IReadOnlyList<LeadStageHistoryRow> History,
    IReadOnlyList<LeadInteractionRow> Interactions);

public sealed record LeadStageHistoryRow(
    LeadStage? FromStage,
    LeadStage ToStage,
    DateTime ChangedAtUtc,
    string? Note);

public sealed record LeadInteractionRow(
    InteractionType Type,
    InteractionChannel Channel,
    DateTime OccurredAtUtc,
    string Note);
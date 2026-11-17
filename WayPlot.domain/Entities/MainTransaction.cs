using WayPlot.domain.Common;

namespace WayPlot.domain.Entities;

public class Lead : BaseEntity
{
    public Guid ContactId { get; set; }
    public Contact Contact { get; set; } = null!;
    public Guid ParcelId { get; set; }
    public Parcel Parcel { get; set; } = null!;
    public LeadStage Stage { get; set; } = LeadStage.New;
    /// <summary>True when the agent asked for Closed and the Tenant Admin has not confirmed yet.</summary>
    public bool CloseRequested { get; set; }
    public decimal? OfferAmount { get; set; }
    public string? LostReason { get; set; }
    public DateTime? AppointmentAtUtc { get; set; }
    public DateTime LastActivityAtUtc { get; set; }
    public Guid? AssignedStaffId { get; set; }
}

public class Interaction : BaseEntity
{
    public Guid LeadId { get; set; }
    public Lead Lead { get; set; } = null!;
    public InteractionType Type { get; set; }
    public InteractionChannel Channel { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string Note { get; set; } = string.Empty;
    /// <summary>1 to 5, Feedback only.</summary>
    public int? Rating { get; set; }
    /// <summary>Complaint only.</summary>
    public ComplaintStatus? ComplaintStatus { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public Guid? LoggedByStaffId { get; set; }
}

/// <summary>Every pipeline move is recorded. Tier B funnel, conversion and days-to-close are computed from this.</summary>
public class StageHistory : BaseEntity
{
    public Guid LeadId { get; set; }
    public Lead Lead { get; set; } = null!;
    public LeadStage? FromStage { get; set; }
    public LeadStage ToStage { get; set; }
    public DateTime ChangedAtUtc { get; set; }
    public Guid? ChangedByStaffId { get; set; }
    public string? Note { get; set; }
}

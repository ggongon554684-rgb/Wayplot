namespace WayPlot.domain.Common;

/// <summary>
/// Every syncable tenant table inherits this. These columns are cheap now and expensive to add later:
/// GUID key (offline-safe), audit fields, Version (conflict checks), SyncStatus (backup queue),
/// IsDeleted (soft delete keeps history for reports) and BranchId (used from Tier C).
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? BranchId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public SyncStatus SyncStatus { get; set; } = SyncStatus.Pending;
    public bool IsDeleted { get; set; }
}

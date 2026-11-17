using WayPlot.domain.Common;

namespace WayPlot.domain.Entities;

/// <summary>Staff row inside a tenant database. UserId is the login in the Master database.</summary>
public class Staff : BaseEntity
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = Roles.Agent;
    public bool IsActive { get; set; } = true;
}

public class Branch : BaseEntity
{
    public string Name { get; set; } = string.Empty;
}

/// <summary>Per-tenant rules as key/value: scoring points, reminder periods, commission rules. Tier B adds rows, not columns.</summary>
public class Setting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

/// <summary>Queue of local changes waiting for the cloud backup. Not syncable itself.</summary>
public class OutboxEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Retrying the same ChangeId must never create a duplicate in the cloud.</summary>
    public Guid ChangeId { get; set; } = Guid.NewGuid();
    public string TableName { get; set; } = string.Empty;
    public Guid RecordId { get; set; }
    public string Operation { get; set; } = "Upsert";
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public int Attempts { get; set; }
}

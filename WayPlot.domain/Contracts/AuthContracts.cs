using WayPlot.domain.Common;

namespace WayPlot.domain.Contracts;

public sealed record LoginRequest(string Username, string Password);

/// <summary>What the desktop app learns at login. It never receives the cloud connection string.</summary>
public sealed record LoginResult(
    Guid UserId,
    string Username,
    string DisplayName,
    string Role,
    Guid? TenantId,
    string? TenantName,
    Tier? Tier,
    string? DatabaseName,
    bool ReadOnly);

namespace WayPlot.domain.Contracts;

public sealed record CompanyProfileRequest(
    string Name,
    string ContactNumbers,
    string Email,
    string Address,
    int ExpectedVersion);

public sealed record CompanyProfileVersion(
    int Version,
    DateTime ChangedAtUtc,
    string ChangedBy,
    string Name,
    string ContactNumbers,
    string Email,
    string Address);

public sealed record CompanyProfileSnapshot(CompanyProfileVersion? Current);

using WayPlot.domain.Common;
using WayPlot.domain.Entities;

namespace WayPlot.domain.Contracts;

/// <summary>One row of the Contacts list. A plain copy of the contact, so screens never touch the entity.</summary>
public sealed record ContactListItem(
    Guid Id,
    string FullName,
    string? Phone,
    string? Email,
    LeadSource Source,
    ContactType Type,
    bool ConsentSms,
    bool ConsentEmail);
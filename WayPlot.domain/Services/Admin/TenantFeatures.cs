using WayPlot.domain.Common;
using WayPlot.domain.Interfaces;

namespace WayPlot.domain.Services.Admin;

/// <summary>Plan tiers switch features on. They never change the database schema.</summary>
public sealed class TenantFeatures(Tier tier) : ITenantFeatures
{
    public bool DataCollection => true;
    public bool MainTransaction => true;
    public bool DealsPayments => true;
    public bool BusinessIntelligence => tier >= Tier.B;
    public bool Actions => tier >= Tier.B;
    public bool Branching => tier >= Tier.C;
}

using Microsoft.Data.SqlClient;
using WayPlot.domain.Master;

namespace WayPlot.infrastructure.Tenancy;

public static class TenantConnection
{
    /// <summary>Local development connection (Windows authentication). A cloud host would use SQL authentication.</summary>
    public static string For(Tenant tenant) => new SqlConnectionStringBuilder
    {
        DataSource = tenant.ServerName,
        InitialCatalog = tenant.DatabaseName,
        IntegratedSecurity = true,
        TrustServerCertificate = true
    }.ConnectionString;
}

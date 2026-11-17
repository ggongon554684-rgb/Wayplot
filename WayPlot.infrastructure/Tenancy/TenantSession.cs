using Microsoft.Data.SqlClient;

namespace WayPlot.infrastructure.Tenancy;

/// <summary>The connection string for the signed-in tenant. Built from the server name in the app's appsettings.json
/// and the database name the login returned. The desktop app never receives a connection string from the API.</summary>
public sealed class TenantSession(string serverName)
{
    private string? _connectionString;

    public string ServerName { get; } = string.IsNullOrWhiteSpace(serverName)
        ? throw new ArgumentException("Tenancy:ServerName is missing in appsettings.json.", nameof(serverName))
        : serverName;

    public string? DatabaseName { get; private set; }
    public bool IsStarted => _connectionString is not null;

    public string ConnectionString => _connectionString
        ?? throw new InvalidOperationException("No tenant database is open. Sign in first.");

    public void Start(string databaseName)
    {
        DatabaseName = databaseName;
        // Same settings as TenantConnection (local development, Windows authentication).
        _connectionString = new SqlConnectionStringBuilder
        {
            DataSource = ServerName,
            InitialCatalog = databaseName,
            IntegratedSecurity = true,
            TrustServerCertificate = true
        }.ConnectionString;
    }

    public void Clear()
    {
        DatabaseName = null;
        _connectionString = null;
    }
}
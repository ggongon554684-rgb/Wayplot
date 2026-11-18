using Microsoft.Extensions.DependencyInjection;
using WayPlot.infrastructure;
using WayPlot.infrastructure.Data.Seed;
using WayPlot.infrastructure.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddWayPlotInfrastructure(builder.Configuration.GetConnectionString("Master")!);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Development only: create Master and seed demo logins.
    var server = builder.Configuration["Tenancy:ServerName"] ?? "(localdb)\\MSSQLLocalDB";
    await MasterSeeder.SeedAsync(app.Services, server);
}

// Every startup: migrate, provision staff, and ensure Tier C branches for every non-suspended tenant.
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<TenantMigrator>().MigrateAllAsync();

app.MapControllers();
app.Run();

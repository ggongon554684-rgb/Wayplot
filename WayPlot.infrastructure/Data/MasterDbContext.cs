using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WayPlot.domain.Common;
using WayPlot.domain.Master;
using WayPlot.infrastructure.Identity;

namespace WayPlot.infrastructure.Data;

/// <summary>The Master database: logins, tenants, plans and subscriptions. Platform staff see only this, never tenant data.</summary>
public class MasterDbContext(DbContextOptions<MasterDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    protected override void ConfigureConventions(ModelConfigurationBuilder c)
    {
        c.Properties<decimal>().HavePrecision(18, 2);
        c.Properties<Tier>().HaveConversion<string>().HaveMaxLength(10);
        c.Properties<TenantStatus>().HaveConversion<string>().HaveMaxLength(30);
        c.Properties<SubscriptionStatus>().HaveConversion<string>().HaveMaxLength(30);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Plan>(e => e.Property(p => p.Name).HasMaxLength(50).IsRequired());

        b.Entity<Tenant>(e =>
        {
            e.Property(t => t.Name).HasMaxLength(120).IsRequired();
            e.Property(t => t.ServerName).HasMaxLength(200).IsRequired();
            e.Property(t => t.DatabaseName).HasMaxLength(128).IsRequired();
            e.HasIndex(t => t.DatabaseName).IsUnique();
            e.HasOne(t => t.Plan).WithMany().HasForeignKey(t => t.PlanId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Subscription>(e =>
        {
            e.Property(s => s.PaymentReference).HasMaxLength(100);
            e.HasOne(s => s.Tenant).WithMany().HasForeignKey(s => s.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.Plan).WithMany().HasForeignKey(s => s.PlanId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<AppUser>(e =>
        {
            e.Property(u => u.DisplayName).HasMaxLength(120);
            e.HasIndex(u => u.TenantId);
            e.HasOne<Tenant>().WithMany().HasForeignKey(u => u.TenantId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

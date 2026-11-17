using Microsoft.EntityFrameworkCore;
using WayPlot.domain.Common;
using WayPlot.domain.Entities;
using WayPlot.domain.Interfaces;

namespace WayPlot.infrastructure.Data;

/// <summary>
/// The one shared tenant schema. Every tenant database has these same tables.
/// Create it through ITenantDbContextFactory with that tenant's connection string.
/// </summary>
public class TenantDbContext : DbContext
{
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public TenantDbContext(DbContextOptions<TenantDbContext> options, ICurrentUser user, IClock clock) : base(options)
    {
        _user = user;
        _clock = clock;
    }

    /// <summary>Set true while the backup service marks rows Synced, so audit fields and Version are left alone.</summary>
    public bool SuppressAudit { get; set; }

    public DbSet<Staff> Staffs => Set<Staff>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<OutboxEntry> Outbox => Set<OutboxEntry>();
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Parcel> Parcels => Set<Parcel>();
    public DbSet<ParcelPhoto> ParcelPhotos => Set<ParcelPhoto>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Interaction> Interactions => Set<Interaction>();
    public DbSet<StageHistory> StageHistories => Set<StageHistory>();
    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Commission> Commissions => Set<Commission>();
    public DbSet<ReferralBonus> ReferralBonuses => Set<ReferralBonus>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<LandownerPayout> LandownerPayouts => Set<LandownerPayout>();

    protected override void ConfigureConventions(ModelConfigurationBuilder c)
    {
        c.Properties<decimal>().HavePrecision(18, 2);
        // Enums are stored as text so rows are readable in SSMS.
        c.Properties<SyncStatus>().HaveConversion<string>().HaveMaxLength(30);
        c.Properties<ParcelStatus>().HaveConversion<string>().HaveMaxLength(30);
        c.Properties<ContactType>().HaveConversion<string>().HaveMaxLength(30);
        c.Properties<LeadSource>().HaveConversion<string>().HaveMaxLength(30);
        c.Properties<LeadStage>().HaveConversion<string>().HaveMaxLength(30);
        c.Properties<InteractionType>().HaveConversion<string>().HaveMaxLength(30);
        c.Properties<InteractionChannel>().HaveConversion<string>().HaveMaxLength(30);
        c.Properties<ComplaintStatus>().HaveConversion<string>().HaveMaxLength(30);
        c.Properties<DealStatus>().HaveConversion<string>().HaveMaxLength(30);
        c.Properties<PaymentStatus>().HaveConversion<string>().HaveMaxLength(30);
        c.Properties<PaymentMethod>().HaveConversion<string>().HaveMaxLength(30);
        c.Properties<CommissionStatus>().HaveConversion<string>().HaveMaxLength(30);
        c.Properties<DocumentType>().HaveConversion<string>().HaveMaxLength(30);
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Soft-delete filter and the Version concurrency token on every syncable table.
        Base<Staff>(b); Base<Setting>(b); Base<Owner>(b); Base<Parcel>(b); Base<ParcelPhoto>(b);
        Base<Contact>(b); Base<Lead>(b); Base<Interaction>(b); Base<StageHistory>(b); Base<Deal>(b);
        Base<Payment>(b); Base<Commission>(b); Base<ReferralBonus>(b); Base<Document>(b); Base<LandownerPayout>(b);

        b.Entity<Staff>(e =>
        {
            e.Property(s => s.Username).HasMaxLength(100).IsRequired();
            e.HasIndex(s => s.UserId).IsUnique();
        });

        b.Entity<Setting>(e =>
        {
            e.Property(s => s.Key).HasMaxLength(100).IsRequired();
            e.HasIndex(s => s.Key).IsUnique();
        });

        b.Entity<Parcel>(e =>
        {
            e.Property(p => p.Label).HasMaxLength(60).IsRequired();
            e.HasIndex(p => p.Label).IsUnique().HasFilter("[IsDeleted] = 0").HasDatabaseName("UX_Parcel_Label");
        });

        b.Entity<Contact>(e =>
        {
            e.Property(c => c.FullName).HasMaxLength(150).IsRequired();
            e.Property(c => c.PhoneNormalized).HasMaxLength(11);
            e.Property(c => c.Email).HasMaxLength(200);
            // The database itself refuses a second contact with the same phone.
            e.HasIndex(c => c.PhoneNormalized).IsUnique()
                .HasFilter("[PhoneNormalized] IS NOT NULL AND [IsDeleted] = 0")
                .HasDatabaseName("UX_Contact_Phone");
            e.HasIndex(c => c.Email);
        });

        b.Entity<Lead>(e =>
            // Race guard: only one lead per parcel may be in Negotiation.
            e.HasIndex(l => l.ParcelId).IsUnique()
                .HasFilter("[Stage] = 'Negotiation' AND [IsDeleted] = 0")
                .HasDatabaseName("UX_Lead_OneNegotiationPerParcel"));

        b.Entity<Deal>(e =>
            // Race guard: only one active deal per parcel, so a lot cannot be sold twice.
            e.HasIndex(d => d.ParcelId).IsUnique()
                .HasFilter("[IsActive] = 1")
                .HasDatabaseName("UX_Deal_OneActivePerParcel"));

        b.Entity<Payment>(e =>
        {
            e.Property(p => p.ReceiptNo).HasMaxLength(50).IsRequired();
            e.HasIndex(p => p.ReceiptNo).IsUnique().HasFilter("[IsDeleted] = 0").HasDatabaseName("UX_Payment_Receipt");
        });

        b.Entity<OutboxEntry>(e =>
        {
            e.Property(o => o.TableName).HasMaxLength(100).IsRequired();
            e.HasIndex(o => o.CreatedAtUtc);
        });

        // History is kept, so no cascading deletes anywhere.
        foreach (var fk in b.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }

    private static void Base<T>(ModelBuilder b) where T : BaseEntity
    {
        var e = b.Entity<T>();
        e.HasQueryFilter(x => !x.IsDeleted);
        e.Property(x => x.Version).IsConcurrencyToken();
        e.Property(x => x.CreatedBy).HasMaxLength(100);
        e.Property(x => x.UpdatedBy).HasMaxLength(100);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        Stamp();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        Stamp();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>Fills audit fields, bumps Version, marks rows Pending for backup, and turns deletes into soft deletes.</summary>
    private void Stamp()
    {
        if (SuppressAudit) return;

        var now = _clock.UtcNow;
        var who = _user.UserName;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>().ToList())
        {
            var e = entry.Entity;
            switch (entry.State)
            {
                case EntityState.Added:
                    e.CreatedAtUtc = now;
                    e.CreatedBy = who;
                    e.UpdatedAtUtc = now;
                    e.UpdatedBy = who;
                    e.SyncStatus = SyncStatus.Pending;
                    e.BranchId ??= _user.BranchId;
                    break;

                case EntityState.Modified:
                    e.UpdatedAtUtc = now;
                    e.UpdatedBy = who;
                    e.Version++;
                    e.SyncStatus = SyncStatus.Pending;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    e.IsDeleted = true;
                    e.UpdatedAtUtc = now;
                    e.UpdatedBy = who;
                    e.Version++;
                    e.SyncStatus = SyncStatus.Pending;
                    break;
            }
        }
    }
}

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
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<OutboxEntry> Outbox => Set<OutboxEntry>();
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Parcel> Parcels => Set<Parcel>();
    public DbSet<ParcelPhoto> ParcelPhotos => Set<ParcelPhoto>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<DuplicateInquiry> DuplicateInquiries => Set<DuplicateInquiry>();
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
        Base<Staff>(b); Base<Branch>(b); Base<Setting>(b); Base<Owner>(b); Base<Parcel>(b); Base<ParcelPhoto>(b);
        Base<Contact>(b); Base<DuplicateInquiry>(b); Base<Lead>(b); Base<Interaction>(b); Base<StageHistory>(b); Base<Deal>(b);
        Base<Payment>(b); Base<Commission>(b); Base<ReferralBonus>(b); Base<Document>(b); Base<LandownerPayout>(b);

        b.Entity<Staff>(e =>
        {
            e.Property(s => s.Username).HasMaxLength(100).IsRequired();
            e.HasIndex(s => s.UserId).IsUnique();
            e.HasOne<Branch>().WithMany().HasForeignKey(s => s.BranchId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Branch>(e =>
        {
            e.Property(branch => branch.Name).HasMaxLength(120).IsRequired();
            e.HasIndex(branch => branch.Name).IsUnique().HasFilter("[IsDeleted] = 0");
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
            e.HasIndex(p => p.AssignedStaffId);
            e.HasOne<Staff>().WithMany().HasForeignKey(p => p.AssignedStaffId).OnDelete(DeleteBehavior.Restrict);
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
            e.HasIndex(c => c.AssignedStaffId);
            e.HasOne<Staff>().WithMany().HasForeignKey(c => c.AssignedStaffId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Staff>().WithMany().HasForeignKey(c => c.ReferrerStaffId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<DuplicateInquiry>(e =>
        {
            e.Property(inquiry => inquiry.Details).IsRequired();
            e.HasIndex(inquiry => new { inquiry.ContactId, inquiry.OccurredAtUtc });
            e.HasOne(inquiry => inquiry.Contact).WithMany().HasForeignKey(inquiry => inquiry.ContactId);
            e.HasOne(inquiry => inquiry.Parcel).WithMany().HasForeignKey(inquiry => inquiry.ParcelId);
            e.HasOne<Staff>().WithMany().HasForeignKey(inquiry => inquiry.RecordedByStaffId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Lead>(e =>
        {
            e.HasIndex(l => l.AssignedStaffId);
            e.HasIndex(l => l.ParcelId);
            e.HasIndex(l => new { l.Id, l.ContactId, l.ParcelId }).IsUnique()
                .HasDatabaseName("UX_Lead_Id_ContactId_ParcelId");
            e.HasOne<Staff>().WithMany().HasForeignKey(l => l.AssignedStaffId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Deal>(e =>
        {
            // Deal contact and parcel are immutable snapshots of the lead at deal creation.
            e.HasOne(d => d.Lead).WithMany()
                .HasForeignKey(d => new { d.LeadId, d.ContactId, d.ParcelId })
                .HasPrincipalKey(l => new { l.Id, l.ContactId, l.ParcelId })
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(d => d.AgentStaffId);
            e.HasIndex(d => d.ParcelId).IsUnique()
                .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0")
                .HasDatabaseName("UX_Deal_OneActivePerParcel");
            e.HasOne<Staff>().WithMany().HasForeignKey(d => d.AgentStaffId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Staff>().WithMany().HasForeignKey(d => d.ConfirmedByStaffId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Payment>(e =>
        {
            e.Property(p => p.ReceiptNo).HasMaxLength(50).IsRequired();
            e.HasIndex(p => p.ReceiptNo).IsUnique().HasFilter("[IsDeleted] = 0").HasDatabaseName("UX_Payment_Receipt");
            e.HasOne<Staff>().WithMany().HasForeignKey(p => p.RecordedByStaffId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Staff>().WithMany().HasForeignKey(p => p.ConfirmedByStaffId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Commission>(e =>
        {
            e.HasIndex(commission => commission.RecipientStaffId);
            e.HasOne<Staff>().WithMany().HasForeignKey(commission => commission.RecipientStaffId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ReferralBonus>(e =>
            e.HasOne<Staff>().WithMany().HasForeignKey(bonus => bonus.ReferrerStaffId).OnDelete(DeleteBehavior.Restrict));

        b.Entity<Document>(e =>
            e.HasOne<Staff>().WithMany().HasForeignKey(document => document.UploadedByStaffId).OnDelete(DeleteBehavior.Restrict));

        b.Entity<Interaction>(e =>
            e.HasOne<Staff>().WithMany().HasForeignKey(interaction => interaction.LoggedByStaffId).OnDelete(DeleteBehavior.Restrict));

        b.Entity<StageHistory>(e =>
            e.HasOne<Staff>().WithMany().HasForeignKey(history => history.ChangedByStaffId).OnDelete(DeleteBehavior.Restrict));

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

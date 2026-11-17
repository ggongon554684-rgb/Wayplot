using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WayPlot.domain.Common;
using WayPlot.domain.Repositories;
using WayPlot.infrastructure.Data;
using WayPlot.infrastructure.Tenancy;

namespace WayPlot.infrastructure.Repositories;

/// <summary>Wraps ONE TenantDbContext. Disposing the unit of work disposes the context.</summary>
internal sealed class TenantUnitOfWork : ITenantUnitOfWork
{
    private readonly TenantDbContext _db;

    public TenantUnitOfWork(TenantDbContext db)
    {
        _db = db;
        Owners = new OwnerRepository(db);
        Parcels = new ParcelRepository(db);
        Contacts = new ContactRepository(db);
        Staff = new StaffRepository(db);
        Settings = new SettingRepository(db);
        Leads = new LeadRepository(db);
        Interactions = new InteractionRepository(db);
        StageHistories = new StageHistoryRepository(db);
        DuplicateInquiries = new DuplicateInquiryRepository(db);
        Deals = new DealRepository(db);
        Payments = new PaymentRepository(db);
        Commissions = new CommissionRepository(db);
        ReferralBonuses = new ReferralBonusRepository(db);
    }

    public IOwnerRepository Owners { get; }
    public IParcelRepository Parcels { get; }
    public IContactRepository Contacts { get; }
    public IStaffRepository Staff { get; }
    public ISettingRepository Settings { get; }
    public ILeadRepository Leads { get; }
    public IInteractionRepository Interactions { get; }
    public IStageHistoryRepository StageHistories { get; }
    public IDuplicateInquiryRepository DuplicateInquiries { get; }
    public IDealRepository Deals { get; }
    public IPaymentRepository Payments { get; }
    public ICommissionRepository Commissions { get; }
    public IReferralBonusRepository ReferralBonuses { get; }

    public async Task<int> SaveAsync()
    {
        try
        {
            return await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException sql
            && sql.Number is 2601 or 2627
            && sql.Message.Contains("UX_Payment_Receipt", StringComparison.Ordinal))
        {
            throw new ReceiptNumberConflictException(ex);
        }
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();
}

/// <summary>Every Create() opens a fresh context on the signed-in tenant's database. Nothing is kept between operations.</summary>
internal sealed class TenantUnitOfWorkFactory(ITenantDbContextFactory dbFactory, TenantSession session)
    : ITenantUnitOfWorkFactory
{
    public ITenantUnitOfWork Create() => new TenantUnitOfWork(dbFactory.Create(session.ConnectionString));
}
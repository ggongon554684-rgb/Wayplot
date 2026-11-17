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
        Leads = new LeadRepository(db);
        Interactions = new InteractionRepository(db);
        StageHistories = new StageHistoryRepository(db);
    }

    public IOwnerRepository Owners { get; }
    public IParcelRepository Parcels { get; }
    public IContactRepository Contacts { get; }
    public ILeadRepository Leads { get; }
    public IInteractionRepository Interactions { get; }
    public IStageHistoryRepository StageHistories { get; }

    public Task<int> SaveAsync() => _db.SaveChangesAsync();

    public ValueTask DisposeAsync() => _db.DisposeAsync();
}

/// <summary>Every Create() opens a fresh context on the signed-in tenant's database. Nothing is kept between operations.</summary>
internal sealed class TenantUnitOfWorkFactory(ITenantDbContextFactory dbFactory, TenantSession session)
    : ITenantUnitOfWorkFactory
{
    public ITenantUnitOfWork Create() => new TenantUnitOfWork(dbFactory.Create(session.ConnectionString));
}
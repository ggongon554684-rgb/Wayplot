namespace WayPlot.domain.Repositories;

/// <summary>One short-lived unit of work per operation. Dispose it when the operation ends.</summary>
public interface ITenantUnitOfWork : IAsyncDisposable
{
    IOwnerRepository Owners { get; }
    IParcelRepository Parcels { get; }
    IContactRepository Contacts { get; }
    ILeadRepository Leads { get; }
    IInteractionRepository Interactions { get; }
    IStageHistoryRepository StageHistories { get; }

    /// <summary>Saves everything changed in this unit of work. Returns the number of rows written.</summary>
    Task<int> SaveAsync();
}

/// <summary>Creates a unit of work for the signed-in tenant. Services call Create() per operation and never keep the result.</summary>
public interface ITenantUnitOfWorkFactory
{
    ITenantUnitOfWork Create();
}
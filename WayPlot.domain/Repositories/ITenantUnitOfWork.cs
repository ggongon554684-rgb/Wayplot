using System.Data;

namespace WayPlot.domain.Repositories;

/// <summary>One short-lived unit of work per operation. Dispose it when the operation ends.</summary>
public interface ITenantUnitOfWork : IAsyncDisposable
{
    IOwnerRepository Owners { get; }
    IParcelRepository Parcels { get; }
    IParcelPhotoRepository ParcelPhotos { get; }
    IContactRepository Contacts { get; }
    IStaffRepository Staff { get; }
    IBranchRepository Branches { get; }
    ISettingRepository Settings { get; }
    ILeadRepository Leads { get; }
    IInteractionRepository Interactions { get; }
    IStageHistoryRepository StageHistories { get; }
    IDuplicateInquiryRepository DuplicateInquiries { get; }
    IDealRepository Deals { get; }
    IPaymentRepository Payments { get; }
    ICommissionRepository Commissions { get; }
    IReferralBonusRepository ReferralBonuses { get; }

    /// <summary>Saves everything changed in this unit of work. Returns the number of rows written.</summary>
    Task<int> SaveAsync();

    Task ExecuteInTransactionAsync(Func<Task> operation);
    Task ExecuteInTransactionAsync(Func<Task> operation, IsolationLevel isolationLevel);
}

/// <summary>Creates a unit of work for the signed-in tenant. Services call Create() per operation and never keep the result.</summary>
public interface ITenantUnitOfWorkFactory
{
    ITenantUnitOfWork Create();
}
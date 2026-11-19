using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Globalization;
using WayPlot.domain.Common;
using WayPlot.domain.Entities;
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
        ParcelPhotos = new ParcelPhotoRepository(db);
        Contacts = new ContactRepository(db);
        Staff = new StaffRepository(db);
        Branches = new BranchRepository(db);
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
    public IParcelPhotoRepository ParcelPhotos { get; }
    public IContactRepository Contacts { get; }
    public IStaffRepository Staff { get; }
    public IBranchRepository Branches { get; }
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
        catch (DbUpdateConcurrencyException ex) when (ex.Entries.Any(entry => entry.Entity is Staff))
        {
            throw new StaffConcurrencyConflictException(ex);
        }
        catch (DbUpdateConcurrencyException ex) when (ex.Entries.Any(entry => entry.Entity is Parcel or ParcelPhoto))
        {
            throw new ParcelConcurrencyConflictException(ex);
        }
        catch (DbUpdateConcurrencyException ex) when (ex.Entries.Any(entry =>
            entry.Entity is Deal or Payment or Commission or ReferralBonus or Contact or Lead))
        {
            throw new DealLifecycleConcurrencyConflictException(ex);
        }
        catch (DbUpdateConcurrencyException ex) when (ex.Entries.Any(entry => entry.Entity is Setting))
        {
            throw new SettingConcurrencyConflictException(ex);
        }
        catch (DbUpdateException ex) when (
            ex.Entries.Any(entry => entry.Entity is Setting)
            && ex.InnerException is SqlException sql
            && sql.Number is 2601 or 2627)
        {
            throw new SettingConcurrencyConflictException(ex);
        }
        catch (DbUpdateException ex) when (
            ex.Entries.Any(entry => entry.Entity is Parcel)
            && ex.InnerException is SqlException sql
            && sql.Number is 2601 or 2627
            && sql.Message.Contains("UX_Parcel_Label", StringComparison.Ordinal))
        {
            throw new ParcelLabelConflictException(ex);
        }
        catch (DbUpdateException ex) when (
            ex.Entries.Any(entry => entry.Entity is Deal)
            && ex.InnerException is SqlException sql
            && sql.Number is 2601 or 2627
            && sql.Message.Contains("UX_Deal_OneActivePerParcel", StringComparison.Ordinal))
        {
            throw new ActiveDealConflictException(ex);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is SqlException sql
            && sql.Number is 2601 or 2627
            && sql.Message.Contains("UX_Payment_Receipt", StringComparison.Ordinal))
        {
            throw new ReceiptNumberConflictException(ex);
        }
    }

    public Task ExecuteInTransactionAsync(Func<Task> operation) =>
        ExecuteInTransactionAsync(operation, IsolationLevel.ReadCommitted);

    public async Task ExecuteInTransactionAsync(Func<Task> operation, IsolationLevel isolationLevel)
    {
        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(isolationLevel);
            await operation();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException ex) when (
            isolationLevel == IsolationLevel.Serializable
            && ex.InnerException is SqlException sql
            && sql.Number == 1205)
        {
            throw new StaffConcurrencyConflictException(ex);
        }
        catch (SqlException ex) when (isolationLevel == IsolationLevel.Serializable && ex.Number == 1205)
        {
            throw new StaffConcurrencyConflictException(ex);
        }
    }

    public async Task<bool> AcquirePipelineStagesLockAsync()
    {
        var transaction = _db.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException("The pipeline stages lock requires an active transaction.");
        await using var command = _db.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock
                @Resource = @resource,
                @LockMode = N'Exclusive',
                @LockOwner = N'Transaction',
                @LockTimeout = 15000;
            SELECT @result;
            """;
        var resource = command.CreateParameter();
        resource.ParameterName = "@resource";
        resource.Value = "WayPlot:PipelineStages";
        command.Parameters.Add(resource);
        var result = await command.ExecuteScalarAsync();
        return result is not null
            && result != DBNull.Value
            && Convert.ToInt32(result, CultureInfo.InvariantCulture) >= 0;
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();
}

/// <summary>Every Create() opens a fresh context on the signed-in tenant's database. Nothing is kept between operations.</summary>
internal sealed class TenantUnitOfWorkFactory(ITenantDbContextFactory dbFactory, TenantSession session)
    : ITenantUnitOfWorkFactory
{
    public ITenantUnitOfWork Create() => new TenantUnitOfWork(dbFactory.Create(session.ConnectionString));
}
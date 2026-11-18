using System.Data;
using WayPlot.domain.Common;
using WayPlot.domain.Entities;
using WayPlot.domain.Events;
using WayPlot.domain.Interfaces;
using WayPlot.domain.Repositories;
using WayPlot.domain.Services.MainTransaction;
using Xunit;

namespace WayPlot.Tests;

public sealed class DealStatusRefreshEventTests
{
    [Fact]
    public async Task Refresh_dispatches_only_for_a_status_change_that_saved_successfully()
    {
        var changed = CreateDeal(DealStatus.Overdue);
        var unchanged = CreateDeal(DealStatus.Current);
        var conflict = CreateDeal(DealStatus.Overdue);
        var events = new EventCollector();
        var factory = new UnitOfWorkFactory([changed, unchanged, conflict], conflict.Id);
        var service = new DealService(factory, events, null!, new TestClock(), null!, null!, TimeProvider.System);

        var result = await service.RefreshDealStatusesAsync();

        Assert.True(result.Ok);
        Assert.Contains("1 conflicting row(s) were skipped", result.Message);
        var dealChanged = Assert.Single(events.Events.OfType<DealChanged>());
        Assert.Equal(changed.Id, dealChanged.DealId);
    }

    private static Deal CreateDeal(DealStatus status)
    {
        var parcelId = Guid.NewGuid();
        return new Deal
        {
            Id = Guid.NewGuid(),
            ParcelId = parcelId,
            Parcel = new Parcel { Id = parcelId, Label = "Lot 1", Status = ParcelStatus.Sold },
            ContactId = Guid.NewGuid(),
            Price = 100m,
            ReservationFee = 100m,
            ConfirmedAtUtc = DateTime.UtcNow.AddDays(1),
            Status = status,
            IsActive = true
        };
    }

    private sealed class EventCollector : IDomainEventDispatcher
    {
        public List<IDomainEvent> Events { get; } = [];
        public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken ct = default)
        {
            Events.Add(domainEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class TestClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }

    private sealed class UnitOfWorkFactory(IReadOnlyList<Deal> deals, Guid conflictingDealId)
        : ITenantUnitOfWorkFactory
    {
        public ITenantUnitOfWork Create() => new UnitOfWork(deals, conflictingDealId);
    }

    private sealed class UnitOfWork : ITenantUnitOfWork
    {
        private readonly Guid _conflictingDealId;
        private readonly DealRepository _deals;
        private Deal? _touched;

        public UnitOfWork(IReadOnlyList<Deal> deals, Guid conflictingDealId)
        {
            _conflictingDealId = conflictingDealId;
            _deals = new DealRepository(deals, deal => _touched = deal);
        }

        public IOwnerRepository Owners => null!;
        public IParcelRepository Parcels => null!;
        public IParcelPhotoRepository ParcelPhotos => null!;
        public IContactRepository Contacts => null!;
        public IStaffRepository Staff => null!;
        public IBranchRepository Branches => throw new NotSupportedException();
        public ISettingRepository Settings => null!;
        public ILeadRepository Leads => null!;
        public IInteractionRepository Interactions => null!;
        public IStageHistoryRepository StageHistories => null!;
        public IDuplicateInquiryRepository DuplicateInquiries => null!;
        public IDealRepository Deals => _deals;
        public IPaymentRepository Payments => null!;
        public ICommissionRepository Commissions => null!;
        public IReferralBonusRepository ReferralBonuses => null!;

        public Task<int> SaveAsync()
        {
            if (_touched?.Id == _conflictingDealId)
                throw new DealLifecycleConcurrencyConflictException(new InvalidOperationException("stale version"));
            return Task.FromResult(1);
        }

        public Task ExecuteInTransactionAsync(Func<Task> operation) => operation();
        public Task ExecuteInTransactionAsync(Func<Task> operation, IsolationLevel isolationLevel) => ExecuteInTransactionAsync(operation);
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private sealed class DealRepository(IReadOnlyList<Deal> deals, Action<Deal> touch) : IDealRepository
        {
            public Task<Deal?> GetAsync(Guid id) => Task.FromResult(deals.FirstOrDefault(deal => deal.Id == id));
            public Task<IReadOnlyList<Deal>> ListAsync(Guid? agentStaffId, Guid? branchId) => Task.FromResult(deals);
            public Task<IReadOnlyList<Deal>> ListFullyPaidMissingCommissionAsync() => throw new NotImplementedException();
            public Task<bool> ActiveForParcelExistsAsync(Guid parcelId) => throw new NotImplementedException();
            public Task<Deal?> GetActiveForParcelAsync(Guid parcelId) => throw new NotImplementedException();
            public Task<bool> HasOtherLiveDealForContactAsync(Guid contactId, Guid exceptDealId) => throw new NotImplementedException();
            public Task<IReadOnlyList<DealStatus>> ListStatusesByParcelAsync(Guid parcelId) => throw new NotImplementedException();
            public Task<IReadOnlyList<Deal>> ListByLeadAsync(Guid leadId) => throw new NotImplementedException();
            public Task<IReadOnlyList<Deal>> ListByContactAsync(Guid contactId, WayPlot.domain.Services.DataCollection.TenantDataScope? scope = null) => throw new NotImplementedException();
            public void Add(Deal deal) => throw new NotImplementedException();
            public void Touch(Deal deal) => touch(deal);
        }
    }
}

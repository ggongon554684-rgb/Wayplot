using WayPlot.domain.Common;

namespace WayPlot.domain.Events;

public interface IDomainEvent
{
    DateTime OccurredAtUtc { get; }
}

public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken ct = default);
}

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken ct = default);
}

// Tier A raises these. Nothing listens yet. Tier B adds scoring and reminder handlers without editing Tier A code.
public sealed record LeadCaptured(Guid LeadId, DateTime OccurredAtUtc) : IDomainEvent;
public sealed record InteractionLogged(Guid LeadId, InteractionType Type, InteractionChannel Channel, DateTime OccurredAtUtc) : IDomainEvent;
public sealed record LeadStageChanged(Guid LeadId, LeadStage From, LeadStage To, DateTime OccurredAtUtc) : IDomainEvent;
public sealed record PaymentConfirmed(Guid DealId, Guid PaymentId, DateTime OccurredAtUtc) : IDomainEvent;
public sealed record DealChanged(Guid DealId, Guid ParcelId, Guid ContactId, DateTime OccurredAtUtc) : IDomainEvent;
public sealed record PayoutChanged(Guid DealId, Guid PayoutId, PayoutKind Kind, DateTime OccurredAtUtc) : IDomainEvent;

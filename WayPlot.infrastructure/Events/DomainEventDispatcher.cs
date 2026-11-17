using Microsoft.Extensions.DependencyInjection;
using WayPlot.domain.Events;

namespace WayPlot.infrastructure.Events;

/// <summary>Calls every registered handler for an event. In Tier A no handlers exist, so it does nothing.</summary>
public sealed class DomainEventDispatcher(IServiceProvider services) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken ct = default)
    {
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        var method = handlerType.GetMethod("HandleAsync")!;

        foreach (var handler in services.GetServices(handlerType))
        {
            var task = (Task)method.Invoke(handler, [domainEvent, ct])!;
            await task;
        }
    }
}

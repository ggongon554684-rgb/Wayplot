using WayPlot.domain.Interfaces;

namespace WayPlot.infrastructure.Common;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

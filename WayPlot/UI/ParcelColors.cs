using WayPlot.domain.Common;

namespace WayPlot.UI;

public static class ParcelColors
{
    public static Color For(ParcelStatus status) => status switch
    {
        ParcelStatus.Available => Color.FromArgb(34, 150, 100),
        ParcelStatus.Reserved => Color.FromArgb(226, 150, 30),
        ParcelStatus.Sold => Color.FromArgb(205, 70, 80),
        ParcelStatus.Leased => Color.FromArgb(96, 80, 200),
        _ => Color.Gray
    };
}

using System.ComponentModel;
using System.Globalization;
using WayPlot.domain.Contracts;

namespace WayPlot.UI;

public partial class MoneyBarControl : UserControl
{
    private decimal _paid;
    private decimal _total;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal Paid
    {
        get => _paid;
        set { _paid = value; Invalidate(); }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal Total
    {
        get => _total;
        set { _total = value; Invalidate(); }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BarColor { get; set; } = Theme.Navy;

    public MoneyBarControl()
    {
        InitializeComponent();
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
            | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
        Height = LogicalToDeviceUnits(34);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var percent = _total <= 0 ? 0m : Math.Clamp(_paid * 100m / _total, 0m, 100m);
        var text = $"₱{_paid.ToString("N2", CultureInfo.GetCultureInfo("en-PH"))} paid of " +
                   $"₱{_total.ToString("N2", CultureInfo.GetCultureInfo("en-PH"))}, {percent:N0}%";

        var barRect = new Rectangle(0, 0, Math.Max(1, Width), Height);
        using var back = new SolidBrush(Color.FromArgb(232, 235, 238));
        g.FillRectangle(back, barRect);

        var fillWidth = (int)Math.Round(barRect.Width * (double)(percent / 100m));
        if (fillWidth > 0)
        {
            using var fill = new SolidBrush(BarColor);
            g.FillRectangle(fill, new Rectangle(0, 0, fillWidth, Height));
        }

        TextRenderer.DrawText(g, text, Theme.Small, barRect, Color.Black,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }
}

using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace WayPlot.UI;

/// <summary>A card with rounded corners (radius 24 by default). Children placed inside keep the card's BackColor.</summary>
public class RoundedPanel : Panel
{
    private int _radius = Theme.CardRadius;
    private Color _fill = Theme.Surface;
    private Color _border = Theme.Border;

    public RoundedPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        base.BackColor = _fill;
        Padding = new Padding(24);   // keeps children clear of the rounded corners
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int Radius
    {
        get => _radius;
        set { _radius = Math.Max(0, value); Invalidate(); }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color BorderColor
    {
        get => _border;
        set { _border = value; Invalidate(); }
    }

    /// <summary>The card's fill color.</summary>
    public override Color BackColor
    {
        get => _fill;
        set { _fill = value; base.BackColor = value; Invalidate(); }
    }

    protected override void OnPaintBackground(PaintEventArgs e) =>
        e.Graphics.Clear(Parent?.BackColor ?? Theme.Page);   // the corners show the parent's color

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using var path = Theme.RoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), _radius);
        using var fill = new SolidBrush(_fill);
        using var pen = new Pen(_border);
        g.FillPath(fill, path);
        g.DrawPath(pen, path);
    }
}
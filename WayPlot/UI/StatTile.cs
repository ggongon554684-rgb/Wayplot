using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace WayPlot.UI;

/// <summary>A small summary tile (radius 14): a caption, a big value and an optional note.</summary>
public sealed class StatTile : Control
{
    private string _caption = string.Empty;
    private string _value = string.Empty;
    private string _note = string.Empty;

    public StatTile()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Size = new Size(190, 104);
        TabStop = false;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string Caption { get => _caption; set { _caption = value; Invalidate(); } }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string Value { get => _value; set { _value = value; Invalidate(); } }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public string Note { get => _note; set { _note = value; Invalidate(); } }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using (var path = Theme.RoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), Theme.TileRadius))
        using (var fill = new SolidBrush(Theme.Surface))
        using (var pen = new Pen(Theme.Border))
        {
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
        }

        const TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                                      TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding;

        TextRenderer.DrawText(g, _caption, Theme.Small, new Rectangle(16, 12, Width - 32, 18), Theme.Muted, flags);
        TextRenderer.DrawText(g, _value, Theme.Stat, new Rectangle(16, 30, Width - 32, 38), Theme.Navy, flags);
        TextRenderer.DrawText(g, _note, Theme.Small, new Rectangle(16, Height - 30, Width - 32, 18), Theme.Muted, flags);
    }
}
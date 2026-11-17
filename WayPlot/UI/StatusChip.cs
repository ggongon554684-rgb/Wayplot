using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Drawing2D;

namespace WayPlot.UI;

public enum ChipKind { Green, Amber, Red, Violet }

/// <summary>A small pill with a status word. It sizes itself to its text.</summary>
public sealed class StatusChip : Control
{
    private const int ChipHeight = 26;
    private const int PadX = 12;
    private ChipKind _kind = ChipKind.Violet;

    public StatusChip()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Font = Theme.SmallBold;
        TabStop = false;
        FitToText();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public ChipKind Kind
    {
        get => _kind;
        set { _kind = value; Invalidate(); }
    }

    [AllowNull]
    public override string Text
    {
        get => base.Text;
        set { base.Text = value; FitToText(); Invalidate(); }
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        FitToText();
    }

    private void FitToText()
    {
        var textWidth = TextRenderer.MeasureText(Text, Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding).Width;
        Size = new Size(textWidth + PadX * 2, ChipHeight);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var (fore, back) = Theme.ChipColors(_kind);
        using var path = Theme.RoundedPath(new Rectangle(0, 0, Width - 1, Height - 1), Height / 2);
        using var brush = new SolidBrush(back);
        g.FillPath(brush, path);

        TextRenderer.DrawText(g, Text, Font, new Rectangle(0, 0, Width, Height), fore,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
    }
}
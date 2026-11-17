using System.Drawing.Drawing2D;

namespace WayPlot.UI;

/// <summary>A row of rounded pill tabs. The selected pill is navy; a disabled pill is greyed and shows its hint on hover.</summary>
public sealed class PillTabBar : Control
{
    public sealed record Tab(string Key, string Title, bool Enabled = true, string? Hint = null);

    private const int PillHeight = 40;
    private const int PillGap = 8;
    private const int PillPadX = 22;

    private readonly List<Tab> _tabs = [];
    private readonly List<Rectangle> _bounds = [];
    private readonly ToolTip _tip = new();
    private int _selected = -1;
    private int _hover = -1;

    public event EventHandler? SelectedKeyChanged;

    public PillTabBar()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        BackColor = Theme.Page;
        Font = Theme.Body;
        Height = 56;
        TabStop = false;
    }

    public string? SelectedKey => _selected >= 0 ? _tabs[_selected].Key : null;

    /// <summary>Replaces the pills and selects selectKey (or does nothing if it is missing or disabled).</summary>
    public void SetTabs(IEnumerable<Tab> tabs, string? selectKey = null)
    {
        _tabs.Clear();
        _tabs.AddRange(tabs);
        _selected = -1;
        _hover = -1;
        LayoutPills();
        Invalidate();

        if (selectKey is not null)
            SelectKey(selectKey);
    }

    public void SelectKey(string key)
    {
        var index = _tabs.FindIndex(t => t.Key == key);
        if (index < 0 || !_tabs[index].Enabled || index == _selected) return;

        _selected = index;
        Invalidate();
        SelectedKeyChanged?.Invoke(this, EventArgs.Empty);
    }

    private void LayoutPills()
    {
        _bounds.Clear();
        var x = 0;
        var y = (Height - PillHeight) / 2;
        foreach (var tab in _tabs)
        {
            var textWidth = TextRenderer.MeasureText(tab.Title, Font, new Size(int.MaxValue, int.MaxValue),
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;
            var width = textWidth + PillPadX * 2;
            _bounds.Add(new Rectangle(x, y, width, PillHeight));
            x += width + PillGap;
        }
    }

    private int HitTest(Point p)
    {
        for (var i = 0; i < _bounds.Count; i++)
            if (_bounds[i].Contains(p)) return i;
        return -1;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        LayoutPills();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        LayoutPills();
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var index = HitTest(e.Location);
        if (index == _hover) return;

        _hover = index;
        Cursor = index >= 0 && _tabs[index].Enabled ? Cursors.Hand : Cursors.Default;
        _tip.SetToolTip(this, index >= 0 ? _tabs[index].Hint ?? string.Empty : string.Empty);
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hover = -1;
        Cursor = Cursors.Default;
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        var index = HitTest(e.Location);
        if (index >= 0 && _tabs[index].Enabled)
            SelectKey(_tabs[index].Key);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_bounds.Count != _tabs.Count) LayoutPills();

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        for (var i = 0; i < _tabs.Count; i++)
        {
            var tab = _tabs[i];
            var r = _bounds[i];

            Color fill, line, fore;
            if (i == _selected) { fill = Theme.Navy; line = Theme.Navy; fore = Color.White; }
            else if (!tab.Enabled) { fill = Theme.Surface; line = Theme.Border; fore = Theme.Disabled; }
            else if (i == _hover) { fill = Theme.SurfaceHover; line = Theme.Border; fore = Theme.Navy; }
            else { fill = Theme.Surface; line = Theme.Border; fore = Theme.Text; }

            using var path = Theme.RoundedPath(new Rectangle(r.X, r.Y, r.Width - 1, r.Height - 1), PillHeight / 2);
            using var brush = new SolidBrush(fill);
            using var pen = new Pen(line);
            g.FillPath(brush, path);
            g.DrawPath(pen, path);
            TextRenderer.DrawText(g, tab.Title, Font, r, fore,
             TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _tip.Dispose();
        base.Dispose(disposing);
    }
}
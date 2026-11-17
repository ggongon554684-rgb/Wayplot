using System.ComponentModel;
using System.Drawing.Drawing2D;
using WayPlot.domain.Common;
using WayPlot.Presenters;

namespace WayPlot.UI;

/// <summary>One color per parcel status, shared by the map and the inspector.</summary>
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

/// <summary>Draws parcels as polygons and markers on a plain background. Click selects, the wheel zooms around the cursor,
/// dragging pans, double-click on empty map fits everything. Keys: + and - zoom, Home fits, Esc clears the selection.
/// It holds no data of its own: the presenter hands it MapShape objects through SetShapes.</summary>
public sealed class ParcelMapControl : Control
{
    private const double MinZoomOverFit = 0.25;
    private const double MaxZoomOverFit = 200;
    private const double MinExtent = 30 / 6378137.0;   // about 30 m in map units, so one marker does not zoom to infinity
    private const int DragThreshold = 4;

    private List<MapShape> _shapes = [];
    private Guid? _selectedId;
    private Guid? _hoverId;

    private PointD _center;
    private double _scale = 1;
    private double _fitScale = 1;
    private bool _userMoved;

    private bool _mouseDown;
    private bool _dragging;
    private Point _downAt;
    private Point _lastAt;

    private string? _message;
    private string? _note;

    /// <summary>Raised when the selected parcel changes. Read SelectedId.</summary>
    public event EventHandler? SelectionChanged;

    public ParcelMapControl()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.Selectable, true);
        TabStop = true;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Guid? SelectedId => _selectedId;

    // ---- Called by the screen ----

    public void SetShapes(IReadOnlyList<MapShape> shapes)
    {
        _shapes = shapes.ToList();
        _hoverId = null;

        if (_selectedId is { } selected && _shapes.All(s => s.Id != selected))
        {
            _selectedId = null;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        FitAll();
    }

    public void SetMessage(string? text)
    {
        _message = text;
        Invalidate();
    }

    public void SetNote(string? text)
    {
        _note = text;
        Invalidate();
    }

    public void FitAll()
    {
        _userMoved = false;

        if (_shapes.Count == 0 || Width < 20 || Height < 20)
        {
            _center = default;
            _scale = _fitScale = 1;
            Invalidate();
            return;
        }

        var minX = _shapes.Min(s => s.MinX);
        var maxX = _shapes.Max(s => s.MaxX);
        var minY = _shapes.Min(s => s.MinY);
        var maxY = _shapes.Max(s => s.MaxY);

        var w = Math.Max(maxX - minX, MinExtent);
        var h = Math.Max(maxY - minY, MinExtent);

        var legendHeight = Theme.Small.Height + Px(20);
        var bottomInset = Px(14) + legendHeight + Px(10);
        var mapHeight = Math.Max(20, Height - bottomInset);
        _fitScale = Math.Min(Width * 0.96 / w, mapHeight * 0.96 / h);
        _scale = _fitScale;
        _center = new PointD(
            (minX + maxX) / 2,
            (minY + maxY) / 2 + (mapHeight / 2.0 - Height / 2.0) / _scale);
        Invalidate();
    }

    // ---- Coordinates ----

    private double ScreenX(double worldX) => (worldX - _center.X) * _scale + Width / 2.0;
    private double ScreenY(double worldY) => Height / 2.0 - (worldY - _center.Y) * _scale;
    private PointF ToScreen(PointD p) => new((float)ScreenX(p.X), (float)ScreenY(p.Y));

    private PointD ToWorld(double sx, double sy) =>
        new(_center.X + (sx - Width / 2.0) / _scale, _center.Y - (sy - Height / 2.0) / _scale);

    private int Px(int logical) => LogicalToDeviceUnits(logical);

    private void ZoomAt(double sx, double sy, double factor)
    {
        if (_shapes.Count == 0) return;

        var newScale = Math.Clamp(_scale * factor, _fitScale * MinZoomOverFit, _fitScale * MaxZoomOverFit);
        if (Math.Abs(newScale - _scale) < double.Epsilon) return;

        // Keep the point under the cursor where it is.
        var before = ToWorld(sx, sy);
        _scale = newScale;
        _center = new PointD(
            before.X - (sx - Width / 2.0) / _scale,
            before.Y + (sy - Height / 2.0) / _scale);

        _userMoved = true;
        Invalidate();
    }

    // ---- Selection and hit testing ----

    private void SelectById(Guid? id)
    {
        if (_selectedId == id) return;
        _selectedId = id;
        Invalidate();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private MapShape? HitTest(Point p)
    {
        var world = ToWorld(p.X, p.Y);
        var markerReach = Px(12);

        for (var i = _shapes.Count - 1; i >= 0; i--)
        {
            var shape = _shapes[i];
            if (shape.Marker is { } marker)
            {
                var dx = ScreenX(marker.X) - p.X;
                var dy = ScreenY(marker.Y) - p.Y;
                if (dx * dx + dy * dy <= markerReach * markerReach) return shape;
            }
            else if (shape.Contains(world))
            {
                return shape;
            }
        }
        return null;
    }

    // ---- Mouse and keyboard ----

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        if (!DesignMode && CanFocus) Focus();   // the wheel only reaches the focused control
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (DesignMode) return;
        Focus();
        if (e.Button != MouseButtons.Left) return;

        _mouseDown = true;
        _dragging = false;
        _downAt = _lastAt = e.Location;
        Capture = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (DesignMode) return;

        if (_mouseDown)
        {
            if (!_dragging &&
                (Math.Abs(e.X - _downAt.X) > DragThreshold || Math.Abs(e.Y - _downAt.Y) > DragThreshold))
            {
                _dragging = true;
                Cursor = Cursors.SizeAll;
            }

            if (_dragging)
            {
                _center = new PointD(
                    _center.X - (e.X - _lastAt.X) / _scale,
                    _center.Y + (e.Y - _lastAt.Y) / _scale);
                _lastAt = e.Location;
                _userMoved = true;
                Invalidate();
            }
            return;
        }

        UpdateHover(e.Location);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left || !_mouseDown) return;

        var wasDrag = _dragging;
        _mouseDown = false;
        _dragging = false;
        Capture = false;
        Cursor = Cursors.Default;

        if (!wasDrag)
            SelectById(HitTest(e.Location)?.Id);

        UpdateHover(e.Location);
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        if (e.Button == MouseButtons.Left && HitTest(e.Location) is null)
            FitAll();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoverId is not null)
        {
            _hoverId = null;
            Invalidate();
        }
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (DesignMode) return;
        ZoomAt(e.X, e.Y, Math.Pow(1.0015, e.Delta));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        switch (e.KeyCode)
        {
            case Keys.Add:
            case Keys.Oemplus:
                ZoomAt(Width / 2.0, Height / 2.0, 1.4);
                e.Handled = true;
                break;
            case Keys.Subtract:
            case Keys.OemMinus:
                ZoomAt(Width / 2.0, Height / 2.0, 1 / 1.4);
                e.Handled = true;
                break;
            case Keys.Home:
                FitAll();
                e.Handled = true;
                break;
            case Keys.Escape:
                SelectById(null);
                e.Handled = true;
                break;
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (!_userMoved) FitAll();   // keep the whole map in view until the agent zooms or pans
    }

    private void UpdateHover(Point p)
    {
        var id = HitTest(p)?.Id;
        if (id == _hoverId) return;

        _hoverId = id;
        Cursor = id is null ? Cursors.Default : Cursors.Hand;
        Invalidate();
    }

    // ---- Painting ----

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Color.White);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var frame = new Rectangle(0, 0, Width - 1, Height - 1);
        using var framePath = RoundedRect(frame, Px(24));

        using (var background = new SolidBrush(BackColor))
            g.FillPath(background, framePath);

        g.SetClip(framePath);

        if (DesignMode)
        {
            DrawCentered(g, "Parcel map");
        }
        else
        {
            DrawGrid(g);

            foreach (var shape in _shapes)
                if (shape.Id != _selectedId) DrawShape(g, shape);
            var selected = _shapes.FirstOrDefault(s => s.Id == _selectedId);
            if (selected is not null) DrawShape(g, selected);

            var legend = DrawLegend(g);
            DrawHint(g, legend);

            if (_note is not null)
                TextRenderer.DrawText(g, _note, Theme.Small, new Rectangle(Px(16), Px(12), Width - Px(32), Px(22)),
                    Theme.Muted, TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis |
                                 TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

            if (_message is not null)
                DrawCentered(g, _message);
        }

        g.ResetClip();
        using var border = new Pen(Color.FromArgb(227, 232, 242), 1f);
        g.DrawPath(border, framePath);
    }

    private void DrawGrid(Graphics g)
    {
        // A faint grid so panning and zooming have something to move against while there are no map tiles.
        var step = Px(64);
        var ox = Mod(ScreenX(0), step);
        var oy = Mod(ScreenY(0), step);

        using var pen = new Pen(Color.FromArgb(230, 235, 244), 1f);
        for (var x = ox; x < Width; x += step) g.DrawLine(pen, (float)x, 0, (float)x, Height);
        for (var y = oy; y < Height; y += step) g.DrawLine(pen, 0, (float)y, Width, (float)y);
    }

    private static double Mod(double value, double modulus) => (value % modulus + modulus) % modulus;

    private void DrawShape(Graphics g, MapShape shape)
    {
        var color = ParcelColors.For(shape.Status);
        var selected = shape.Id == _selectedId;
        var hover = shape.Id == _hoverId && !selected;

        if (shape.Marker is { } marker)
        {
            var c = ToScreen(marker);
            if (c.X < -50 || c.Y < -50 || c.X > Width + 50 || c.Y > Height + 50) return;

            var r = Px(9) + (selected ? Px(3) : hover ? Px(1) : 0);
            var rect = new RectangleF(c.X - r, c.Y - r, r * 2, r * 2);
            using var fill = new SolidBrush(color);
            using var outline = new Pen(selected ? Theme.Navy : Color.White, selected ? 3f : 2f);
            g.FillEllipse(fill, rect);
            g.DrawEllipse(outline, rect);
            DrawLabel(g, shape.Label, new Rectangle((int)c.X - Px(60), (int)c.Y + r + Px(2), Px(120), Px(20)));
            return;
        }

        using var path = new GraphicsPath(FillMode.Alternate);
        foreach (var ring in shape.Rings)
        {
            if (ring.Length < 3) continue;
            var points = new PointF[ring.Length];
            for (var i = 0; i < ring.Length; i++)
                points[i] = ToScreen(ring[i]);
            path.AddPolygon(points);
        }

        if (path.PointCount == 0) return;
        var bounds = path.GetBounds();
        if (!bounds.IntersectsWith(ClientRectangle)) return;

        var alpha = selected ? 150 : hover ? 115 : 80;
        using var brush = new SolidBrush(Color.FromArgb(alpha, color));
        using var pen = new Pen(selected ? Theme.Navy : color, selected ? 3f : 1.75f) { LineJoin = LineJoin.Round };
        g.FillPath(brush, path);
        g.DrawPath(pen, path);

        if (bounds.Width >= Px(56) && bounds.Height >= Px(22))
            DrawLabel(g, shape.Label, Rectangle.Round(bounds));
    }

    private static void DrawLabel(Graphics g, string text, Rectangle rect) =>
        TextRenderer.DrawText(g, text, Theme.Small, rect, Theme.Text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

    private void DrawCentered(Graphics g, string text) =>
        TextRenderer.DrawText(g, text, Theme.Body, new Rectangle(Px(24), 0, Width - Px(48), Height), Theme.Muted,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak |
            TextFormatFlags.NoPrefix);

    /// <summary>Draws the status colors bottom-left and returns the box, so the hint can sit beside it.</summary>
    private Rectangle DrawLegend(Graphics g)
    {
        var statuses = Enum.GetValues<ParcelStatus>();
        var font = Theme.Small;
        var dot = Px(10);
        var pad = Px(10);
        var gap = Px(14);
        var textGap = Px(6);

        var widths = statuses
            .Select(s => TextRenderer.MeasureText(s.ToString(), font, new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width)
            .ToArray();

        var height = font.Height + pad * 2;
        var width = pad * 2 + statuses.Length * (dot + textGap) + widths.Sum() + gap * (statuses.Length - 1);
        var box = new Rectangle(Px(14), Height - Px(14) - height, width, height);

        using (var path = RoundedRect(box, Px(10)))
        using (var fill = new SolidBrush(Color.FromArgb(235, 255, 255, 255)))
        using (var edge = new Pen(Color.FromArgb(227, 232, 242), 1f))
        {
            g.FillPath(fill, path);
            g.DrawPath(edge, path);
        }

        var x = box.X + pad;
        for (var i = 0; i < statuses.Length; i++)
        {
            using (var dotBrush = new SolidBrush(ParcelColors.For(statuses[i])))
                g.FillEllipse(dotBrush, x, box.Y + (height - dot) / 2, dot, dot);
            x += dot + textGap;

            TextRenderer.DrawText(g, statuses[i].ToString(), font, new Rectangle(x, box.Y, widths[i] + Px(4), height),
                Theme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            x += widths[i] + gap;
        }
        return box;
    }

    private void DrawHint(Graphics g, Rectangle legend)
    {
        const string text = "Scroll to zoom · drag to pan · double-click to fit";
        var font = Theme.Small;
        var size = TextRenderer.MeasureText(text, font, new Size(int.MaxValue, int.MaxValue),
            TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);

        var right = Width - Px(18);
        if (right - size.Width < legend.Right + Px(16)) return;   // no room beside the legend

        TextRenderer.DrawText(g, text, font, new Rectangle(right - size.Width, legend.Y, size.Width, legend.Height),
            Theme.Muted, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine |
                         TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
    }

    private static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        var d = radius * 2;
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
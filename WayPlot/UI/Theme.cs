using System.Drawing.Drawing2D;

namespace WayPlot.UI;

/// <summary>One place for colors, fonts and sizes. White is dominant, navy is the accent.</summary>
public static class Theme
{
    public const int CardRadius = 24;
    public const int TileRadius = 14;
    public const int ButtonHeight = 40;

    public static readonly Color Page = Hex("#FFFFFF");
    public static readonly Color Surface = Hex("#F5F7FB");
    public static readonly Color Border = Hex("#E3E8F2");
    public static readonly Color Navy = Hex("#14284B");
    public static readonly Color Text = Hex("#16233F");
    public static readonly Color Muted = Hex("#5F6B80");

    // Used by the pill tabs for hover and disabled states.
    public static readonly Color SurfaceHover = Hex("#E9EEF7");
    public static readonly Color Disabled = Hex("#A3ADBF");

    public static readonly Font Body = new("Segoe UI", 10f, FontStyle.Regular);
    public static readonly Font BodyBold = new("Segoe UI", 10f, FontStyle.Bold);
    public static readonly Font Small = new("Segoe UI", 9f, FontStyle.Regular);
    public static readonly Font SmallBold = new("Segoe UI", 9f, FontStyle.Bold);
    public static readonly Font Heading = new("Segoe UI", 14f, FontStyle.Bold);
    public static readonly Font Title = new("Segoe UI", 20f, FontStyle.Bold);
    public static readonly Font Stat = new("Segoe UI", 22f, FontStyle.Bold);

    public static (Color Fore, Color Back) ChipColors(ChipKind kind) => kind switch
    {
        ChipKind.Green => (Hex("#14633D"), Hex("#DDF1E6")),
        ChipKind.Amber => (Hex("#7A4B00"), Hex("#FBEBC8")),
        ChipKind.Red => (Hex("#8F2A1B"), Hex("#F9DDD9")),
        _ => (Hex("#43337A"), Hex("#E6E2F6"))
    };

    /// <summary>A rounded rectangle. Pass the rectangle you want drawn (for borders, use Width - 1 and Height - 1).</summary>
    public static GraphicsPath RoundedPath(Rectangle r, int radius)
    {
        var d = Math.Max(1, Math.Min(radius * 2, Math.Min(r.Width, r.Height)));
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    /// <summary>Flat button, never shorter than 40 px. Primary is navy; secondary is white with a border.</summary>
    public static Button CreateButton(string text, bool primary = true)
    {
        var button = new Button
        {
            Text = text,
            Height = ButtonHeight,
            MinimumSize = new Size(0, ButtonHeight),
            FlatStyle = FlatStyle.Flat,
            Font = BodyBold,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            BackColor = primary ? Navy : Page,
            ForeColor = primary ? Color.White : Navy,
            Padding = new Padding(16, 0, 16, 0)
        };
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = primary ? Navy : Border;
        return button;
    }

    private static Color Hex(string html) => ColorTranslator.FromHtml(html);
}
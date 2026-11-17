using System.Text.RegularExpressions;

namespace WayPlot.UI;

/// <summary>Styles a Designer button the same way Theme.CreateButton styles a code-made one.</summary>
public static class ButtonStyle
{
    public static void Apply(Button button, bool primary = true)
    {
        var height = button.LogicalToDeviceUnits(Theme.ButtonHeight);
        button.FlatStyle = FlatStyle.Flat;
        button.Font = Theme.BodyBold;
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
        button.BackColor = primary ? Theme.Navy : Theme.Page;
        button.ForeColor = primary ? Color.White : Theme.Navy;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = primary ? Theme.Navy : Theme.Border;
        button.MinimumSize = new Size(0, height);
        button.Height = height;
        button.Padding = new Padding(16, 0, 16, 0);
    }
}

/// <summary>Turns enum names such as PhoneCall into words for screens ("Phone call").</summary>
public static class EnumText
{
    public static string Pretty(Enum value)
    {
        var text = Regex.Replace(value.ToString(), "(?<=[a-z0-9])(?=[A-Z])", " ");
        return text.Length < 2 ? text : text[0] + text[1..].ToLowerInvariant();
    }
}
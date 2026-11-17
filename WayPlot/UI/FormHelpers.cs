using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

namespace WayPlot.UI;

public readonly record struct ButtonAvailability(
    Button Button,
    bool Visible,
    bool Enabled,
    string? DisabledReason,
    bool Primary);

/// <summary>Styles a Designer button the same way Theme.CreateButton styles a code-made one.</summary>
public static class ButtonStyle
{
    private static readonly ConditionalWeakTable<Control, DisabledButtonToolTipTracker> DisabledToolTipTrackers = new();

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
        button.FlatAppearance.MouseOverBackColor = primary ? Theme.Navy : Theme.SurfaceHover;
        button.FlatAppearance.MouseDownBackColor = primary ? Theme.Navy : Theme.SurfaceHover;
        button.MinimumSize = new Size(0, height);
        button.Height = height;
        button.Padding = new Padding(16, 0, 16, 0);
    }

    public static void ApplyAvailability(IEnumerable<ButtonAvailability> controls, ToolTip toolTip)
    {
        foreach (var control in controls)
        {
            var button = control.Button;
            Apply(button, control.Primary);
            button.Visible = control.Visible;
            button.Enabled = control.Visible && control.Enabled;
            if (!button.Enabled)
            {
                button.BackColor = Theme.Surface;
                button.ForeColor = Theme.Disabled;
                button.Cursor = Cursors.Default;
                button.FlatAppearance.BorderColor = Theme.Border;
                button.FlatAppearance.MouseOverBackColor = Theme.Surface;
                button.FlatAppearance.MouseDownBackColor = Theme.Surface;
                var reason = control.Visible ? control.DisabledReason ?? "Unavailable." : null;
                toolTip.SetToolTip(button, string.Empty);
                if (button.Parent is { } parent)
                    DisabledToolTipTrackers.GetValue(parent, key => new DisabledButtonToolTipTracker(key, toolTip))
                        .SetReason(button, reason);
            }
            else
            {
                button.Cursor = Cursors.Hand;
                toolTip.SetToolTip(button, string.Empty);
                if (button.Parent is { } parent)
                    DisabledToolTipTrackers.GetValue(parent, key => new DisabledButtonToolTipTracker(key, toolTip))
                        .SetReason(button, null);
            }
        }
    }

    private sealed class DisabledButtonToolTipTracker
    {
        private readonly Control _parent;
        private readonly ToolTip _toolTip;
        private readonly Dictionary<Button, string> _reasons = [];
        private Button? _hoveredButton;

        public DisabledButtonToolTipTracker(Control parent, ToolTip toolTip)
        {
            _parent = parent;
            _toolTip = toolTip;
            _parent.MouseMove += OnParentMouseMove;
            _parent.MouseLeave += OnParentMouseLeave;
        }

        public void SetReason(Button button, string? reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                _reasons.Remove(button);
            else
                _reasons[button] = reason;

            if (_hoveredButton == button)
            {
                _toolTip.Hide(_parent);
                _hoveredButton = null;
            }
        }

        private void OnParentMouseMove(object? sender, MouseEventArgs e)
        {
            var child = _parent.GetChildAtPoint(e.Location, GetChildAtPointSkip.None);
            if (child is Button button
                && !button.Enabled
                && _reasons.TryGetValue(button, out var reason))
            {
                if (_hoveredButton != button)
                {
                    _toolTip.Show(reason, _parent, e.X + 12, e.Y + 12, 5000);
                    _hoveredButton = button;
                }
                return;
            }

            if (_hoveredButton is not null)
            {
                _toolTip.Hide(_parent);
                _hoveredButton = null;
            }
        }

        private void OnParentMouseLeave(object? sender, EventArgs e)
        {
            _toolTip.Hide(_parent);
            _hoveredButton = null;
        }
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
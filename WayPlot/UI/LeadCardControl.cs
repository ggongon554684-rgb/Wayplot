using System.ComponentModel;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.UI;

namespace WayPlot.UI;

public partial class LeadCardControl : UserControl
{
    public LeadCardControl()
    {
        InitializeComponent();
        ApplyTheme();
        // The labels and the inner layout cover the whole card, so they must forward the mouse
        // events too; otherwise clicking the card text selects nothing and cannot start a drag.
        WireMouse(this);
    }

    private Point _pressPoint;
    private bool _pressArmed;

    private void WireMouse(Control control)
    {
        control.MouseDown += (_, args) =>
        {
            if (args.Button != MouseButtons.Left) return;
            _pressPoint = Cursor.Position;
            _pressArmed = true;
        };
        control.MouseMove += (_, _) => TryStartDrag();
        control.MouseUp += (_, _) => _pressArmed = false;
        if (control is not Button)
            control.Click += (_, _) => { if (Card is not null) Selected?.Invoke(Card); };
        foreach (Control child in control.Controls)
        {
            if (child is Button) continue;
            WireMouse(child);
        }
    }

    // A plain click selects the card; only a real drag (mouse moved past the system drag size) starts a move.
    private void TryStartDrag()
    {
        if (!_pressArmed || Card is null || DragStarting is null) return;
        if ((MouseButtons & MouseButtons.Left) == 0)
        {
            _pressArmed = false;
            return;
        }
        var drag = SystemInformation.DragSize;
        var now = Cursor.Position;
        if (Math.Abs(now.X - _pressPoint.X) < drag.Width / 2 && Math.Abs(now.Y - _pressPoint.Y) < drag.Height / 2)
            return;
        _pressArmed = false;
        AsyncUiEvent.Run(this, StartDragAsync, "Could not start the lead drag. Try again.");
    }

    private async Task StartDragAsync()
    {
        if (Card is null || DragStarting is null) return;
        await DragStarting(Card);
        if ((MouseButtons & MouseButtons.Left) == 0) return;   // released while the allowed moves were loading
        DoDragDrop(Card.LeadId, DragDropEffects.Move);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public LeadCardDto Card { get; private set; } = null!;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<LeadCardDto>? Selected { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<LeadCardDto>? MoveRequested { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<LeadCardDto, Task<IReadOnlyList<LeadMoveOptionDto>>>? DragStarting { get; set; }

    public void SetCard(LeadCardDto card, bool isAdmin)
    {
        Card = card;
        lblContact.Text = card.ContactName;
        lblParcel.Text = card.ParcelLabel;
        lblStageInfo.Text = card.DaysInStage == 1 ? "1 day in stage" : $"{card.DaysInStage} days in stage";
        lblActivity.Text = $"Last activity {Relative(card.LastActivityAt)}";
        lblAgent.Visible = isAdmin;
        lblAgent.Text = $"Agent: {card.AgentName ?? "Unassigned"}";
        lblClosed.Visible = card.HasPendingClosedRequest;
        lblDeal.Visible = card.DealStatus is not null;
        lblDeal.Text = card.DealStatus is { } status ? $"Deal: {EnumText.Pretty(status)}" : "";
        btnMove.Visible = MoveRequested is not null;
    }

    private void ApplyTheme()
    {
        BackColor = Theme.Surface;
        lblContact.Font = Theme.BodyBold;
        lblContact.ForeColor = Theme.Navy;
        lblParcel.ForeColor = Theme.Text;
        lblStageInfo.ForeColor = lblActivity.ForeColor = lblAgent.ForeColor = Theme.Muted;
        lblClosed.ForeColor = Theme.Navy;
        lblDeal.ForeColor = Theme.Text;
        ButtonStyle.Apply(btnMove, false);
    }

    private static string Relative(DateTime value)
    {
        var days = Math.Max(0, (DateTime.UtcNow - value.ToUniversalTime()).Days);
        return days == 0 ? "today" : days == 1 ? "1 day ago" : $"{days} days ago";
    }
}

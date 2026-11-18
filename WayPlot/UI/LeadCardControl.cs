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
        MouseDown += (_, args) => AsyncUiEvent.Run(this, async () =>
        {
            if (args.Button == MouseButtons.Left && Card is not null && DragStarting is not null)
            {
                if (DragStarting is not null)
                    await DragStarting(Card);
                DoDragDrop(Card.LeadId, DragDropEffects.Move);
            }
        }, "Could not start the lead drag. Try again.");
        Click += (_, _) => Selected?.Invoke(Card);
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
        lblStageInfo.Text = $"{card.DaysInStage} days in stage";
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

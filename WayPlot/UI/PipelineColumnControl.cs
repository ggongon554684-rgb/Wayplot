using System.ComponentModel;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;

namespace WayPlot.UI;

public partial class PipelineColumnControl : UserControl
{
    public PipelineColumnControl()
    {
        InitializeComponent();
        ApplyTheme();
        AllowDrop = true;
        DragEnter += (_, args) => args.Effect = args.Data?.GetDataPresent(typeof(Guid)) == true
            ? DragDropEffects.Move : DragDropEffects.None;
        DragDrop += (_, args) =>
        {
            if (args.Data?.GetData(typeof(Guid)) is Guid leadId)
                LeadDropped?.Invoke(leadId, StageKey);
        };
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string StageKey { get; private set; } = "";

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string StageName { get; private set; } = "";

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<Guid, string>? LeadDropped { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<LeadCardDto>? CardSelected { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<LeadCardDto>? MoveRequested { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<LeadCardDto, Task<IReadOnlyList<LeadMoveOptionDto>>>? DragStarting { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ToolTip SharedToolTip { get; set; } = new();

    public void SetColumn(PipelineColumnDto column, bool isAdmin)
    {
        StageKey = column.StageKey;
        StageName = column.StageName;
        lblHeader.Text = $"{column.StageName} ({column.TotalCount})";
        cards.SuspendLayout();
        cards.Controls.Clear();
        foreach (var card in column.Cards)
        {
            var control = new LeadCardControl();
            control.SetCard(card, isAdmin);
            control.Selected = CardSelected;
            control.MoveRequested = MoveRequested;
            control.DragStarting = DragStarting;
            cards.Controls.Add(control);
        }
        cards.ResumeLayout();
        lblNotice.Text = column.TotalCount > column.Cards.Count
            ? $"Showing {column.Cards.Count} of {column.TotalCount}. Narrow the filters."
            : "";
        lblNotice.Visible = !string.IsNullOrEmpty(lblNotice.Text);
    }

    public void SetDropState(bool allowed, string? reason)
    {
        BackColor = allowed ? Color.FromArgb(232, 244, 234) : Theme.Surface;
        SharedToolTip.SetToolTip(this, allowed ? "" : reason);
        Cursor = allowed ? Cursors.Hand : Cursors.No;
    }

    public void ResetDropState()
    {
        BackColor = Theme.Surface;
        SharedToolTip.SetToolTip(this, "");
        Cursor = Cursors.Default;
    }

    private void ApplyTheme()
    {
        BackColor = Theme.Surface;
        lblHeader.Font = Theme.BodyBold;
        lblHeader.ForeColor = Theme.Navy;
        lblNotice.Font = Theme.Small;
        lblNotice.ForeColor = Theme.Muted;
        cards.BackColor = Theme.Page;
    }
}

using System.ComponentModel;
using System.Globalization;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.Presenters;
using WayPlot.UI;

namespace WayPlot.Views;

public partial class CommissionsView : UserControl, ICommissionsView
{
    private CommissionsPresenter? _presenter;

    public CommissionsView()
    {
        InitializeComponent();
        ApplyTheme();

        btnRules.Click += (_, _) => AsyncUiEvent.Run(this, OpenRulesAsync, "Could not open commission rules. Try again.");
        btnApprove.Click += (_, _) => AsyncUiEvent.Run(this, ApproveAsync, "Could not approve the payout. Try again.");
        btnMarkPaid.Click += (_, _) => AsyncUiEvent.Run(this, MarkPaidAsync, "Could not mark the payout as paid. Try again.");
        listPayouts.SelectedIndexChanged += (_, _) => UpdateActions();
        listPayouts.Resize += (_, _) => FitColumns();
        FitColumns();
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public CommissionsPresenter? Presenter
    {
        set
        {
            _presenter = value;
            UpdateActions();
        }
    }

    protected override async void OnLoad(EventArgs e)
    {
        try
        {
            base.OnLoad(e);
            if (DesignMode) return;
            UpdateActions();
            if (_presenter is not null)
                await _presenter.LoadAsync();
        }
        catch (Exception)
        {
            if (!IsDisposed)
                ShowStatus("Could not load commissions. Try again.", true);
        }
    }

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        lblTitle.Font = Theme.Heading;
        lblTitle.ForeColor = Theme.Navy;
        lblHelp.Font = Theme.Small;
        lblHelp.ForeColor = Theme.Muted;
        lblSummary.Font = Theme.Body;
        lblSummary.ForeColor = Theme.Muted;
        listPayouts.Font = Theme.Body;
        listPayouts.BackColor = Theme.Surface;
        listPayouts.ForeColor = Theme.Text;
        ButtonStyle.ApplyAvailability(
        [
            new(btnRules, true, true, null, false),
            new(btnApprove, true, true, null, false),
            new(btnMarkPaid, true, true, null, true)
        ], actionToolTip);
    }

    private void UpdateActions()
    {
        var actions = _presenter?.GetActionStates(SelectedPayout);
        ButtonStyle.ApplyAvailability(
        [
            new(btnRules, actions?.ManageRules.Visible == true,
                actions?.ManageRules.Enabled == true, actions?.ManageRules.DisabledReason, false),
            new(btnApprove, actions?.Approve.Visible == true,
                actions?.Approve.Enabled == true, actions?.Approve.DisabledReason, false),
            new(btnMarkPaid, actions?.MarkPaid.Visible == true,
                actions?.MarkPaid.Enabled == true, actions?.MarkPaid.DisabledReason, true)
        ], actionToolTip);
    }

    private async Task OpenRulesAsync()
    {
        var presenter = _presenter;
        if (presenter is null) return;
        var result = await presenter.GetRulesAsync();
        if (IsDisposed) return;
        if (!result.Ok || result.Value is null)
        {
            ShowStatus(result.Message, true);
            return;
        }

        using var dialog = new CommissionRulesDialog
        {
            Rules = result.Value,
            HasBranching = presenter.HasBranching,
            SaveHandler = presenter.SaveRulesAsync
        };
        if (dialog.ShowDialog(FindForm()) == DialogResult.OK && dialog.Result is { } saved && saved.Ok)
            await presenter.LoadAsync(saved.Message);
    }

    private async Task ApproveAsync()
    {
        var presenter = _presenter;
        if (presenter is null || SelectedPayout is not { } payout) return;
        if (MessageBox.Show(FindForm(), "Approve this payout?", "Approve payout",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        var result = await presenter.ApproveAsync(payout);
        if (IsDisposed) return;
        if (!result.Ok)
        {
            ShowStatus(result.Message, true);
            return;
        }
        await presenter.LoadAsync(result.Message);
    }

    private async Task MarkPaidAsync()
    {
        var presenter = _presenter;
        if (presenter is null || SelectedPayout is not { } payout) return;
        if (MessageBox.Show(FindForm(), "Record this payout as Paid today?", "Mark payout Paid",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        var result = await presenter.MarkPaidAsync(payout);
        if (IsDisposed) return;
        if (!result.Ok)
        {
            ShowStatus(result.Message, true);
            return;
        }
        await presenter.LoadAsync(result.Message);
    }

    public void ShowPayouts(IReadOnlyList<PayoutListItem> payouts)
    {
        if (IsDisposed) return;
        var selected = SelectedPayout is { } current ? (current.Kind, current.Id) : ((PayoutKind Kind, Guid Id)?)null;

        listPayouts.BeginUpdate();
        listPayouts.Items.Clear();
        foreach (var payout in payouts)
        {
            var item = new ListViewItem(EnumText.Pretty(payout.Kind)) { Tag = payout };
            item.SubItems.Add(payout.RecipientName);
            item.SubItems.Add(payout.ContactName);
            item.SubItems.Add(payout.ParcelLabel);
            item.SubItems.Add(Peso(payout.Amount));
            item.SubItems.Add(payout.Status == CommissionStatus.PaidNeedsReview
                ? "Paid — review"
                : EnumText.Pretty(payout.Status));
            item.SubItems.Add(payout.CreatedAtUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture));
            item.SubItems.Add(payout.PaidAtUtc?.ToLocalTime().ToString("g", CultureInfo.CurrentCulture) ?? "—");
            listPayouts.Items.Add(item);
            if (selected is { } selectedPayout && selectedPayout.Kind == payout.Kind && selectedPayout.Id == payout.Id)
                item.Selected = true;
        }
        listPayouts.EndUpdate();
        FitColumns();
        UpdateActions();
    }

    public void ShowStatus(string text, bool isError)
    {
        if (IsDisposed) return;
        lblSummary.Text = text;
        lblSummary.ForeColor = isError ? ColorTranslator.FromHtml("#8F2A1B") : Theme.Muted;
    }

    private PayoutListItem? SelectedPayout =>
        listPayouts.SelectedItems.Count > 0 ? listPayouts.SelectedItems[0].Tag as PayoutListItem : null;

    private void FitColumns()
    {
        if (listPayouts.Columns.Count != 8) return;
        colKind.Width = LogicalToDeviceUnits(120);
        colRecipient.Width = LogicalToDeviceUnits(120);
        colContact.Width = LogicalToDeviceUnits(150);
        colParcel.Width = LogicalToDeviceUnits(110);
        colAmount.Width = LogicalToDeviceUnits(100);
        colStatus.Width = LogicalToDeviceUnits(110);
        colCreated.Width = LogicalToDeviceUnits(125);
        var used = colKind.Width + colRecipient.Width + colContact.Width + colParcel.Width
            + colAmount.Width + colStatus.Width + colCreated.Width;
        colPaid.Width = Math.Max(0, listPayouts.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - used);
    }

    private static string Peso(decimal amount) => $"₱{amount.ToString("N2", CultureInfo.InvariantCulture)}";
}

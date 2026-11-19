using System.ComponentModel;
using System.Globalization;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.Presenters;
using WayPlot.UI;

namespace WayPlot.Views;

public partial class DealsView : UserControl, IDealsView
{
    private DealsPresenter? _presenter;

    public DealsView()
    {
        InitializeComponent();
        ApplyTheme();

        btnRequestClosed.Click += (_, _) => AsyncUiEvent.Run(this, RequestClosedAsync, "Could not submit the Closed request. Try again.");
        btnConfirmClosed.Click += (_, _) => AsyncUiEvent.Run(this, ConfirmClosedAsync, "Could not confirm the deal. Try again.");
        btnReturnRequest.Click += (_, _) => AsyncUiEvent.Run(this, ReturnRequestAsync, "Could not return the request. Try again.");
        btnCancelDeal.Click += (_, _) => AsyncUiEvent.Run(this, CancelDealAsync, "Could not cancel the deal. Try again.");
        btnDefaultDeal.Click += (_, _) => AsyncUiEvent.Run(this, DefaultDealAsync, "Could not default the deal. Try again.");
        btnRecordPayment.Click += (_, _) => AsyncUiEvent.Run(this, RecordPaymentAsync, "Could not record the payment. Try again.");
        btnConfirmPayment.Click += (_, _) => AsyncUiEvent.Run(this, ConfirmPaymentAsync, "Could not confirm the payment. Try again.");
        btnReturnPayment.Click += (_, _) => AsyncUiEvent.Run(this, ReturnPaymentAsync, "Could not return the payment. Try again.");
        listDeals.SelectedIndexChanged += (_, _) => AsyncUiEvent.Run(this, DealSelectionChangedAsync, "Could not load the deal. Try again.");
        listPayments.SelectedIndexChanged += (_, _) => UpdateActionButtons();
        listDeals.Resize += (_, _) => FitColumns();
        listPayments.Resize += (_, _) => FitPaymentColumns();
        FitColumns();
        FitPaymentColumns();
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DealsPresenter? Presenter
    {
        set
        {
            _presenter = value;
            UpdateActionButtons();
        }
    }

    protected override async void OnLoad(EventArgs e)
    {
        try
        {
            base.OnLoad(e);
            if (DesignMode) return;
            if (_presenter is null)
            {
                UpdateActionButtons();
                return;
            }

            UpdateActionButtons();
            await _presenter.LoadAsync();
        }
        catch (Exception)
        {
            if (!IsDisposed)
                ShowStatus("Could not load deals. Try again.", true);
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
        lblPaymentSummary.Font = Theme.Small;
        lblPaymentSummary.ForeColor = Theme.Muted;
        lblPayments.Font = Theme.BodyBold;
        lblPayments.ForeColor = Theme.Navy;
        listDeals.Font = Theme.Body;
        listDeals.BackColor = Theme.Surface;
        listDeals.ForeColor = Theme.Text;
        listPayments.Font = Theme.Body;
        listPayments.BackColor = Theme.Surface;
        listPayments.ForeColor = Theme.Text;
        ButtonStyle.ApplyAvailability(
        [
            new(btnRequestClosed, true, true, null, true),
            new(btnConfirmClosed, true, true, null, false),
            new(btnReturnRequest, true, true, null, false),
            new(btnCancelDeal, true, true, null, false),
            new(btnDefaultDeal, true, true, null, false),
            new(btnRecordPayment, true, true, null, true),
            new(btnConfirmPayment, true, true, null, false),
            new(btnReturnPayment, true, true, null, false)
        ], actionToolTip);
    }

    private async Task RequestClosedAsync()
    {
        if (_presenter is null) return;
        var candidates = await _presenter.GetCandidatesAsync();
        if (IsDisposed) return;
        if (!candidates.Ok || candidates.Value is null)
        {
            ShowStatus(candidates.Message, true);
            return;
        }
        if (candidates.Value.Count == 0)
        {
            ShowStatus("No leads are currently eligible for a Closed request.", false);
            return;
        }

        using var dialog = new CreateDealDialog
        {
            Candidates = candidates.Value,
            SaveHandler = _presenter.RequestClosedAsync
        };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.Result is not { } result || !result.Ok)
            return;

        await _presenter.LoadAsync(result.Message);
    }

    private async Task ConfirmClosedAsync()
    {
        if (_presenter is null || SelectedDealId is not { } dealId) return;
        using var dialog = new ConfirmClosedDialog();
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;

        var result = await _presenter.ConfirmClosedAsync(dealId, dialog.SelectedStatus);
        if (IsDisposed) return;
        if (!result.Ok)
        {
            ShowStatus(result.Message, true);
            return;
        }

        await _presenter.LoadAsync(result.Message);
    }

    private async Task ReturnRequestAsync()
    {
        if (_presenter is null || SelectedDealId is not { } dealId) return;
        if (MessageBox.Show(
                FindForm(),
                "Return this pending Closed request? The parcel will become Available.",
                "Return request",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        var result = await _presenter.ReturnRequestAsync(dealId);
        if (IsDisposed) return;
        if (!result.Ok)
        {
            ShowStatus(result.Message, true);
            return;
        }

        await _presenter.LoadAsync(result.Message);
    }

    private async Task CancelDealAsync()
    {
        await TerminateDealAsync(defaulted: false);
    }

    private async Task DefaultDealAsync()
    {
        await TerminateDealAsync(defaulted: true);
    }

    private async Task TerminateDealAsync(bool defaulted)
    {
        var presenter = _presenter;
        if (presenter is null || SelectedDealId is not { } dealId) return;

        var summary = await presenter.GetTerminationSummaryAsync(dealId);
        if (IsDisposed) return;
        if (!summary.Ok || summary.Value is null)
        {
            ShowStatus(summary.Message, true);
            return;
        }

        using var dialog = new TerminateDealDialog(defaulted, summary.Value);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.Result is not { } input)
            return;

        var result = defaulted
            ? await presenter.DefaultDealAsync(dealId, input.Reason, input.MoneyOutcome, input.RefundedAmount)
            : await presenter.CancelDealAsync(dealId, input.Reason, input.MoneyOutcome, input.RefundedAmount);
        if (IsDisposed) return;
        if (!result.Ok)
        {
            ShowStatus(result.Message, true);
            return;
        }
        await presenter.LoadAsync(result.Message);
    }

    private async Task DealSelectionChangedAsync()
    {
        UpdateActionButtons();
        if (_presenter is null) return;
        if (SelectedDealId is { } dealId)
            await _presenter.LoadPaymentsAsync(dealId);
        else
            _presenter.ClearPayments("Select a deal to see its payments.");
    }

    private async Task RecordPaymentAsync()
    {
        var presenter = _presenter;
        if (presenter is null || SelectedDeal is not { } deal) return;
        using var dialog = new RecordPaymentDialog
        {
            Deal = deal,
            SaveHandler = presenter.RecordPaymentAsync
        };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.Result is not { } result || !result.Ok)
            return;

        await presenter.LoadAsync(result.Message);
        await presenter.LoadPaymentsAsync(deal.Id);
    }

    private async Task ConfirmPaymentAsync()
    {
        if (_presenter is null || SelectedPaymentId is not { } paymentId || SelectedDealId is not { } dealId)
            return;
        var result = await _presenter.ConfirmPaymentAsync(paymentId);
        if (IsDisposed) return;
        if (!result.Ok)
        {
            ShowPaymentStatus(result.Message, true);
            return;
        }

        await _presenter.LoadAsync(result.Message);
        await _presenter.LoadPaymentsAsync(dealId);
    }

    private async Task ReturnPaymentAsync()
    {
        var presenter = _presenter;
        if (presenter is null || SelectedPaymentId is not { } paymentId || SelectedDealId is not { } dealId)
            return;
        using var dialog = new ReturnPaymentDialog
        {
            SaveHandler = note => presenter.ReturnPaymentAsync(paymentId, note)
        };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.Result is not { } result || !result.Ok)
            return;

        await presenter.LoadAsync(result.Message);
        await presenter.LoadPaymentsAsync(dealId);
    }

    public void ShowDeals(IReadOnlyList<DealListItem> deals)
    {
        if (IsDisposed) return;

        var selectedDealId = SelectedDealId;
        listDeals.BeginUpdate();
        listDeals.Items.Clear();
        foreach (var deal in deals)
        {
            var status = deal.Status == DealStatus.Current
                ? deal.IsConfirmed ? "Current — confirmed" : "Current — pending"
                : EnumText.Pretty(deal.Status);
            var item = new ListViewItem(deal.ContactName) { Tag = deal };
            item.SubItems.Add(deal.ParcelLabel);
            item.SubItems.Add(Peso(deal.Price));
            item.SubItems.Add(Peso(deal.DownPayment));
            item.SubItems.Add(deal.Status == DealStatus.Cancelled || deal.ReservationFee == 0
                ? "—"
                : deal.ReservationFeeDue == 0 ? "Paid" : Peso(deal.ReservationFeeDue));
            item.SubItems.Add(deal.Status == DealStatus.Cancelled ? "—" : Peso(deal.RemainingBalance));
            item.SubItems.Add(status);
            item.SubItems.Add(deal.CreatedAtUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture));
            listDeals.Items.Add(item);
            if (selectedDealId == deal.Id) item.Selected = true;
        }
        listDeals.EndUpdate();
        FitColumns();
        UpdateActionButtons();
    }

    public void ShowPayments(IReadOnlyList<PaymentListItem> payments)
    {
        if (IsDisposed) return;

        listPayments.BeginUpdate();
        listPayments.Items.Clear();
        foreach (var payment in payments)
        {
            var item = new ListViewItem(payment.PaidAtUtc.ToLocalTime().ToString("g", CultureInfo.CurrentCulture))
            {
                Tag = payment,
                ToolTipText = payment.ReviewNote ?? string.Empty
            };
            item.SubItems.Add(payment.ReceiptNo);
            item.SubItems.Add(Peso(payment.Amount));
            item.SubItems.Add(EnumText.Pretty(payment.Method));
            item.SubItems.Add(EnumText.Pretty(payment.Status));
            item.SubItems.Add(string.IsNullOrWhiteSpace(payment.ReviewNote) ? "—" : payment.ReviewNote);
            listPayments.Items.Add(item);
        }
        listPayments.EndUpdate();
        FitPaymentColumns();
        UpdateActionButtons();
    }

    public void ShowStatus(string text, bool isError)
    {
        if (IsDisposed) return;
        lblSummary.Text = text;
        lblSummary.ForeColor = isError ? ColorTranslator.FromHtml("#8F2A1B") : Theme.Muted;
    }

    public void ShowPaymentStatus(string text, bool isError)
    {
        if (IsDisposed) return;
        lblPaymentSummary.Text = text;
        lblPaymentSummary.ForeColor = isError ? ColorTranslator.FromHtml("#8F2A1B") : Theme.Muted;
    }

    private Guid? SelectedDealId =>
        SelectedDeal?.Id;

    private DealListItem? SelectedDeal =>
        listDeals.SelectedItems.Count > 0 && listDeals.SelectedItems[0].Tag is DealListItem deal ? deal : null;

    private Guid? SelectedPaymentId =>
        SelectedPayment?.Id;

    private PaymentListItem? SelectedPayment =>
        listPayments.SelectedItems.Count > 0 && listPayments.SelectedItems[0].Tag is PaymentListItem payment ? payment : null;

    private void UpdateActionButtons()
    {
        var unavailable = new ActionAvailability(false, false, null);
        var actions = _presenter?.GetActionStates(SelectedDeal, SelectedPayment)
            ?? new DealActionStates(
                unavailable, unavailable, unavailable, unavailable,
                unavailable, unavailable, unavailable, unavailable);

        ButtonStyle.ApplyAvailability(
        [
            new(btnRequestClosed, actions.RequestClosed.Visible, actions.RequestClosed.Enabled, actions.RequestClosed.DisabledReason, true),
            new(btnConfirmClosed, actions.ConfirmClosed.Visible, actions.ConfirmClosed.Enabled, actions.ConfirmClosed.DisabledReason, false),
            new(btnReturnRequest, actions.ReturnRequest.Visible, actions.ReturnRequest.Enabled, actions.ReturnRequest.DisabledReason, false),
            new(btnCancelDeal, actions.CancelDeal.Visible, actions.CancelDeal.Enabled, actions.CancelDeal.DisabledReason, false),
            new(btnDefaultDeal, actions.DefaultDeal.Visible, actions.DefaultDeal.Enabled, actions.DefaultDeal.DisabledReason, false),
            new(btnRecordPayment, actions.RecordPayment.Visible, actions.RecordPayment.Enabled, actions.RecordPayment.DisabledReason, true),
            new(btnConfirmPayment, actions.ConfirmPayment.Visible, actions.ConfirmPayment.Enabled, actions.ConfirmPayment.DisabledReason, false),
            new(btnReturnPayment, actions.ReturnPayment.Visible, actions.ReturnPayment.Enabled, actions.ReturnPayment.DisabledReason, false)
        ], actionToolTip);
    }

    private void FitColumns()
    {
        if (listDeals.Columns.Count < 8) return;
        colContact.Width = LogicalToDeviceUnits(140);
        colParcel.Width = LogicalToDeviceUnits(120);
        colPrice.Width = LogicalToDeviceUnits(110);
        colDownPayment.Width = LogicalToDeviceUnits(110);
        colReservationDue.Width = LogicalToDeviceUnits(120);
        colRemaining.Width = LogicalToDeviceUnits(120);
        colStatus.Width = LogicalToDeviceUnits(130);
        var used = colContact.Width + colParcel.Width + colPrice.Width + colDownPayment.Width + colReservationDue.Width
            + colRemaining.Width + colStatus.Width;
        colCreated.Width = Math.Max(0, listDeals.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - used);
    }

    private void FitPaymentColumns()
    {
        if (listPayments.Columns.Count < 6) return;
        colPaymentDate.Width = LogicalToDeviceUnits(135);
        colReceipt.Width = LogicalToDeviceUnits(180);
        colAmount.Width = LogicalToDeviceUnits(130);
        colMethod.Width = LogicalToDeviceUnits(120);
        colPaymentStatus.Width = LogicalToDeviceUnits(130);
        var used = colPaymentDate.Width + colReceipt.Width + colAmount.Width + colMethod.Width + colPaymentStatus.Width;
        colReviewNote.Width = Math.Max(0, listPayments.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - used);
    }

    private static string Peso(decimal amount) => "₱" + amount.ToString("N2", CultureInfo.InvariantCulture);
}

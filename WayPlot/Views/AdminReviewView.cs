using System.ComponentModel;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.Presenters;
using WayPlot.UI;

namespace WayPlot.Views;

public partial class AdminReviewView : UserControl
{
    private AdminReviewPresenter? _presenter;
    private ReviewEntry? _selectedEntry;

    private abstract record ReviewEntry(string Display);
    private sealed record ClosedEntry(DealListItem Deal, DealSummaryDto Summary)
        : ReviewEntry($"{Deal.ContactName} — {Deal.ParcelLabel}");
    private sealed record PaymentEntry(ReviewPaymentListItem Item)
        : ReviewEntry($"{Item.ContactName} — {Item.ParcelLabel} — {Item.Payment.ReceiptNo}");
    private sealed record PayoutEntry(PayoutListItem Item)
        : ReviewEntry($"{EnumText.Pretty(Item.Kind)} — {Item.ContactName} — {Item.ParcelLabel} — {Item.RecipientName}");

    public AdminReviewView()
    {
        InitializeComponent();
        ApplyTheme();
        btnRefresh.Click += (_, _) => AsyncUiEvent.Run(this, () => LoadAsync(), "Could not refresh the review queue. Try again.");
        btnConfirmClosed.Click += (_, _) => AsyncUiEvent.Run(this, ConfirmClosedAsync, "Could not confirm the request. Try again.");
        btnReturnClosed.Click += (_, _) => AsyncUiEvent.Run(this, ReturnClosedAsync, "Could not return the request. Try again.");
        btnConfirmPayment.Click += (_, _) => AsyncUiEvent.Run(this, ConfirmPaymentAsync, "Could not confirm the payment. Try again.");
        btnReturnPayment.Click += (_, _) => AsyncUiEvent.Run(this, ReturnPaymentAsync, "Could not return the payment. Try again.");
        btnApprove.Click += (_, _) => AsyncUiEvent.Run(this, ApproveAsync, "Could not approve the payout. Try again.");
        btnMarkPaid.Click += (_, _) => AsyncUiEvent.Run(this, MarkPaidAsync, "Could not mark the payout as paid. Try again.");
        listQueue.SelectedIndexChanged += (_, _) => SelectionChanged();
        detailCard.ConfirmRequested = _ => AsyncUiEvent.Run(this, ConfirmClosedAsync, "Could not confirm the request. Try again.");
        detailCard.ReturnRequested = _ => AsyncUiEvent.Run(this, ReturnClosedAsync, "Could not return the request. Try again.");
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public AdminReviewPresenter? Presenter
    {
        set => _presenter = value;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (DesignMode || _presenter is null) return;
        AsyncUiEvent.Run(this, () => LoadAsync(), "The review queue could not be loaded. Try again.");
    }

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        lblTitle.Font = Theme.Heading;
        lblTitle.ForeColor = Theme.Navy;
        lblStatus.Font = Theme.Small;
        lblStatus.ForeColor = Theme.Muted;
        listQueue.Font = Theme.Body;
        listQueue.BackColor = Theme.Surface;
        listQueue.ForeColor = Theme.Text;
        foreach (var button in new[] { btnRefresh, btnConfirmClosed, btnReturnClosed, btnConfirmPayment,
                     btnReturnPayment, btnApprove, btnMarkPaid })
            ButtonStyle.Apply(button, button == btnRefresh || button == btnConfirmClosed || button == btnMarkPaid);
        UpdateActions();
    }

    private async Task LoadAsync(string? notice = null)
    {
        if (_presenter is null) return;
        ShowStatus("Loading review queue...", false);
        var result = await _presenter.LoadAsync();
        if (IsDisposed) return;
        if (!result.Ok || result.Value is null)
        {
            ShowStatus(result.Message, true);
            return;
        }

        listQueue.BeginUpdate();
        listQueue.Items.Clear();
        listQueue.Groups.Clear();
        var closedGroup = new ListViewGroup($"Closed requests waiting for confirmation ({result.Value.ClosedRequests.Count})");
        var paymentGroup = new ListViewGroup($"Payments waiting for confirmation ({result.Value.PendingPayments.Count})");
        var payoutGroup = new ListViewGroup($"Commissions and referral bonuses pending or approved ({result.Value.UnpaidPayouts.Count})");
        listQueue.Groups.AddRange([closedGroup, paymentGroup, payoutGroup]);
        foreach (var closed in result.Value.ClosedRequests)
            AddRow(new ClosedEntry(closed.Deal, closed.Summary), closedGroup, "Closed request");
        foreach (var payment in result.Value.PendingPayments)
            AddRow(new PaymentEntry(payment), paymentGroup,
                $"{payment.Payment.ReceiptNo} — {payment.Payment.Amount.ToString("N2")} — {payment.Payment.Status}");
        foreach (var payout in result.Value.UnpaidPayouts)
            AddRow(new PayoutEntry(payout), payoutGroup,
                $"{EnumText.Pretty(payout.Kind)} — {payout.Amount.ToString("N2")} — {EnumText.Pretty(payout.Status)}");
        listQueue.EndUpdate();
        ShowStatus(notice ?? Summary(result.Value), false);
        SelectionChanged();
    }

    private void AddRow(ReviewEntry entry, ListViewGroup group, string status)
    {
        var item = new ListViewItem(entry.Display, group) { Tag = entry };
        item.SubItems.Add(status);
        listQueue.Items.Add(item);
    }

    private void SelectionChanged()
    {
        _selectedEntry = listQueue.SelectedItems.Count > 0
            ? listQueue.SelectedItems[0].Tag as ReviewEntry
            : null;
        detailHost.Controls.Clear();
        if (_selectedEntry is ClosedEntry closed)
        {
            detailCard.Deal = closed.Summary;
            detailCard.LoadPayments = async dealId =>
            {
                var result = await _presenter!.GetPaymentsAsync(dealId);
                if (!result.Ok || result.Value is null)
                    throw new InvalidOperationException(result.Message);
                return result.Value;
            };
            detailHost.Controls.Add(detailCard);
        }
        UpdateActions();
    }

    private void UpdateActions()
    {
        var closed = _selectedEntry is ClosedEntry;
        var payment = _selectedEntry is PaymentEntry;
        var payout = (_selectedEntry as PayoutEntry)?.Item;
        ButtonStyle.ApplyAvailability(
        [
            new(btnConfirmClosed, true, closed,
                closed ? null : "Select a Closed request waiting for confirmation.", true),
            new(btnReturnClosed, true, closed,
                closed ? null : "Select a Closed request waiting for confirmation.", false),
            new(btnConfirmPayment, true, payment,
                payment ? null : "Select a payment waiting for confirmation.", false),
            new(btnReturnPayment, true, payment,
                payment ? null : "Select a payment waiting for confirmation.", false),
            new(btnApprove, true, payout?.Status == CommissionStatus.Pending,
                payout is null ? "Select a commission or referral bonus."
                    : payout.Status != CommissionStatus.Pending ? "Only a pending payout can be approved." : null, false),
            new(btnMarkPaid, true, payout?.Status == CommissionStatus.Approved && payout.CanMarkPaid,
                payout is null ? "Select a commission or referral bonus."
                    : payout.Status != CommissionStatus.Approved ? "Only an approved payout can be marked Paid."
                    : payout.PaidBlockedReason, true)
        ], actionToolTip);
    }

    private async Task ConfirmClosedAsync()
    {
        if (_presenter is null || _selectedEntry is not ClosedEntry entry) return;
        try
        {
            using var dialog = new ConfirmClosedDialog();
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
            var result = await _presenter.ConfirmClosedAsync(entry.Deal.Id, dialog.SelectedStatus);
            if (IsDisposed) return;
            if (!result.Ok) { ShowStatus(result.Message, true); return; }
            await LoadAsync(result.Message);
        }
        catch (Exception) { if (!IsDisposed) ShowStatus("The request could not be confirmed. Reload and try again.", true); }
    }

    private async Task ReturnClosedAsync()
    {
        if (_presenter is null || _selectedEntry is not ClosedEntry entry) return;
        try
        {
            if (MessageBox.Show(FindForm(), "Return this Closed request to Negotiation?",
                    "Return request", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            var result = await _presenter.ReturnClosedAsync(entry.Deal.Id);
            if (IsDisposed) return;
            if (!result.Ok) { ShowStatus(result.Message, true); return; }
            await LoadAsync(result.Message);
        }
        catch (Exception) { if (!IsDisposed) ShowStatus("The request could not be returned. Reload and try again.", true); }
    }

    private async Task ConfirmPaymentAsync()
    {
        if (_presenter is null || _selectedEntry is not PaymentEntry entry) return;
        try
        {
            var result = await _presenter.ConfirmPaymentAsync(entry.Item.Payment.Id);
            if (IsDisposed) return;
            if (!result.Ok) { ShowStatus(result.Message, true); return; }
            await LoadAsync(result.Message);
        }
        catch (Exception) { if (!IsDisposed) ShowStatus("The payment could not be confirmed. Reload and try again.", true); }
    }

    private async Task ReturnPaymentAsync()
    {
        if (_presenter is null || _selectedEntry is not PaymentEntry entry) return;
        try
        {
            using var dialog = new ReturnPaymentDialog
            {
                SaveHandler = note => _presenter.ReturnPaymentAsync(entry.Item.Payment.Id, note)
            };
            if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.Result is not { Ok: true } result)
            {
                if (dialog.Result is { Ok: false } failed) ShowStatus(failed.Message, true);
                return;
            }
            await LoadAsync(result.Message);
        }
        catch (Exception) { if (!IsDisposed) ShowStatus("The payment could not be returned. Reload and try again.", true); }
    }

    private async Task ApproveAsync()
    {
        if (_presenter is null || _selectedEntry is not PayoutEntry entry) return;
        try
        {
            if (MessageBox.Show(FindForm(), "Approve this payout?", "Approve payout",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            var result = await _presenter.ApproveAsync(entry.Item);
            if (IsDisposed) return;
            if (!result.Ok) { ShowStatus(result.Message, true); return; }
            await LoadAsync(result.Message);
        }
        catch (Exception) { if (!IsDisposed) ShowStatus("The payout could not be approved. Reload and try again.", true); }
    }

    private async Task MarkPaidAsync()
    {
        if (_presenter is null || _selectedEntry is not PayoutEntry entry) return;
        try
        {
            if (MessageBox.Show(FindForm(), "Record this payout as Paid today?", "Mark payout Paid",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            var result = await _presenter.MarkPaidAsync(entry.Item);
            if (IsDisposed) return;
            if (!result.Ok) { ShowStatus(result.Message, true); return; }
            await LoadAsync(result.Message);
        }
        catch (Exception) { if (!IsDisposed) ShowStatus("The payout could not be marked Paid. Reload and try again.", true); }
    }

    private void ShowStatus(string message, bool isError)
    {
        if (IsDisposed) return;
        lblStatus.Text = message;
        lblStatus.ForeColor = isError ? ColorTranslator.FromHtml("#8F2A1B") : Theme.Muted;
    }

    private static string Summary(AdminReviewData data) =>
        $"{data.ClosedRequests.Count} Closed request(s), {data.PendingPayments.Count} payment(s), " +
        $"{data.UnpaidPayouts.Count} unpaid payout(s) awaiting review.";
}

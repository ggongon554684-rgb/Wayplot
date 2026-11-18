using System.Globalization;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.UI;

namespace WayPlot.Views;

public sealed record TerminateDealDialogResult(
    string Reason,
    DealMoneyOutcome MoneyOutcome,
    decimal? RefundedAmount);

public partial class TerminateDealDialog : Form
{
    private readonly DealTerminationSummary _summary;

    public TerminateDealDialog(bool defaulted, DealTerminationSummary summary)
    {
        InitializeComponent();
        _summary = summary;
        Text = defaulted ? "Default deal" : "Cancel deal";
        lblTitle.Text = defaulted ? "Default deal" : "Cancel deal";
        btnSave.Text = defaulted ? "Default deal" : "Cancel deal";
        lblPaidTotal.Text = $"Confirmed payments: {Peso(summary.ConfirmedPaymentsTotal)}";
        lblEffects.Text =
            $"The parcel returns to Available. {summary.UnpaidPayoutCount} unpaid commission(s) or referral bonus(es) will be cancelled. " +
            $"{summary.PaidPayoutCount} paid commission(s) or referral bonus(es) will be flagged for review.";
        nudRefundAmount.Maximum = summary.ConfirmedPaymentsTotal;
        rdoRefunded.CheckedChanged += (_, _) => UpdateRefundAmount();
        rdoPartlyRefunded.CheckedChanged += (_, _) => UpdateRefundAmount();
        rdoKept.CheckedChanged += (_, _) => UpdateRefundAmount();
        btnSave.Click += (_, _) => Save();
        ApplyTheme();
    }

    public TerminateDealDialogResult? Result { get; private set; }

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        Font = Theme.Body;
        lblTitle.Font = Theme.Heading;
        lblTitle.ForeColor = Theme.Navy;
        lblPaidTotal.Font = Theme.BodyBold;
        lblPaidTotal.ForeColor = Theme.Text;
        lblReason.Font = Theme.Small;
        lblReason.ForeColor = Theme.Muted;
        lblRefundAmount.Font = Theme.Small;
        lblRefundAmount.ForeColor = Theme.Muted;
        lblEffects.Font = Theme.Small;
        lblEffects.ForeColor = Theme.Muted;
        lblError.ForeColor = ColorTranslator.FromHtml("#8F2A1B");
        txtReason.Font = Theme.Body;
        nudRefundAmount.Font = Theme.Body;
        rdoRefunded.Font = Theme.Body;
        rdoPartlyRefunded.Font = Theme.Body;
        rdoKept.Font = Theme.Body;
        ButtonStyle.Apply(btnSave, primary: true);
        ButtonStyle.Apply(btnCancel, primary: false);
    }

    private void UpdateRefundAmount()
    {
        var partial = rdoPartlyRefunded.Checked;
        lblRefundAmount.Enabled = partial;
        nudRefundAmount.Enabled = partial;
    }

    private void Save()
    {
        lblError.Visible = false;
        var reason = txtReason.Text.Trim();
        if (reason.Length == 0)
        {
            ShowError("Enter a reason.");
            return;
        }
        if (reason.Length > 500)
        {
            ShowError("The reason cannot exceed 500 characters.");
            return;
        }

        DealMoneyOutcome outcome;
        if (rdoRefunded.Checked)
            outcome = DealMoneyOutcome.Refunded;
        else if (rdoPartlyRefunded.Checked)
            outcome = DealMoneyOutcome.PartlyRefunded;
        else if (rdoKept.Checked)
            outcome = DealMoneyOutcome.Kept;
        else
        {
            ShowError("Choose what should happen to money already paid.");
            return;
        }

        decimal? refundAmount = null;
        if (outcome == DealMoneyOutcome.PartlyRefunded)
        {
            if (nudRefundAmount.Value <= 0 || nudRefundAmount.Value >= _summary.ConfirmedPaymentsTotal)
            {
                ShowError("A partial refund must be greater than zero and less than the confirmed payments total.");
                return;
            }
            refundAmount = nudRefundAmount.Value;
        }

        Result = new TerminateDealDialogResult(reason, outcome, refundAmount);
        DialogResult = DialogResult.OK;
    }

    private void ShowError(string message)
    {
        lblError.Text = message;
        lblError.Visible = true;
    }

    private static string Peso(decimal amount) =>
        "₱" + amount.ToString("N2", CultureInfo.InvariantCulture);
}

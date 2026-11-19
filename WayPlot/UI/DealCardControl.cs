using System.ComponentModel;
using System.Globalization;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;

namespace WayPlot.UI;

public partial class DealCardControl : UserControl
{
    private DealSummaryDto _deal = null!;

    public DealCardControl()
    {
        InitializeComponent();
        ApplyTheme();
        btnPayments.Click += (_, _) => TogglePayments();
        btnShowMap.Click += (_, _) => ShowMapRequested?.Invoke(_deal.ParcelId);
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DealSummaryDto Deal
    {
        get => _deal;
        set
        {
            _deal = value;
            Render();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<Guid, Task<IReadOnlyList<DealPaymentDto>>>? LoadPayments { get; set; }
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<Guid>? ShowMapRequested { get; set; }
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<DealSummaryDto>? RecordPaymentRequested { get; set; }
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<DealSummaryDto>? ConfirmRequested { get; set; }
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<DealSummaryDto>? ReturnRequested { get; set; }
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<DealSummaryDto>? CancelRequestRequested { get; set; }
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<DealSummaryDto>? CancelRequested { get; set; }
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<DealSummaryDto>? DefaultRequested { get; set; }
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<DealPaymentDto>? ConfirmPaymentRequested { get; set; }
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<DealPaymentDto>? ReturnPaymentRequested { get; set; }

    private void ApplyTheme()
    {
        BackColor = Theme.Surface;
        lblParcel.Font = Theme.BodyBold;
        lblParcel.ForeColor = Theme.Navy;
        lblStatus.Font = Theme.Small;
        lblStatus.ForeColor = Theme.Text;
        lblMoney.Font = Theme.Heading;
        lblMoney.ForeColor = Theme.Navy;
        lblNet.Font = Theme.Small;
        lblNet.ForeColor = Theme.Muted;
        lblNext.Font = Theme.Small;
        lblNext.ForeColor = Theme.Text;
        lblOverdue.Font = Theme.Small;
        lblOverdue.ForeColor = Color.FromArgb(143, 42, 27);
        lblPenalty.Font = Theme.Small;
        lblPenalty.ForeColor = Theme.Muted;
        lblTermination.Font = Theme.Small;
        lblTermination.ForeColor = Theme.Muted;
        ButtonStyle.Apply(btnShowMap, false);
        ButtonStyle.Apply(btnPayments, false);
    }

    private void Render()
    {
        if (_deal is null) return;
        actions.Controls.Clear();
        actions.Controls.Add(btnPayments);

        if (!string.IsNullOrEmpty(_deal.AgentName))
        {
            // Agent name is displayed in the card header context where space permits.
        }
        lblParcel.Text = _deal.ParcelLabel;
        lblNet.Text = $"Net price: {Peso(_deal.NetPrice)}";
        lblMoney.Text = $"{Peso(_deal.Balance)} remaining";
        moneyBar.Paid = _deal.ConfirmedPaid;
        moneyBar.Total = _deal.NetPrice;
        moneyBar.BarColor = StatusColor(_deal);

        var status = DealStatusText(_deal);
        lblStatus.Text = status.Icon + " " + status.Text;
        lblStatus.ForeColor = status.Color;

        lblNext.Text = _deal.NextDueDate is { } due && _deal.NextDueAmount is { } amount
            ? $"Next payment {Peso(amount)} due {due:dd MMM yyyy}"
            : _deal.IsPendingConfirmation
                ? "No schedule until confirmation"
                : "No next payment";
        lblNext.Visible = !_deal.IsPendingConfirmation || lblNext.Text.Length > 0;
        lblOverdue.Text = _deal.OverdueAmount > 0
            ? $"{Peso(_deal.OverdueAmount)} overdue for {_deal.DaysOverdue} days"
            : "";
        lblOverdue.Visible = _deal.OverdueAmount > 0;
        lblPenalty.Text = _deal.LatePenaltyPercentPerMonth > 0
            ? $"Late penalty: {_deal.LatePenaltyPercentPerMonth:N0}% per month"
            : "";
        lblPenalty.Visible = !string.IsNullOrEmpty(lblPenalty.Text);

        lblTermination.Text = _deal.Status is DealStatus.Cancelled or DealStatus.Defaulted
            ? $"Reason: {_deal.TerminationReason ?? "No reason stored"}\n" +
              $"Money outcome: {_deal.TerminationMoneyOutcome?.ToString() ?? "Not stored"}" +
              (_deal.RefundedAmount is { } refund ? $"\nRefunded: {Peso(refund)}" : "")
            : "";
        lblTermination.Visible = lblTermination.Text.Length > 0;

        if (RecordPaymentRequested is not null && !_deal.IsPendingConfirmation
            && _deal.Status is DealStatus.Current or DealStatus.Overdue)
            AddAction("Record payment", () => RecordPaymentRequested(_deal));
        if (ConfirmRequested is not null && _deal.IsPendingConfirmation)
            AddAction("Confirm", () => ConfirmRequested(_deal));
        if (ReturnRequested is not null && _deal.IsPendingConfirmation)
            AddAction("Return request", () => ReturnRequested(_deal));
        if (CancelRequestRequested is not null && _deal.IsPendingConfirmation)
            AddAction("Cancel request", () => CancelRequestRequested(_deal));
        if (CancelRequested is not null && !_deal.IsPendingConfirmation
            && _deal.Status is DealStatus.Current or DealStatus.Overdue)
            AddAction("Cancel", () => CancelRequested(_deal));
        if (DefaultRequested is not null && !_deal.IsPendingConfirmation
            && _deal.Status is DealStatus.Current or DealStatus.Overdue)
            AddAction("Default", () => DefaultRequested(_deal));

        btnPayments.Text = paymentsExpanded ? "Hide payments" : "Payments";
        paymentsPanel.Visible = paymentsExpanded;
        BackColor = _deal.Status is DealStatus.Cancelled or DealStatus.Defaulted
            ? Color.FromArgb(248, 248, 248) : Theme.Surface;
    }

    private bool paymentsExpanded;

    private void AddAction(string text, Action action)
    {
        var button = new Button { Text = text, AutoSize = true, Margin = new Padding(4, 0, 4, 0) };
        ButtonStyle.Apply(button, false);
        button.Click += (_, _) => action();
        actions.Controls.Add(button);
    }

    private async void TogglePayments()
    {
        try
        {
            paymentsExpanded = !paymentsExpanded;
            btnPayments.Text = paymentsExpanded ? "Hide payments" : "Payments";
            paymentsPanel.Visible = paymentsExpanded;
            if (!paymentsExpanded || LoadPayments is null) return;

            var payments = await LoadPayments(_deal.DealId);
            paymentsList.Controls.Clear();
            foreach (var payment in payments.OrderByDescending(payment => payment.PaidAtUtc))
                AddPayment(payment);
        }
        catch (Exception ex)
        {
            AppLog.Error("DealCardControl.TogglePayments", ex);
            if (IsDisposed) return;
            paymentsList.Controls.Clear();
            paymentsList.Controls.Add(new Label
            {
                Text = "Payments could not be loaded.",
                ForeColor = Color.FromArgb(143, 42, 27),
                AutoSize = true,
                Margin = new Padding(8)
            });
        }
    }

    private void AddPayment(DealPaymentDto payment)
    {
        var panel = new TableLayoutPanel
        {
            ColumnCount = 1,
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(8),
            Margin = new Padding(0, 0, 0, 6),
            BackColor = Theme.Page
        };
        var statusText = payment.Status switch
        {
            PaymentStatus.AwaitingAdmin => "AwaitingAdmin",
            PaymentStatus.Confirmed => "Confirmed",
            _ => "Returned"
        };
        panel.Controls.Add(new Label
        {
            Text = $"{payment.PaidAtUtc.ToLocalTime():dd MMM yyyy} — {Peso(payment.Amount)} — " +
                   $"{EnumText.Pretty(payment.Method)} — Receipt {payment.ReceiptNo}",
            AutoSize = true,
            MaximumSize = new Size(420, 0),
            ForeColor = Theme.Text
        });
        panel.Controls.Add(new Label
        {
            Text = statusText + (string.IsNullOrWhiteSpace(payment.ReviewNote)
                ? "" : $" — {payment.ReviewNote}"),
            AutoSize = true,
            MaximumSize = new Size(420, 0),
            ForeColor = Theme.Muted
        });
        if (payment.Status == PaymentStatus.AwaitingAdmin)
        {
            var paymentActions = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight
            };
            if (ConfirmPaymentRequested is not null)
            {
                var confirm = new Button { Text = "Confirm", AutoSize = true };
                ButtonStyle.Apply(confirm, false);
                confirm.Click += (_, _) => ConfirmPaymentRequested(payment);
                paymentActions.Controls.Add(confirm);
            }
            if (ReturnPaymentRequested is not null)
            {
                var returned = new Button { Text = "Return", AutoSize = true };
                ButtonStyle.Apply(returned, false);
                returned.Click += (_, _) => ReturnPaymentRequested(payment);
                paymentActions.Controls.Add(returned);
            }
            panel.Controls.Add(paymentActions);
        }
        paymentsList.Controls.Add(panel);
    }

    private static string Peso(decimal amount) =>
        "₱" + amount.ToString("N2", CultureInfo.GetCultureInfo("en-PH"));

    private static Color StatusColor(DealSummaryDto deal) => deal.Status switch
    {
        DealStatus.Overdue => Color.FromArgb(178, 49, 49),
        DealStatus.FullyPaid => Color.FromArgb(38, 91, 168),
        DealStatus.Cancelled or DealStatus.Defaulted => Color.FromArgb(120, 120, 120),
        _ => deal.IsPendingConfirmation
            ? Color.FromArgb(191, 127, 36)
            : Color.FromArgb(46, 125, 82)
    };

    private static (string Icon, string Text, Color Color) DealStatusText(DealSummaryDto deal)
    {
        if (deal.IsPendingConfirmation)
            return ("●", "Awaiting confirmation", Color.FromArgb(191, 127, 36));
        return deal.Status switch
        {
            DealStatus.Current => ("●", "Current", Color.FromArgb(46, 125, 82)),
            DealStatus.Overdue => ("▲", "Overdue", Color.FromArgb(178, 49, 49)),
            DealStatus.FullyPaid => ("✓", "Fully paid", Color.FromArgb(38, 91, 168)),
            DealStatus.Cancelled => ("×", "Cancelled", Color.Gray),
            DealStatus.Defaulted => ("×", "Defaulted", Color.Gray),
            _ => ("●", deal.Status.ToString(), Theme.Text)
        };
    }
}

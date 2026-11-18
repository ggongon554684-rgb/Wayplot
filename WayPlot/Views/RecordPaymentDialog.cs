using System.ComponentModel;
using System.Globalization;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.Presenters;
using WayPlot.UI;

namespace WayPlot.Views;

public partial class RecordPaymentDialog : Form
{
    private static readonly PaymentMethod[] Methods = Enum.GetValues<PaymentMethod>();
    private DealListItem? _deal;
    private decimal? _prefillAmount;

    public RecordPaymentDialog()
    {
        InitializeComponent();
        ApplyTheme();

        nudAmount.Maximum = 9999999999999999.99m;
        foreach (var method in Methods)
            cboMethod.Items.Add(EnumText.Pretty(method));
        cboMethod.SelectedIndex = 0;
        btnSave.Click += (_, _) => AsyncUiEvent.Run(this, SaveAsync, "Could not save the payment. Try again.");
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DealListItem? Deal
    {
        set
        {
            _deal = value;
            if (value is null) return;

            lblDeal.Text = $"{value.ContactName} — {value.ParcelLabel}";
            lblBalance.Text = $"Remaining balance: ₱{value.RemainingBalance.ToString("N2", CultureInfo.InvariantCulture)}";
            nudAmount.Value = Math.Min(value.RemainingBalance, nudAmount.Maximum);
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<RecordPaymentRequest, Task<ServiceResult<PaymentListItem>>>? SaveHandler { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal? PrefillAmount
    {
        get => _prefillAmount;
        set
        {
            _prefillAmount = value;
            if (value is > 0)
                nudAmount.Value = Math.Min(value.Value, nudAmount.Maximum);
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ServiceResult<PaymentListItem>? Result { get; private set; }

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        Font = Theme.Body;
        lblTitle.Font = Theme.Heading;
        lblTitle.ForeColor = Theme.Navy;
        lblDeal.Font = Theme.BodyBold;
        lblDeal.ForeColor = Theme.Text;
        lblBalance.Font = Theme.Small;
        lblBalance.ForeColor = Theme.Muted;
        lblDate.Font = Theme.Small;
        lblDate.ForeColor = Theme.Muted;
        lblReceipt.Font = Theme.Small;
        lblReceipt.ForeColor = Theme.Muted;
        lblAmount.Font = Theme.Small;
        lblAmount.ForeColor = Theme.Muted;
        lblMethod.Font = Theme.Small;
        lblMethod.ForeColor = Theme.Muted;
        lblError.ForeColor = ColorTranslator.FromHtml("#8F2A1B");
        nudAmount.Font = Theme.Body;
        dtpPaidAt.Font = Theme.Body;
        txtReceipt.Font = Theme.Body;
        cboMethod.Font = Theme.Body;
        ButtonStyle.Apply(btnSave, primary: true);
        ButtonStyle.Apply(btnCancel, primary: false);
    }

    private async Task SaveAsync()
    {
        if (_deal is null || SaveHandler is null || cboMethod.SelectedIndex < 0 || cboMethod.SelectedIndex >= Methods.Length)
            return;

        lblError.Visible = false;
        btnSave.Enabled = false;
        try
        {
            var result = await SaveHandler(new RecordPaymentRequest(
                _deal.Id, nudAmount.Value, dtpPaidAt.Value, Methods[cboMethod.SelectedIndex], txtReceipt.Text));
            if (IsDisposed) return;
            if (!result.Ok)
            {
                lblError.Text = result.Message;
                lblError.Visible = true;
                return;
            }

            Result = result;
            DialogResult = DialogResult.OK;
        }
        finally
        {
            if (!IsDisposed) btnSave.Enabled = true;
        }
    }
}

using System.ComponentModel;
using System.Globalization;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.Presenters;
using WayPlot.UI;

namespace WayPlot.Views;

public partial class CreateDealDialog : Form
{
    private IReadOnlyList<DealCandidateItem> _candidates = [];

    public CreateDealDialog()
    {
        InitializeComponent();
        ApplyTheme();

        nudPrice.Maximum = 9999999999999999.99m;
        nudDiscount.Maximum = 9999999999999999.99m;
        nudReservationFee.Maximum = 9999999999999999.99m;
        nudDownPayment.Maximum = 9999999999999999.99m;
        cboLead.SelectedIndexChanged += (_, _) => SelectCandidate();
        cboPaymentPlan.SelectedIndexChanged += (_, _) => UpdatePlanFields();
        cboPaymentPlan.Items.AddRange(["Full payment", "Installment plan"]);
        cboPaymentPlan.SelectedIndex = 0;
        btnSave.Click += (_, _) => AsyncUiEvent.Run(this, SaveAsync, "Could not save the deal. Try again.");
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<DealCandidateItem> Candidates
    {
        set
        {
            _candidates = value;
            cboLead.Items.Clear();
            foreach (var candidate in _candidates)
                cboLead.Items.Add($"{candidate.ContactName} — {candidate.ParcelLabel}");
            cboLead.SelectedIndex = _candidates.Count == 0 ? -1 : 0;
            lblLead.Text = _candidates.Count == 0
                ? "Eligible lead"
                : $"{_candidates[0].ClosingStageName} lead";
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<CreateDealRequest, Task<ServiceResult<DealListItem>>>? SaveHandler { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ServiceResult<DealListItem>? Result { get; private set; }

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        Font = Theme.Body;
        lblTitle.Font = Theme.Heading;
        lblTitle.ForeColor = Theme.Navy;
        lblLead.Font = Theme.Small;
        lblLead.ForeColor = Theme.Muted;
        lblPrice.Font = Theme.Small;
        lblPrice.ForeColor = Theme.Muted;
        lblDiscount.Font = Theme.Small;
        lblDiscount.ForeColor = Theme.Muted;
        lblReservation.Font = Theme.Small;
        lblReservation.ForeColor = Theme.Muted;
        lblDownPayment.Font = Theme.Small;
        lblDownPayment.ForeColor = Theme.Muted;
        lblPaymentPlan.Font = Theme.Small;
        lblPaymentPlan.ForeColor = Theme.Muted;
        lblInstallmentCount.Font = Theme.Small;
        lblInstallmentCount.ForeColor = Theme.Muted;
        lblDueDay.Font = Theme.Small;
        lblDueDay.ForeColor = Theme.Muted;
        lblError.ForeColor = ColorTranslator.FromHtml("#8F2A1B");
        cboLead.Font = Theme.Body;
        cboPaymentPlan.Font = Theme.Body;
        nudPrice.Font = Theme.Body;
        nudDiscount.Font = Theme.Body;
        nudReservationFee.Font = Theme.Body;
        nudDownPayment.Font = Theme.Body;
        nudInstallmentCount.Font = Theme.Body;
        nudDueDay.Font = Theme.Body;
        ButtonStyle.Apply(btnSave, primary: true);
        ButtonStyle.Apply(btnCancel, primary: false);
    }

    private void SelectCandidate()
    {
        if (cboLead.SelectedIndex < 0 || cboLead.SelectedIndex >= _candidates.Count) return;
        var candidate = _candidates[cboLead.SelectedIndex];
        var defaultPrice = candidate.OfferAmount is > 0 ? candidate.OfferAmount.Value : candidate.ParcelPrice;
        nudPrice.Value = Math.Clamp(defaultPrice, nudPrice.Minimum, nudPrice.Maximum);
        nudDiscount.Value = 0;
        nudReservationFee.Value = Math.Clamp(candidate.ReservationFee, nudReservationFee.Minimum, nudReservationFee.Maximum);
        nudDownPayment.Value = 0;
        UpdatePlanFields();
    }

    private void UpdatePlanFields()
    {
        var installment = cboPaymentPlan.SelectedIndex == 1;
        lblInstallmentCount.Visible = nudInstallmentCount.Visible = installment;
        lblDueDay.Visible = nudDueDay.Visible = installment;
    }

    private async Task SaveAsync()
    {
        if (SaveHandler is null || cboLead.SelectedIndex < 0 || cboLead.SelectedIndex >= _candidates.Count)
            return;

        lblError.Visible = false;
        btnSave.Enabled = false;
        try
        {
            var candidate = _candidates[cboLead.SelectedIndex];
            var result = await SaveHandler(new CreateDealRequest(
                candidate.LeadId,
                nudPrice.Value,
                nudDiscount.Value,
                nudReservationFee.Value,
                nudDownPayment.Value,
                cboPaymentPlan.SelectedIndex == 1,
                cboPaymentPlan.SelectedIndex == 1 ? decimal.ToInt32(nudInstallmentCount.Value) : 0,
                cboPaymentPlan.SelectedIndex == 1 ? decimal.ToInt32(nudDueDay.Value) : 1));
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

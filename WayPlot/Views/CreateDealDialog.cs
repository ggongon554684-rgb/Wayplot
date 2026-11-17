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
        nudDownPayment.Maximum = 9999999999999999.99m;
        cboLead.SelectedIndexChanged += (_, _) => SelectCandidate();
        btnSave.Click += async (_, _) => await SaveAsync();
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
        lblDownPayment.Font = Theme.Small;
        lblDownPayment.ForeColor = Theme.Muted;
        lblReservation.Font = Theme.Small;
        lblReservation.ForeColor = Theme.Muted;
        lblError.ForeColor = ColorTranslator.FromHtml("#8F2A1B");
        cboLead.Font = Theme.Body;
        nudPrice.Font = Theme.Body;
        nudDownPayment.Font = Theme.Body;
        ButtonStyle.Apply(btnSave, primary: true);
        ButtonStyle.Apply(btnCancel, primary: false);
    }

    private void SelectCandidate()
    {
        if (cboLead.SelectedIndex < 0 || cboLead.SelectedIndex >= _candidates.Count) return;
        var candidate = _candidates[cboLead.SelectedIndex];
        var defaultPrice = candidate.OfferAmount is > 0 ? candidate.OfferAmount.Value : candidate.ParcelPrice;
        nudPrice.Value = Math.Clamp(defaultPrice, nudPrice.Minimum, nudPrice.Maximum);
        nudDownPayment.Value = 0;
        lblReservation.Text = candidate.ReservationFee == 0
            ? "No reservation fee is set for this parcel."
            : $"Reservation fee: ₱{candidate.ReservationFee.ToString("N2", CultureInfo.InvariantCulture)}";
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
            var result = await SaveHandler(new CreateDealRequest(candidate.LeadId, nudPrice.Value, nudDownPayment.Value));
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

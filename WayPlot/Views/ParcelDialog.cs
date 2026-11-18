using System.ComponentModel;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.UI;

namespace WayPlot.Views;

public partial class ParcelDialog : Form
{
    private readonly ParcelDto? _parcel;
    private readonly List<OwnerDto> _owners;
    private readonly IReadOnlyList<ParcelAgentOption> _agents;
    private readonly string? _geoJson;
    private readonly double? _latitude;
    private readonly double? _longitude;

    public ParcelDialog(
        IReadOnlyList<OwnerDto> owners,
        IReadOnlyList<ParcelAgentOption>? agents,
        ParcelDto? parcel,
        decimal? areaSqm,
        string? geoJson,
        double? latitude,
        double? longitude)
    {
        _parcel = parcel;
        _owners = owners.ToList();
        _agents = agents ?? [];
        _geoJson = geoJson;
        _latitude = latitude;
        _longitude = longitude;

        InitializeComponent();
        ApplyTheme();
        ConfigureOwnerPicker();
        ConfigureAgentPicker(agents);
        PopulateFields(areaSqm);

        btnSave.Click += (_, _) => SaveParcel();
        btnCancel.Click += (_, _) => Close();
        btnNewOwner.Click += async (_, _) => await CreateOwnerAsync();
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<CreateOwnerRequest, Task<ServiceResult<OwnerDto>>>? CreateOwnerHandler { get; set; }

    public CreateParcelRequest? CreateRequest { get; private set; }
    public UpdateParcelRequest? UpdateRequest { get; private set; }

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        Font = Theme.Body;
        lblTitle.Font = Theme.Heading;
        lblTitle.ForeColor = Theme.Navy;
        lblError.ForeColor = Color.Firebrick;
        ButtonStyle.Apply(btnSave, primary: true);
        ButtonStyle.Apply(btnCancel, primary: false);
        ButtonStyle.Apply(btnNewOwner, primary: false);
    }

    private void ConfigureOwnerPicker()
    {
        cboOwner.DataSource = _owners;
        cboOwner.DisplayMember = nameof(OwnerDto.Name);
        cboOwner.ValueMember = nameof(OwnerDto.Id);
        if (_parcel is not null)
            cboOwner.SelectedValue = _parcel.OwnerId;
    }

    private void ConfigureAgentPicker(IReadOnlyList<ParcelAgentOption>? agents)
    {
        var showAgentPicker = _parcel is null && agents is not null;
        lblAgent.Visible = showAgentPicker;
        cboAgent.Visible = showAgentPicker;
        if (!showAgentPicker)
            return;

        cboAgent.Items.Add("Unassigned");
        foreach (var agent in _agents)
            cboAgent.Items.Add(agent.FullName);
        cboAgent.SelectedIndex = 0;
    }

    private void PopulateFields(decimal? areaSqm)
    {
        lblTitle.Text = _parcel is null ? "New parcel" : "Edit parcel details";
        txtLabel.Text = _parcel?.Label ?? string.Empty;
        txtDescription.Text = _parcel?.Description ?? string.Empty;
        txtZoning.Text = _parcel?.Zoning ?? string.Empty;
        txtUtilities.Text = _parcel?.Utilities ?? string.Empty;
        txtRoadAccess.Text = _parcel?.RoadAccess ?? string.Empty;
        txtTaxDeclaration.Text = _parcel?.TaxDeclarationNo ?? string.Empty;
        txtTitleNo.Text = _parcel?.TitleNo ?? string.Empty;
        numPrice.Value = _parcel?.Price ?? 0;
        numReservationFee.Value = _parcel?.ReservationFee ?? 0;
        numArea.Value = areaSqm ?? _parcel?.AreaSqm ?? 0;
        numPrice.ReadOnly = _parcel is not null;
        numReservationFee.ReadOnly = _parcel is not null;
        numPrice.Enabled = _parcel is null;
        numReservationFee.Enabled = _parcel is null;
    }

    private void SaveParcel()
    {
        if (cboOwner.SelectedItem is not OwnerDto owner)
        {
            lblError.Text = "Choose an owner or create a new owner.";
            return;
        }

        var label = txtLabel.Text.Trim();
        if (label.Length == 0)
        {
            lblError.Text = "Enter a parcel label.";
            return;
        }
        if (label.Length > 60)
        {
            lblError.Text = "The label must be 60 characters or fewer.";
            return;
        }
        if (numArea.Value <= 0)
        {
            lblError.Text = "The area must be more than zero square meters.";
            return;
        }

        if (_parcel is null)
        {
            Guid? assignedStaffId = cboAgent.Visible && cboAgent.SelectedIndex > 0
                ? _agents[cboAgent.SelectedIndex - 1].Id
                : null;
            CreateRequest = new CreateParcelRequest(
                label,
                txtDescription.Text.Trim(),
                numArea.Value,
                txtZoning.Text.Trim(),
                numPrice.Value,
                numReservationFee.Value,
                _geoJson,
                _latitude,
                _longitude,
                txtTaxDeclaration.Text.Trim(),
                txtTitleNo.Text.Trim(),
                txtUtilities.Text.Trim(),
                txtRoadAccess.Text.Trim(),
                owner.Id,
                AssignedStaffId: assignedStaffId);
        }
        else
        {
            UpdateRequest = new UpdateParcelRequest(
                _parcel.Id,
                label,
                txtDescription.Text.Trim(),
                numArea.Value,
                txtZoning.Text.Trim(),
                null,
                null,
                null,
                txtTaxDeclaration.Text.Trim(),
                txtTitleNo.Text.Trim(),
                txtUtilities.Text.Trim(),
                txtRoadAccess.Text.Trim(),
                owner.Id);
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private async Task CreateOwnerAsync()
    {
        using var dialog = new OwnerDialog();
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.OwnerRequest is not { } request)
            return;
        if (CreateOwnerHandler is null)
        {
            lblError.Text = "Owner creation is unavailable.";
            return;
        }

        btnNewOwner.Enabled = false;
        try
        {
            var result = await CreateOwnerHandler(request);
            if (IsDisposed)
                return;
            if (!result.Ok || result.Value is null)
            {
                lblError.Text = result.Message;
                return;
            }

            var selectedId = result.Value.Id;
            _owners.Add(result.Value);
            cboOwner.DataSource = null;
            cboOwner.DataSource = _owners;
            cboOwner.DisplayMember = nameof(OwnerDto.Name);
            cboOwner.ValueMember = nameof(OwnerDto.Id);
            cboOwner.SelectedValue = selectedId;
            lblError.Text = string.Empty;
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
                lblError.Text = $"The owner could not be saved. {ex.Message}";
        }
        finally
        {
            if (!IsDisposed)
                btnNewOwner.Enabled = true;
        }
    }
}

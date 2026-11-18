using System.ComponentModel;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.UI;

namespace WayPlot.Views;

public partial class CompanyProfileDialog : Form
{
    private CompanyProfileVersion? _current;

    public CompanyProfileDialog()
    {
        InitializeComponent();
        BackColor = Theme.Page;
        Font = Theme.Body;
        lblTitle.Font = Theme.Heading;
        lblTitle.ForeColor = Theme.Navy;
        lblVersion.Font = Theme.Small;
        lblVersion.ForeColor = Theme.Muted;
        lblError.ForeColor = ColorTranslator.FromHtml("#8F2A1B");
        ButtonStyle.Apply(btnSave, true);
        ButtonStyle.Apply(btnCancel, false);
        btnSave.Click += async (_, _) => await SaveAsync();
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public CompanyProfileSnapshot Profile
    {
        set
        {
            _current = value.Current;
            if (_current is not { } current)
            {
                lblVersion.Text = "Not saved yet (version 0).";
                return;
            }
            txtName.Text = current.Name;
            txtContactNumbers.Text = current.ContactNumbers;
            txtEmail.Text = current.Email;
            txtAddress.Text = current.Address;
            lblVersion.Text = $"Version {current.Version}; last changed by {current.ChangedBy} " +
                $"on {current.ChangedAtUtc.ToLocalTime():g}.";
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsReadOnly
    {
        set
        {
            txtName.ReadOnly = value;
            txtContactNumbers.ReadOnly = value;
            txtEmail.ReadOnly = value;
            txtAddress.ReadOnly = value;
            ButtonStyle.ApplyAvailability(
                [new(btnSave, true, !value,
                    value ? "Only a Tenant Admin can save the company profile." : null, true)],
                toolTip);
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<CompanyProfileRequest, Task<ServiceResult<CompanyProfileSnapshot>>>? SaveHandler { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ServiceResult<CompanyProfileSnapshot>? Result { get; private set; }

    private async Task SaveAsync()
    {
        if (SaveHandler is null) return;
        btnSave.Enabled = false;
        lblError.Visible = false;
        try
        {
            var result = await SaveHandler(new CompanyProfileRequest(
                txtName.Text,
                txtContactNumbers.Text,
                txtEmail.Text,
                txtAddress.Text,
                _current?.Version ?? 0));
            Result = result;
            if (IsDisposed) return;
            if (!result.Ok)
            {
                lblError.Text = result.Message;
                lblError.Visible = true;
                return;
            }
            DialogResult = DialogResult.OK;
        }
        catch (Exception)
        {
            if (!IsDisposed)
            {
                lblError.Text = "The company profile could not be saved. Reload and try again.";
                lblError.Visible = true;
            }
        }
        finally
        {
            if (!IsDisposed) btnSave.Enabled = true;
        }
    }
}

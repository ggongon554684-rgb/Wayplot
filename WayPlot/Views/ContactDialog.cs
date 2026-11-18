using Microsoft.Data.SqlClient;
using System.ComponentModel;
using System.Data;
using System.Xml.Linq;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Entities;
using WayPlot.UI;

namespace WayPlot.Views;

/// <summary>The New contact dialog. Layout is in ContactDialog.Designer.cs. It checks nothing itself:
/// every rule (name, phone format, consent, duplicates) lives in ContactService, and its message is shown here.</summary>
public partial class ContactDialog : Form
{
    private static readonly LeadSource[] Sources = Enum.GetValues<LeadSource>()
        .Where(source => source != LeadSource.Import)
        .ToArray();
    private static readonly ContactType[] Types = [ContactType.Customer, ContactType.Landowner];
    private ContactReferralOptions _referralOptions = new([], []);

    public ContactDialog()
    {
        InitializeComponent();

        foreach (var source in Sources) cboSource.Items.Add(EnumText.Pretty(source));
        foreach (var type in Types) cboType.Items.Add(EnumText.Pretty(type));
        cboSource.SelectedIndex = 0;
        cboType.SelectedIndex = Math.Max(0, Array.IndexOf(Types, ContactType.Customer));
        cboReferrerType.Items.AddRange(["None", "Existing contact", "Staff member", "Other person"]);
        cboReferrerType.SelectedIndex = 0;

        ApplyTheme();
        cboReferrerType.SelectedIndexChanged += (_, _) => UpdateReferrerControls();
        btnSave.Click += async (_, _) => await SaveAsync();
    }

    /// <summary>Set by the screen that opens the dialog. The Designer never saves it.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<CaptureContactRequest, Task<ServiceResult<CaptureContactResult>>>? SaveHandler { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ContactReferralOptions ReferralOptions
    {
        set
        {
            _referralOptions = value;
            UpdateReferrerControls();
        }
    }

    /// <summary>The service's answer after a successful save (including "matched an existing contact").</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ServiceResult<CaptureContactResult>? Result { get; private set; }

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        Font = Theme.Body;
        foreach (var label in new[]
                 {
                     lblName, lblPhone, lblEmail, lblSource, lblType, lblReferrerType,
                     lblReferrerChoice, lblReferrerName, lblReferrerPhone, lblNote
                 })
        {
            label.ForeColor = Theme.Muted;
            label.Font = Theme.Small;
        }
        lblError.ForeColor = ColorTranslator.FromHtml("#8F2A1B");
        chkSms.ForeColor = Theme.Text;
        chkEmail.ForeColor = Theme.Text;
        cboReferrerType.Font = Theme.Body;
        cboReferrerChoice.Font = Theme.Body;
        ButtonStyle.Apply(btnSave, primary: true);
        ButtonStyle.Apply(btnCancel, primary: false);
    }

    private void UpdateReferrerControls()
    {
        if (cboReferrerType.SelectedIndex < 0) return;

        var type = cboReferrerType.SelectedIndex;
        var choices = type == 1 ? _referralOptions.Contacts
            : type == 2 ? _referralOptions.Staff
            : Array.Empty<ReferralChoice>();
        cboReferrerChoice.Items.Clear();
        foreach (var choice in choices)
            cboReferrerChoice.Items.Add(choice);

        var useChoice = type is 1 or 2;
        var useOther = type == 3;
        lblReferrerChoice.Visible = useChoice;
        cboReferrerChoice.Visible = useChoice;
        lblReferrerName.Visible = useOther;
        txtReferrerName.Visible = useOther;
        lblReferrerPhone.Visible = useOther;
        txtReferrerPhone.Visible = useOther;
        layout.PerformLayout();
    }

    private async Task SaveAsync()
    {
        if (SaveHandler is null) return;

        lblError.Visible = false;
        btnSave.Enabled = false;
        try
        {
            Guid? referrerContactId = null;
            Guid? referrerStaffId = null;
            string? referrerName = null;
            string? referrerPhone = null;
            if (cboReferrerType.SelectedIndex == 1 && cboReferrerChoice.SelectedItem is ReferralChoice contact)
                referrerContactId = contact.Id;
            else if (cboReferrerType.SelectedIndex == 2 && cboReferrerChoice.SelectedItem is ReferralChoice staff)
                referrerStaffId = staff.Id;
            else if (cboReferrerType.SelectedIndex == 3)
            {
                referrerName = txtReferrerName.Text;
                referrerPhone = txtReferrerPhone.Text;
            }

            var request = new CaptureContactRequest(
                txtName.Text,
                txtPhone.Text,
                txtEmail.Text,
                Sources[cboSource.SelectedIndex],
                Types[cboType.SelectedIndex],
                chkSms.Checked,
                chkEmail.Checked,
                txtNote.Text,
                ReferrerContactId: referrerContactId,
                ReferrerStaffId: referrerStaffId,
                ReferrerName: referrerName,
                ReferrerPhone: referrerPhone);

            var result = await SaveHandler(request);
            if (!result.Ok)
            {
                lblError.Text = result.Message;
                lblError.Visible = true;
                return;
            }

            Result = result;
            DialogResult = DialogResult.OK;   // closes the dialog
        }
        finally
        {
            if (!IsDisposed) btnSave.Enabled = true;
        }
    }
}
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
    private static readonly LeadSource[] Sources = Enum.GetValues<LeadSource>();
    private static readonly ContactType[] Types = Enum.GetValues<ContactType>();

    public ContactDialog()
    {
        InitializeComponent();

        foreach (var source in Sources) cboSource.Items.Add(EnumText.Pretty(source));
        foreach (var type in Types) cboType.Items.Add(EnumText.Pretty(type));
        cboSource.SelectedIndex = 0;
        cboType.SelectedIndex = Math.Max(0, Array.IndexOf(Types, ContactType.Customer));

        ApplyTheme();
        btnSave.Click += async (_, _) => await SaveAsync();
    }

    /// <summary>Set by the screen that opens the dialog. The Designer never saves it.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<CaptureContactRequest, Task<ServiceResult<CaptureContactResult>>>? SaveHandler { get; set; }

    /// <summary>The service's answer after a successful save (including "matched an existing contact").</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ServiceResult<CaptureContactResult>? Result { get; private set; }

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        Font = Theme.Body;
        foreach (var label in new[] { lblName, lblPhone, lblEmail, lblSource, lblType, lblNote })
        {
            label.ForeColor = Theme.Muted;
            label.Font = Theme.Small;
        }
        lblError.ForeColor = ColorTranslator.FromHtml("#8F2A1B");
        chkSms.ForeColor = Theme.Text;
        chkEmail.ForeColor = Theme.Text;
        ButtonStyle.Apply(btnSave, primary: true);
        ButtonStyle.Apply(btnCancel, primary: false);
    }

    private async Task SaveAsync()
    {
        if (SaveHandler is null) return;

        lblError.Visible = false;
        btnSave.Enabled = false;
        try
        {
            var request = new CaptureContactRequest(
                txtName.Text,
                txtPhone.Text,
                txtEmail.Text,
                Sources[cboSource.SelectedIndex],
                Types[cboType.SelectedIndex],
                chkSms.Checked,
                chkEmail.Checked,
                txtNote.Text);

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
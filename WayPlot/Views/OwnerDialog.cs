using WayPlot.domain.Contracts;
using WayPlot.UI;

namespace WayPlot.Views;

public partial class OwnerDialog : Form
{
    public OwnerDialog()
    {
        InitializeComponent();
        BackColor = Theme.Page;
        Font = Theme.Body;
        lblTitle.Font = Theme.Heading;
        lblTitle.ForeColor = Theme.Navy;
        lblError.ForeColor = Color.Firebrick;
        ButtonStyle.Apply(btnSave, primary: true);
        ButtonStyle.Apply(btnCancel, primary: false);
        btnSave.Click += (_, _) => SaveOwner();
        btnCancel.Click += (_, _) => Close();
    }

    public CreateOwnerRequest? OwnerRequest { get; private set; }

    private void SaveOwner()
    {
        var name = txtName.Text.Trim();
        if (name.Length == 0)
        {
            lblError.Text = "Enter the owner's name.";
            return;
        }

        OwnerRequest = new CreateOwnerRequest(
            name,
            txtPhone.Text.Trim(),
            txtEmail.Text.Trim(),
            chkListingAgreement.Checked);
        DialogResult = DialogResult.OK;
        Close();
    }
}

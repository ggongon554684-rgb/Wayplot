using System.ComponentModel;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.Presenters;
using WayPlot.UI;

namespace WayPlot.Views;

public partial class ReturnPaymentDialog : Form
{
    public ReturnPaymentDialog()
    {
        InitializeComponent();
        ApplyTheme();
        btnSave.Click += (_, _) => AsyncUiEvent.Run(this, SaveAsync, "Could not return the payment. Try again.");
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<string?, Task<ServiceResult<PaymentListItem>>>? SaveHandler { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ServiceResult<PaymentListItem>? Result { get; private set; }

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        Font = Theme.Body;
        lblTitle.Font = Theme.Heading;
        lblTitle.ForeColor = Theme.Navy;
        lblHelp.Font = Theme.Small;
        lblHelp.ForeColor = Theme.Muted;
        lblNote.Font = Theme.Small;
        lblNote.ForeColor = Theme.Muted;
        lblError.ForeColor = ColorTranslator.FromHtml("#8F2A1B");
        txtNote.Font = Theme.Body;
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
            var result = await SaveHandler(txtNote.Text);
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

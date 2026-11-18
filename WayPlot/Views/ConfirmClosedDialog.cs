using System.ComponentModel;
using WayPlot.domain.Common;
using WayPlot.UI;

namespace WayPlot.Views;

public partial class ConfirmClosedDialog : Form
{
    private static readonly ParcelStatus[] ClosingStatuses = [ParcelStatus.Sold, ParcelStatus.Leased];

    public ConfirmClosedDialog()
    {
        InitializeComponent();
        BackColor = Theme.Page;
        Font = Theme.Body;
        lblTitle.Font = Theme.Heading;
        lblTitle.ForeColor = Theme.Navy;
        lblStatus.Font = Theme.Small;
        lblStatus.ForeColor = Theme.Muted;
        cboStatus.Font = Theme.Body;
        foreach (var status in ClosingStatuses)
            cboStatus.Items.Add(EnumText.Pretty(status));
        cboStatus.SelectedIndex = 0;
        ButtonStyle.Apply(btnConfirm, primary: true);
        ButtonStyle.Apply(btnCancel, primary: false);
        btnConfirm.Click += (_, _) =>
        {
            if (cboStatus.SelectedIndex < 0) return;
            SelectedStatus = ClosingStatuses[cboStatus.SelectedIndex];
            DialogResult = DialogResult.OK;
        };
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ParcelStatus SelectedStatus { get; private set; } = ParcelStatus.Sold;
}

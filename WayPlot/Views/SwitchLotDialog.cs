using System.ComponentModel;
using WayPlot.domain.Contracts;
using WayPlot.UI;

namespace WayPlot.Views;

public partial class SwitchLotDialog : Form
{
    public SwitchLotDialog(Guid currentParcelId, string currentParcelLabel)
    {
        InitializeComponent();
        picker.ExcludeParcelId = currentParcelId;
        lblCurrent.Text = $"Current lot: {currentParcelLabel}";
        lblEffect.Text = "This lead will be marked Lost and a new lead starts at New for the chosen lot.";
        ApplyTheme();
        picker.SelectedChanged += (_, _) => btnSave.Enabled = picker.SelectedParcel is not null;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ParcelDto? SelectedParcel => picker.SelectedParcel;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Reason => txtReason.Text;

    public void SetParcels(IEnumerable<ParcelDto> parcels) => picker.SetParcels(parcels);

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        Font = Theme.Body;
        lblCurrent.ForeColor = Theme.Muted;
        lblEffect.ForeColor = Theme.Muted;
        txtReason.Font = Theme.Body;
        ButtonStyle.Apply(btnSave, true);
        ButtonStyle.Apply(btnCancel, false);
        btnSave.Enabled = false;
    }
}

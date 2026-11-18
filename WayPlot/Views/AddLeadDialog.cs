using System.ComponentModel;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.UI;

namespace WayPlot.Views;

public partial class AddLeadDialog : Form
{
    public AddLeadDialog()
    {
        InitializeComponent();
        ApplyTheme();
        picker.SelectedChanged += (_, _) => UpdateSave();
        cboAgent.SelectedIndexChanged += (_, _) => UpdateSave();
        btnSave.Click += (_, _) => DialogResult = DialogResult.OK;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ParcelDto? SelectedParcel => picker.SelectedParcel;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Note => txtNote.Text;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Guid? SelectedAgentId => cboAgent.SelectedItem is StaffListItem staff ? staff.Id : null;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowAgentPicker
    {
        set
        {
            lblAgent.Visible = value;
            cboAgent.Visible = value;
            layout.PerformLayout();
            UpdateSave();
        }
    }

    public void SetParcels(IEnumerable<ParcelDto> parcels) => picker.SetParcels(parcels);

    public void SetAgents(IEnumerable<StaffListItem> staff)
    {
        cboAgent.Items.Clear();
        foreach (var member in staff.Where(item => item.Role == Roles.Agent && item.IsActive))
            cboAgent.Items.Add(member);
        if (cboAgent.Items.Count > 0) cboAgent.SelectedIndex = 0;
    }

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        Font = Theme.Body;
        lblAgent.ForeColor = lblNote.ForeColor = lblParcel.ForeColor = Theme.Muted;
        txtNote.Font = Theme.Body;
        cboAgent.Font = Theme.Body;
        ButtonStyle.Apply(btnSave, true);
        ButtonStyle.Apply(btnCancel, false);
        UpdateSave();
    }

    private void UpdateSave()
    {
        var agentReady = !lblAgent.Visible || cboAgent.SelectedItem is StaffListItem;
        btnSave.Enabled = picker.SelectedParcel is not null && agentReady;
    }
}

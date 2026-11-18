using WayPlot.domain.Contracts;
using WayPlot.UI;

namespace WayPlot.Views;

public partial class ReassignDialog : Form
{
    private readonly Guid _parcelId;
    private readonly IReadOnlyList<ParcelAgentOption> _agents;

    public ReassignDialog(Guid parcelId, IReadOnlyList<ParcelAgentOption> agents, Guid? currentStaffId)
    {
        _parcelId = parcelId;
        _agents = agents;
        InitializeComponent();
        BackColor = Theme.Page;
        Font = Theme.Body;
        lblTitle.Font = Theme.Heading;
        lblTitle.ForeColor = Theme.Navy;
        lblError.ForeColor = Color.Firebrick;
        foreach (var agent in _agents)
            cboAgent.Items.Add(agent.FullName);
        var currentIndex = _agents.ToList().FindIndex(agent => agent.Id == currentStaffId);
        if (currentIndex >= 0)
            cboAgent.SelectedIndex = currentIndex;
        ButtonStyle.Apply(btnSave, primary: true);
        ButtonStyle.Apply(btnCancel, primary: false);
        btnSave.Click += (_, _) => Save();
        btnCancel.Click += (_, _) => Close();
    }

    public ReassignParcelRequest? Request { get; private set; }

    private void Save()
    {
        if (cboAgent.SelectedIndex < 0)
        {
            lblError.Text = "Choose an active Agent.";
            return;
        }

        Request = new ReassignParcelRequest(_parcelId, _agents[cboAgent.SelectedIndex].Id);
        DialogResult = DialogResult.OK;
        Close();
    }
}

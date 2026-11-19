using System.ComponentModel;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.UI;

namespace WayPlot.Views;

public partial class PipelineStagesDialog : Form
{
    private sealed record StageRow(
        PipelineStageAdminInfo Stage,
        Panel Panel,
        CheckBox Active,
        TextBox Name,
        Label Count);

    private readonly List<StageRow> _rows = [];
    private readonly int _expectedVersion;
    private readonly Func<PipelineStageSaveRequest, Task<ServiceResult<PipelineStageConfigSnapshot>>> _save;

    public PipelineStagesDialog(
        PipelineStageAdminSnapshot snapshot,
        Func<PipelineStageSaveRequest, Task<ServiceResult<PipelineStageConfigSnapshot>>> save)
    {
        InitializeComponent();
        _expectedVersion = snapshot.ExpectedVersion;
        _save = save;
        ApplyTheme();
        flowStages.SizeChanged += (_, _) =>
        {
            var width = Math.Max(200, flowStages.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 8);
            foreach (var row in _rows)
                row.Panel.Width = width;
        };
        lblVersion.Text = _expectedVersion == 0
            ? "No saved configuration yet (version 0)."
            : $"Current configuration version {_expectedVersion}.";

        foreach (var stage in snapshot.Stages.OrderBy(item => item.SortOrder))
            AddStageRow(stage);

        btnSave.Click += (_, _) => AsyncUiEvent.Run(
            this, SaveAsync, "Pipeline stages could not be saved. Reload and try again.");
        btnCancel.Click += (_, _) => Close();
        AcceptButton = btnSave;
        CancelButton = btnCancel;
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ServiceResult<PipelineStageConfigSnapshot>? Result { get; private set; }

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        Font = Theme.Body;
        lblTitle.Font = Theme.Heading;
        lblTitle.ForeColor = Theme.Navy;
        foreach (var label in new[] { lblInstructions, lblVersion, lblNewFixed, lblClosedFixed, lblLostFixed })
        {
            label.Font = Theme.Small;
            label.ForeColor = Theme.Muted;
        }
        lblStatus.ForeColor = Theme.Muted;
        ButtonStyle.Apply(btnSave, primary: true);
        ButtonStyle.Apply(btnCancel, primary: false);
    }

    private void AddStageRow(PipelineStageAdminInfo stage)
    {
        var rowPanel = new Panel
        {
            Height = 40,
            Width = flowStages.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 8,
            Margin = new Padding(0, 0, 0, 4),
            AccessibleName = $"{PipelineStageDefinition.Key(stage.Stage)} stage settings"
        };
        var layout = new TableLayoutPanel
        {
            ColumnCount = 5,
            RowCount = 1,
            Dock = DockStyle.Fill,
            Margin = new Padding(0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));

        var slot = new Label
        {
            Text = PipelineStageDefinition.Key(stage.Stage),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            AccessibleName = $"{PipelineStageDefinition.Key(stage.Stage)} fixed stage slot"
        };
        var active = new CheckBox
        {
            Checked = stage.IsActive,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            AccessibleName = $"Enable {PipelineStageDefinition.Key(stage.Stage)}"
        };
        var name = new TextBox
        {
            Text = stage.Name,
            MaxLength = 30,
            Dock = DockStyle.Fill,
            AccessibleName = $"{PipelineStageDefinition.Key(stage.Stage)} display name"
        };
        var count = new Label
        {
            Text = stage.OpenLeadCount.ToString(),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            AccessibleName = $"Open leads in {PipelineStageDefinition.Key(stage.Stage)}: {stage.OpenLeadCount}"
        };
        var orderButtons = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            WrapContents = false,
            Margin = new Padding(0)
        };
        var up = new Button { Text = "Up", AutoSize = true, AccessibleName = $"Move {stage.Name} up" };
        var down = new Button { Text = "Down", AutoSize = true, AccessibleName = $"Move {stage.Name} down" };
        ButtonStyle.Apply(up, primary: false);
        ButtonStyle.Apply(down, primary: false);
        up.Click += (_, _) => MoveRow(rowPanel, -1);
        down.Click += (_, _) => MoveRow(rowPanel, 1);
        orderButtons.Controls.Add(up);
        orderButtons.Controls.Add(down);

        layout.Controls.Add(slot, 0, 0);
        layout.Controls.Add(active, 1, 0);
        layout.Controls.Add(name, 2, 0);
        layout.Controls.Add(count, 3, 0);
        layout.Controls.Add(orderButtons, 4, 0);
        rowPanel.Controls.Add(layout);
        flowStages.Controls.Add(rowPanel);
        _rows.Add(new StageRow(stage, rowPanel, active, name, count));
    }

    private void MoveRow(Panel rowPanel, int offset)
    {
        var index = _rows.FindIndex(row => row.Panel == rowPanel);
        var target = index + offset;
        if (index < 0 || target < 0 || target >= _rows.Count)
            return;

        (_rows[index], _rows[target]) = (_rows[target], _rows[index]);
        flowStages.SuspendLayout();
        flowStages.Controls.Clear();
        foreach (var row in _rows)
            flowStages.Controls.Add(row.Panel);
        flowStages.ResumeLayout();
    }

    private async Task SaveAsync()
    {
        btnSave.Enabled = false;
        lblStatus.Text = string.Empty;
        try
        {
            var stages = _rows.Select((row, index) => new PipelineStageSetting(
                row.Stage.Stage,
                row.Name.Text.Trim(),
                index,
                row.Active.Checked)).ToList();
            var result = await _save(new PipelineStageSaveRequest(_expectedVersion, stages));
            if (IsDisposed) return;
            if (!result.Ok)
            {
                ShowStatus(result.Message, true);
                return;
            }

            Result = result;
            DialogResult = DialogResult.OK;
        }
        catch (Exception)
        {
            if (!IsDisposed)
                ShowStatus("Pipeline stages could not be saved. Reload and try again.", true);
        }
        finally
        {
            if (!IsDisposed) btnSave.Enabled = true;
        }
    }

    private void ShowStatus(string message, bool isError)
    {
        lblStatus.Text = message;
        lblStatus.ForeColor = isError ? ColorTranslator.FromHtml("#8F2A1B") : Theme.Muted;
    }
}

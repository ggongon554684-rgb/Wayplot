using System.ComponentModel;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.UI;

namespace WayPlot.Views;

public sealed record MoveStageDialogResult(LeadStage TargetStage, string? Note, string? LostReason);

public partial class MoveStageDialog : Form
{
    private IReadOnlyList<PipelineStageInfo> _stages = [];
    private sealed record StageChoice(LeadStage Stage, string Name);

    public MoveStageDialog() : this(new DefaultPipelineStageProvider().GetStages()) { }

    public MoveStageDialog(IReadOnlyList<PipelineStageInfo> stages)
    {
        InitializeComponent();
        ApplyTheme();

        _stages = stages;
        foreach (var stage in stages)
        {
            if (Enum.TryParse<LeadStage>(stage.Key, out var leadStage))
                cboStage.Items.Add(new StageChoice(leadStage, stage.Name));
        }
        cboStage.DisplayMember = nameof(StageChoice.Name);

        txtNote.MaxLength = 0;
        cboStage.SelectedIndexChanged += (_, _) => UpdateNotePrompt();
        btnSave.Click += (_, _) => Save();
        btnCancel.Click += (_, _) => Close();
        UpdateNotePrompt();
    }

    public MoveStageDialogResult? Result { get; private set; }

    public void SelectStage(LeadStage stage)
    {
        for (var index = 0; index < cboStage.Items.Count; index++)
        {
            if (cboStage.Items[index] is StageChoice choice && choice.Stage == stage)
            {
                cboStage.SelectedIndex = index;
                return;
            }
        }
    }

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        Font = Theme.Body;
        lblTitle.Font = Theme.Heading;
        lblTitle.ForeColor = Theme.Navy;
        lblHelp.ForeColor = Theme.Muted;
        lblNote.ForeColor = Theme.Text;
        lblStatus.ForeColor = Theme.Muted;
        ButtonStyle.Apply(btnSave, primary: true);
        ButtonStyle.Apply(btnCancel, primary: false);
    }

    private void UpdateNotePrompt()
    {
        var lost = cboStage.SelectedItem is StageChoice choice
            && choice.Stage == LeadStage.Lost;
        lblNote.Text = lost ? "Notes / lost reason" : "Notes (optional)";
        lblHelp.Text = lost
            ? "Enter the reason for marking this lead Lost."
            : "Add an optional note to the stage history.";
    }

    private void Save()
    {
        if (cboStage.SelectedIndex < 0)
        {
            ShowStatus("Select a target stage.", true);
            return;
        }

        if (cboStage.SelectedItem is not StageChoice choice)
        {
            ShowStatus("Select a target stage.", true);
            return;
        }
        var targetStage = choice.Stage;
        var note = string.IsNullOrWhiteSpace(txtNote.Text) ? null : txtNote.Text.Trim();
        if (targetStage == LeadStage.Lost && note is null)
        {
            ShowStatus("Enter the reason for marking this lead Lost.", true);
            return;
        }

        Result = new MoveStageDialogResult(
            targetStage,
            note,
            targetStage == LeadStage.Lost ? note : null);
        DialogResult = DialogResult.OK;
        Close();
    }

    private void ShowStatus(string text, bool isError)
    {
        lblStatus.Text = text;
        lblStatus.ForeColor = isError ? ColorTranslator.FromHtml("#8F2A1B") : Theme.Muted;
    }
}

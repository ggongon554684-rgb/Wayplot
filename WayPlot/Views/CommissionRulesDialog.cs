using System.ComponentModel;
using System.Globalization;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.UI;

namespace WayPlot.Views;

public partial class CommissionRulesDialog : Form
{
    private static readonly CommissionCalculationType[] CalculationTypes =
        Enum.GetValues<CommissionCalculationType>();
    private static readonly CommissionPayableTiming[] PayableTimings =
        Enum.GetValues<CommissionPayableTiming>();
    private IReadOnlyList<CommissionOverrideTarget> _targets = Array.Empty<CommissionOverrideTarget>();
    private int _expectedVersion;

    public CommissionRulesDialog()
    {
        InitializeComponent();
        nudCommissionAmount.Maximum = 9999999999999999.99m;
        nudReferralAmount.Maximum = 9999999999999999.99m;
        foreach (var type in CalculationTypes)
        {
            cboCommissionType.Items.Add(EnumText.Pretty(type));
            cboReferralType.Items.Add(EnumText.Pretty(type));
        }
        foreach (var timing in PayableTimings)
            cboPayableTiming.Items.Add(EnumText.Pretty(timing));

        ApplyTheme();
        btnSave.Click += async (_, _) => await SaveAsync();
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public CommissionRulesSnapshot Rules
    {
        set
        {
            listHistory.Items.Clear();
            foreach (var version in value.History.OrderByDescending(item => item.Version))
                listHistory.Items.Add(HistoryLine(version));

            if (value.Current is not { } current)
            {
                _expectedVersion = 0;
                lblCurrentVersion.Text = "No rules saved yet (version 0).";
                return;
            }
            _expectedVersion = current.Version;
            lblCurrentVersion.Text = $"Current version {current.Version}; last changed by {current.ChangedBy} " +
                $"on {current.ChangedAtUtc.ToLocalTime():g}.";
            cboCommissionType.SelectedIndex = Array.IndexOf(CalculationTypes, current.CommissionType);
            nudCommissionAmount.Value = Clamp(nudCommissionAmount, current.CommissionAmount);
            nudAgentShare.Value = Clamp(nudAgentShare, current.AgentSharePercent);
            nudCompanyShare.Value = Clamp(nudCompanyShare, current.CompanySharePercent);
            nudBranchManagerShare.Value = Clamp(nudBranchManagerShare, current.BranchManagerSharePercent);
            cboReferralType.SelectedIndex = Array.IndexOf(CalculationTypes, current.ReferralType);
            nudReferralAmount.Value = Clamp(nudReferralAmount, current.ReferralAmount);
            cboPayableTiming.SelectedIndex = Array.IndexOf(PayableTimings, current.PayableTiming);
            LoadOverrides(current.Overrides ?? []);
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public IReadOnlyList<CommissionOverrideTarget> Targets
    {
        set
        {
            _targets = value;
            ConfigureGrid(dgvAgentOverrides, CommissionOverrideScope.Agent, value);
            ConfigureGrid(dgvParcelOverrides, CommissionOverrideScope.Parcel, value);
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool HasBranching
    {
        set
        {
            lblBranchManagerShare.Visible = value;
            nudBranchManagerShare.Visible = value;
            lblShareHelp.Text = value
                ? "Agent, company, and Branch Manager shares must total exactly 100%."
                : "Agent and Company shares must total exactly 100%.";
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<CommissionRulesRequest, Task<ServiceResult<CommissionRulesSnapshot>>>? SaveHandler { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ServiceResult<CommissionRulesSnapshot>? Result { get; private set; }

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        Font = Theme.Body;
        lblTitle.Font = Theme.Heading;
        lblTitle.ForeColor = Theme.Navy;
        foreach (var label in new[]
                 {
                     lblInstructions, lblCommissionType, lblCommissionAmount, lblShareHelp,
                     lblAgentShare, lblCompanyShare, lblBranchManagerShare, lblReferralType,
                     lblReferralAmount, lblPayableTiming, lblHistory, lblCurrentVersion,
                     lblOverrides, lblAgentOverrides, lblParcelOverrides
                 })
        {
            label.Font = Theme.Small;
            label.ForeColor = Theme.Muted;
        }
        lblError.ForeColor = ColorTranslator.FromHtml("#8F2A1B");
        listHistory.Font = Theme.Small;
        listHistory.BackColor = Theme.Surface;
        listHistory.ForeColor = Theme.Text;
        ButtonStyle.Apply(btnSave, primary: true);
        ButtonStyle.Apply(btnCancel, primary: false);
    }

    private async Task SaveAsync()
    {
        if (SaveHandler is null) return;
        if (cboCommissionType.SelectedIndex < 0 || cboReferralType.SelectedIndex < 0
            || cboPayableTiming.SelectedIndex < 0)
        {
            ShowError("Choose a calculation type and payout timing.");
            return;
        }

        lblError.Visible = false;
        btnSave.Enabled = false;
        try
        {
            if (!TryReadOverrides(out var overrides))
                return;
            var request = new CommissionRulesRequest(
                CalculationTypes[cboCommissionType.SelectedIndex],
                nudCommissionAmount.Value,
                nudAgentShare.Value,
                nudCompanyShare.Value,
                nudBranchManagerShare.Visible ? nudBranchManagerShare.Value : 0,
                CalculationTypes[cboReferralType.SelectedIndex],
                nudReferralAmount.Value,
                PayableTimings[cboPayableTiming.SelectedIndex],
                _expectedVersion,
                overrides);
            var result = await SaveHandler(request);
            if (IsDisposed) return;
            if (!result.Ok)
            {
                ShowError(result.Message);
                return;
            }

            Result = result;
            DialogResult = DialogResult.OK;
        }
        catch (Exception)
        {
            if (!IsDisposed) ShowError("Commission rules could not be saved. Reload and try again.");
        }
        finally
        {
            if (!IsDisposed) btnSave.Enabled = true;
        }
    }

    private void ShowError(string message)
    {
        lblError.Text = message;
        lblError.Visible = true;
    }

    private static decimal Clamp(NumericUpDown control, decimal value) =>
        Math.Clamp(value, control.Minimum, control.Maximum);

    private static string HistoryLine(CommissionRulesVersion version)
    {
        var type = EnumText.Pretty(version.CommissionType);
        var amount = version.CommissionType == CommissionCalculationType.Percent
            ? $"{version.CommissionAmount.ToString("N2", CultureInfo.InvariantCulture)}%"
            : $"₱{version.CommissionAmount.ToString("N2", CultureInfo.InvariantCulture)}";
        var referralType = EnumText.Pretty(version.ReferralType);
        var referralAmount = version.ReferralType == CommissionCalculationType.Percent
            ? $"{version.ReferralAmount.ToString("N2", CultureInfo.InvariantCulture)}%"
            : $"₱{version.ReferralAmount.ToString("N2", CultureInfo.InvariantCulture)}";
        return $"v{version.Version} — {version.ChangedAtUtc.ToLocalTime():g} — {version.ChangedBy} — " +
            $"{type} {amount}; shares A/C/BM {version.AgentSharePercent:N2}/{version.CompanySharePercent:N2}/{version.BranchManagerSharePercent:N2}; " +
            $"referral {referralType} {referralAmount}; {EnumText.Pretty(version.PayableTiming)}";
    }

    private void LoadOverrides(IReadOnlyList<CommissionRuleOverride> overrides)
    {
        dgvAgentOverrides.Rows.Clear();
        dgvParcelOverrides.Rows.Clear();
        foreach (var item in overrides)
        {
            var grid = item.Scope == CommissionOverrideScope.Agent ? dgvAgentOverrides : dgvParcelOverrides;
            var targetColumn = item.Scope == CommissionOverrideScope.Agent ? "AgentTarget" : "ParcelTarget";
            if (!_targets.Any(target => target.Scope == item.Scope && target.Id == item.TargetId))
            {
                if (grid.Columns[targetColumn] is not DataGridViewComboBoxColumn column)
                    throw new InvalidOperationException("Commission override target column is unavailable.");
                var choices = (column.DataSource as IReadOnlyList<CommissionOverrideTarget>
                    ?? Array.Empty<CommissionOverrideTarget>()).ToList();
                choices.Add(new CommissionOverrideTarget(item.Scope, item.TargetId, $"Unavailable ({item.TargetId})"));
                column.DataSource = choices;
            }
            var rowIndex = grid.Rows.Add();
            grid.Rows[rowIndex].Cells[targetColumn].Value = item.TargetId;
            grid.Rows[rowIndex].Cells["Type"].Value = item.Type;
            grid.Rows[rowIndex].Cells["Amount"].Value = item.Amount.ToString(CultureInfo.CurrentCulture);
        }
    }

    private static void ConfigureGrid(
        DataGridView grid, CommissionOverrideScope scope, IReadOnlyList<CommissionOverrideTarget> targets)
    {
        var targetColumn = (DataGridViewComboBoxColumn)grid.Columns[0];
        targetColumn.DataSource = targets.Where(target => target.Scope == scope).ToList();
        targetColumn.DisplayMember = nameof(CommissionOverrideTarget.Display);
        targetColumn.ValueMember = nameof(CommissionOverrideTarget.Id);
        var typeColumn = (DataGridViewComboBoxColumn)grid.Columns[1];
        typeColumn.DataSource = CalculationTypes.ToList();
    }

    private bool TryReadOverrides(out IReadOnlyList<CommissionRuleOverride> overrides)
    {
        var result = new List<CommissionRuleOverride>();
        if (!TryReadGrid(dgvAgentOverrides, CommissionOverrideScope.Agent, "AgentTarget", result)
            || !TryReadGrid(dgvParcelOverrides, CommissionOverrideScope.Parcel, "ParcelTarget", result))
        {
            ShowError("Choose a target, calculation type, and valid amount for each override row.");
            overrides = Array.Empty<CommissionRuleOverride>();
            return false;
        }
        overrides = result;
        return true;
    }

    private static bool TryReadGrid(
        DataGridView grid,
        CommissionOverrideScope scope,
        string targetColumn,
        ICollection<CommissionRuleOverride> overrides)
    {
        grid.EndEdit();
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow) continue;
            if (row.Cells[targetColumn].Value is not Guid targetId
                || row.Cells["Type"].Value is not CommissionCalculationType type
                || !decimal.TryParse(Convert.ToString(row.Cells["Amount"].Value),
                    NumberStyles.Number, CultureInfo.CurrentCulture, out var amount))
                return false;
            overrides.Add(new CommissionRuleOverride(scope, targetId, type, amount));
        }
        return true;
    }
}

using System.ComponentModel;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.Presenters;
using WayPlot.UI;

namespace WayPlot.Views;

public partial class TeamAdminView : UserControl
{
    private TeamAdminPresenter? _presenter;
    private TeamAdminData? _data;

    public TeamAdminView()
    {
        InitializeComponent();
        ApplyTheme();
        btnInvite.Click += (_, _) => { };
        btnChangeRole.Click += (_, _) => { };
        btnCompanyProfile.Click += (_, _) =>
            AsyncUiEvent.Run(this, OpenCompanyProfileAsync, "The company profile could not be opened.");
        btnDeactivate.Click += (_, _) =>
            AsyncUiEvent.Run(this, () => SetActiveAsync(false), "The staff account could not be changed.");
        btnReactivate.Click += (_, _) =>
            AsyncUiEvent.Run(this, () => SetActiveAsync(true), "The staff account could not be changed.");
        btnChangeBranch.Click += (_, _) =>
            AsyncUiEvent.Run(this, ChangeBranchAsync, "The branch could not be changed.");
        btnReassign.Click += (_, _) =>
            AsyncUiEvent.Run(this, ReassignSelectedAsync, "The record could not be reassigned.");
        btnReassignAll.Click += (_, _) =>
            AsyncUiEvent.Run(this, ReassignAllAsync, "The records could not be reassigned.");
        listStaff.SelectedIndexChanged += (_, _) => UpdateStaffActions();
        cboRecordType.SelectedIndexChanged += (_, _) => UpdateRecordChoices();
        cboRecord.SelectedIndexChanged += (_, _) => UpdateReassignmentActions();
        cboTarget.SelectedIndexChanged += (_, _) => UpdateReassignmentActions();
        cboSource.SelectedIndexChanged += (_, _) => UpdateReassignmentActions();
        cboDestination.SelectedIndexChanged += (_, _) => UpdateReassignmentActions();
        cboBranch.SelectedIndexChanged += (_, _) => UpdateStaffActions();
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TeamAdminPresenter? Presenter
    {
        set => _presenter = value;
    }

    protected override async void OnLoad(EventArgs e)
    {
        try
        {
            base.OnLoad(e);
            if (DesignMode || _presenter is null) return;
            await LoadAsync();
        }
        catch (Exception)
        {
            if (!IsDisposed)
                ShowStatus("The Team screen could not be loaded. Try again.", true);
        }
    }

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        lblTitle.Font = Theme.Heading;
        lblTitle.ForeColor = Theme.Navy;
        lblStatus.Font = Theme.Small;
        lblStatus.ForeColor = Theme.Muted;
        foreach (var label in new[] { lblRecordType, lblRecord, lblTarget, lblSource, lblDestination, lblBranch })
        {
            label.Font = Theme.Small;
            label.ForeColor = Theme.Muted;
        }
        listStaff.Font = Theme.Body;
        listStaff.BackColor = Theme.Surface;
        listStaff.ForeColor = Theme.Text;
        foreach (var button in new[] { btnInvite, btnChangeRole, btnCompanyProfile, btnDeactivate,
                     btnReactivate, btnChangeBranch, btnReassign, btnReassignAll })
            ButtonStyle.Apply(button, button == btnCompanyProfile || button == btnReassignAll);
        ButtonStyle.ApplyAvailability(
        [
            new(btnInvite, true, false, "Needs the account service (planned)", false),
            new(btnChangeRole, true, false, "Needs the account service (planned)", false)
        ], toolTip);
        cboRecordType.Items.AddRange(["Lead", "Contact", "Parcel"]);
        cboRecordType.SelectedIndex = 0;
        chkIncludeOpenLeads.Checked = true;
        chkIncludeOpenLeads.Text = "Also reassign open leads for a contact";
    }

    private async Task LoadAsync(string? notice = null)
    {
        var presenter = _presenter;
        if (presenter is null) return;
        ShowStatus("Loading staff and reassignment choices...", false);
        var result = await presenter.LoadAsync();
        if (IsDisposed) return;
        if (!result.Ok || result.Value is null)
        {
            ShowStatus(result.Message, true);
            return;
        }

        _data = result.Value;
        PopulateStaff();
        PopulateAgents();
        PopulateBranches();
        UpdateRecordChoices();
        ShowStatus(notice ?? $"{_data.Staff.Count} staff account(s).", false);
    }

    private void PopulateStaff()
    {
        listStaff.BeginUpdate();
        listStaff.Items.Clear();
        foreach (var member in _data!.Staff)
        {
            var item = new ListViewItem(member.DisplayName) { Tag = member };
            item.SubItems.Add(member.Username);
            item.SubItems.Add(member.Role);
            item.SubItems.Add(member.IsActive ? "Active" : "Inactive");
            item.SubItems.Add(member.OpenLeadCount.ToString());
            item.SubItems.Add(member.AvailableParcelCount.ToString());
            item.SubItems.Add(_data.Branches.FirstOrDefault(branch => branch.Id == member.BranchId)?.Name ?? "—");
            listStaff.Items.Add(item);
        }
        listStaff.EndUpdate();
        UpdateStaffActions();
    }

    private void PopulateAgents()
    {
        cboTarget.Items.Clear();
        cboSource.Items.Clear();
        foreach (var agent in _data!.ActiveAgents)
        {
            cboTarget.Items.Add(agent);
            cboSource.Items.Add(agent);
        }
        if (cboTarget.Items.Count > 0) cboTarget.SelectedIndex = 0;
        if (cboSource.Items.Count > 0) cboSource.SelectedIndex = 0;
    }

    private void PopulateBranches()
    {
        cboBranch.Items.Clear();
        foreach (var branch in _data!.Branches)
            cboBranch.Items.Add(branch);
        if (cboBranch.Items.Count > 0) cboBranch.SelectedIndex = 0;
    }

    private void UpdateRecordChoices()
    {
        var selectedId = (cboRecord.SelectedItem as ReassignmentChoice)?.Id;
        cboRecord.Items.Clear();
        if (_data is null) return;
        var choices = cboRecordType.SelectedIndex switch
        {
            0 => _data.Leads,
            1 => _data.Contacts,
            2 => _data.Parcels,
            _ => Array.Empty<ReassignmentChoice>()
        };
        foreach (var choice in choices)
            cboRecord.Items.Add(choice);
        var selected = choices.ToList().FindIndex(choice => choice.Id == selectedId);
        if (cboRecord.Items.Count > 0)
            cboRecord.SelectedIndex = selected >= 0 ? selected : 0;
        chkIncludeOpenLeads.Visible = cboRecordType.SelectedIndex == 1;
        UpdateReassignmentActions();
    }

    private void UpdateStaffActions()
    {
        var member = SelectedStaff;
        var selectedBranch = cboBranch.SelectedItem as BranchListItem;
        var branchUnavailable = member is null ? "Select a staff member first."
            : selectedBranch is null ? "No existing branches are available."
            : member.BranchId == selectedBranch.Id ? "This staff member is already assigned to that branch."
            : null;
        var activeAdmins = _data?.Staff.Count(item => item.IsActive && item.Role == Roles.TenantAdmin) ?? 0;
        var deactivateReason = member is null ? "Select a staff member first."
            : member.Id == _presenter?.CurrentStaffId ? "You cannot deactivate your own account."
            : member.Role == Roles.TenantAdmin && activeAdmins <= 1
                ? "The last active Tenant Admin cannot be deactivated."
                : null;
        ButtonStyle.ApplyAvailability(
        [
            new(btnDeactivate, member?.IsActive == true, member?.IsActive == true && deactivateReason is null,
                deactivateReason, false),
            new(btnReactivate, member is not null && !member.IsActive,
                member is not null && !member.IsActive,
                member is null ? "Select a staff member first." : null, false),
            new(btnChangeBranch, _presenter?.HasBranching == true,
                _presenter?.HasBranching == true && branchUnavailable is null,
                branchUnavailable, false)
        ], toolTip);
    }

    private void UpdateReassignmentActions()
    {
        var hasRecord = cboRecord.SelectedItem is ReassignmentChoice;
        var hasTarget = cboTarget.SelectedItem is ParcelAgentOption;
        var hasSource = cboSource.SelectedItem is ParcelAgentOption;
        var hasDestination = cboDestination.SelectedItem is ParcelAgentOption;
        ButtonStyle.ApplyAvailability(
        [
            new(btnReassign, true, hasRecord && hasTarget,
                !hasRecord ? "Select a lead, contact, or parcel." :
                !hasTarget ? "No active Agent is available." : null, false),
            new(btnReassignAll, true, hasSource && hasDestination && cboSource.SelectedIndex != cboDestination.SelectedIndex,
                !hasSource || !hasDestination ? "Choose an active source and destination Agent." :
                cboSource.SelectedIndex == cboDestination.SelectedIndex ? "Choose different source and destination Agents." : null, true)
        ], toolTip);
    }

    private async Task SetActiveAsync(bool active)
    {
        var presenter = _presenter;
        var member = SelectedStaff;
        if (presenter is null || member is null) return;
        try
        {
            var prompt = active
                ? $"Reactivate {member.DisplayName}'s account?"
                : $"Deactivate {member.DisplayName}'s account? Existing history will be retained.";
            if (MessageBox.Show(FindForm(), prompt, active ? "Reactivate staff" : "Deactivate staff",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            var result = active
                ? await presenter.ReactivateAsync(member)
                : await presenter.DeactivateAsync(member);
            if (IsDisposed) return;
            if (!result.Ok)
            {
                ShowStatus(result.Message, true);
                return;
            }
            await LoadAsync(result.Message);
        }
        catch (Exception)
        {
            if (!IsDisposed) ShowStatus("The staff account could not be changed. Reload and try again.", true);
        }
    }

    private async Task ChangeBranchAsync()
    {
        if (_presenter is null || SelectedStaff is not { } member
            || cboBranch.SelectedItem is not BranchListItem branch)
            return;
        try
        {
            var result = await _presenter.ChangeBranchAsync(member, branch.Id);
            if (IsDisposed) return;
            if (!result.Ok)
            {
                ShowStatus(result.Message, true);
                return;
            }
            await LoadAsync(result.Message);
        }
        catch (Exception)
        {
            if (!IsDisposed) ShowStatus("The branch could not be changed. Reload and try again.", true);
        }
    }

    private async Task ReassignSelectedAsync()
    {
        if (_presenter is null || cboRecord.SelectedItem is not ReassignmentChoice record
            || cboTarget.SelectedItem is not ParcelAgentOption agent)
            return;
        try
        {
            if (MessageBox.Show(FindForm(),
                    $"Reassign the selected {cboRecordType.Text.ToLowerInvariant()} to {agent.FullName}?",
                    "Confirm reassignment", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            ServiceResult<bool> result;
            if (cboRecordType.SelectedIndex == 0)
                result = await _presenter.ReassignLeadAsync(record.Id, agent.Id);
            else if (cboRecordType.SelectedIndex == 1)
                result = await _presenter.ReassignContactAsync(record.Id, agent.Id, chkIncludeOpenLeads.Checked);
            else if (cboRecordType.SelectedIndex == 2)
            {
                var parcelResult = await _presenter.ReassignParcelAsync(record.Id, agent.Id);
                result = parcelResult.Ok
                    ? ServiceResult<bool>.Success(true, parcelResult.Message)
                    : ServiceResult<bool>.Fail(parcelResult.Message);
            }
            else
                result = ServiceResult<bool>.Fail("Choose a record type.");
            if (IsDisposed) return;
            if (!result.Ok)
            {
                ShowStatus(result.Message, true);
                return;
            }
            await LoadAsync(result.Message);
        }
        catch (Exception)
        {
            if (!IsDisposed) ShowStatus("The record could not be reassigned. Reload and try again.", true);
        }
    }

    private async Task ReassignAllAsync()
    {
        if (_presenter is null || cboSource.SelectedItem is not ParcelAgentOption source
            || cboDestination.SelectedItem is not ParcelAgentOption destination)
            return;
        try
        {
            var preview = await _presenter.PreviewReassignAllAsync(source.Id, destination.Id);
            if (IsDisposed) return;
            if (!preview.Ok || preview.Value is null)
            {
                ShowStatus(preview.Message, true);
                return;
            }

            var counts = preview.Value;
            var confirmation = $"Move {counts.ContactsReassigned} contact(s), {counts.LeadsReassigned} open lead(s), " +
                $"and {counts.ParcelsReassigned} parcel(s) from {source.FullName} to {destination.FullName}? History will be preserved.";
            if (MessageBox.Show(FindForm(), confirmation, "Reassign all records",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            var result = await _presenter.ReassignAllAsync(source.Id, destination.Id);
            if (IsDisposed) return;
            if (!result.Ok)
            {
                ShowStatus(result.Message, true);
                return;
            }
            await LoadAsync(result.Message);
        }
        catch (Exception)
        {
            if (!IsDisposed) ShowStatus("The records could not be reassigned. Reload and try again.", true);
        }
    }

    private async Task OpenCompanyProfileAsync()
    {
        if (_presenter is null) return;
        try
        {
            var result = await _presenter.GetCompanyProfileAsync();
            if (IsDisposed) return;
            if (!result.Ok || result.Value is null)
            {
                ShowStatus(result.Message, true);
                return;
            }
            using var dialog = new CompanyProfileDialog
            {
                Profile = result.Value,
                SaveHandler = _presenter.SaveCompanyProfileAsync
            };
            if (dialog.ShowDialog(FindForm()) == DialogResult.OK && dialog.Result is { Ok: true } saved)
                ShowStatus(saved.Message, false);
            else if (dialog.Result is { Ok: false } failed)
                ShowStatus(failed.Message, true);
        }
        catch (Exception)
        {
            if (!IsDisposed) ShowStatus("The company profile could not be opened. Try again.", true);
        }
    }

    private StaffListItem? SelectedStaff =>
        listStaff.SelectedItems.Count > 0 ? listStaff.SelectedItems[0].Tag as StaffListItem : null;

    private void ShowStatus(string message, bool isError)
    {
        if (IsDisposed) return;
        lblStatus.Text = message;
        lblStatus.ForeColor = isError ? ColorTranslator.FromHtml("#8F2A1B") : Theme.Muted;
    }
}

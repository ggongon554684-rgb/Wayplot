using System.ComponentModel;
using System.Text;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.UI;

namespace WayPlot.Views;

public partial class ContactImportDialog : Form
{
    private IReadOnlyList<ImportCsvColumn>? _columns;
    private ContactImportPreview? _preview;
    private ContactImportSummary? _summary;
    private string? _csvContent;
    private int _previewTicket;
    private bool _busy;
    private bool _canMapAgent;

    public ContactImportDialog()
    {
        InitializeComponent();
        ApplyTheme();

        btnBrowse.Click += (_, _) => ChooseCsv();
        btnPreview.Click += (_, _) => AsyncUiEvent.Run(this, PreviewAsync, "Could not preview the import. Try again.");
        btnConfirm.Click += (_, _) => AsyncUiEvent.Run(this, ImportAsync, "Could not import contacts. Try again.");
        btnDownloadRejected.Click += (_, _) => DownloadRejected();
        btnClose.Click += (_, _) => CloseDialog();
        listRows.Resize += (_, _) => FitColumns();
        foreach (var combo in MappingCombos())
            combo.SelectedIndexChanged += (_, _) => MappingChanged();
        FormClosing += (_, e) =>
        {
            if (_busy)
            {
                e.Cancel = true;
                return;
            }
            if (_summary is not null)
                DialogResult = DialogResult.OK;
        };
        CancelButton = btnClose;
        UpdateActionAvailability();
        FitColumns();
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<string, ServiceResult<IReadOnlyList<ImportCsvColumn>>>? ColumnsHandler { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<string, ContactImportMapping, Task<ServiceResult<ContactImportPreview>>>? PreviewHandler { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<string, ContactImportMapping, Task<ServiceResult<ContactImportSummary>>>? ImportHandler { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool CanMapAgent
    {
        get => _canMapAgent;
        set
        {
            _canMapAgent = value;
            UpdateAgentMapping();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ContactImportSummary? Result { get; private set; }

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        ForeColor = Theme.Text;
        lblTitle.Font = Theme.Heading;
        lblTitle.ForeColor = Theme.Navy;
        lblHelp.Font = Theme.Body;
        lblHelp.ForeColor = Theme.Muted;
        lblSummary.Font = Theme.Body;
        lblSummary.ForeColor = Theme.Muted;
        txtFilePath.Font = Theme.Body;
        groupMapping.Font = Theme.BodyBold;
        foreach (var control in MappingCombos())
            control.Font = Theme.Body;
        foreach (var label in new[] { lblName, lblPhone, lblEmail, lblSource, lblConsent, lblParcel, lblAgent })
            label.Font = Theme.Body;
        listRows.Font = Theme.Body;
        listRows.BackColor = Theme.Surface;
        listRows.ForeColor = Theme.Text;
        UpdateActionAvailability();
    }

    private void ChooseCsv()
    {
        using var picker = new OpenFileDialog
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            Title = "Choose contacts CSV"
        };
        if (picker.ShowDialog(this) != DialogResult.OK)
            return;

        string content;
        try
        {
            content = File.ReadAllText(picker.FileName);
        }
        catch (IOException)
        {
            ShowStatus("Could not read the selected file.", true);
            return;
        }
        catch (UnauthorizedAccessException)
        {
            ShowStatus("You do not have permission to read the selected file.", true);
            return;
        }

        if (ColumnsHandler is null)
        {
            ShowStatus("CSV import is unavailable.", true);
            return;
        }

        var columns = ColumnsHandler(content);
        if (!columns.Ok || columns.Value is null)
        {
            _csvContent = null;
            _columns = null;
            txtFilePath.Clear();
            ResetPreview();
            ShowStatus(columns.Message, true);
            UpdateActionAvailability();
            return;
        }

        _csvContent = content;
        _columns = columns.Value;
        txtFilePath.Text = picker.FileName;
        PopulateMappings(_columns);
        ResetPreview();
        ShowStatus($"CSV loaded with {_columns.Count} columns. Map the fields and preview before importing.", false);
        UpdateActionAvailability();
    }

    private async Task PreviewAsync()
    {
        if (_csvContent is null || PreviewHandler is null)
            return;

        var ticket = ++_previewTicket;
        _busy = true;
        SetMappingEnabled(false);
        UpdateActionAvailability();
        ShowStatus("Checking rows...", false);
        try
        {
            var result = await PreviewHandler(_csvContent, GetMapping());
            if (ticket != _previewTicket || IsDisposed)
                return;
            if (!result.Ok || result.Value is null)
            {
                _preview = null;
                ShowStatus(result.Message, true);
                ShowRows([]);
                return;
            }

            _preview = result.Value;
            ShowRows(_preview.Rows);
            var valid = _preview.Rows.Count(row => row.Status == ContactImportRowStatus.Valid);
            var duplicates = _preview.Rows.Count(row => row.Status == ContactImportRowStatus.Duplicate);
            var rejected = _preview.Rows.Count(row => row.Status == ContactImportRowStatus.Invalid);
            ShowStatus($"Preview: {valid} valid, {duplicates} duplicates, {rejected} rejected.", false);
        }
        catch (Exception)
        {
            if (ticket == _previewTicket && !IsDisposed)
                ShowStatus("Could not preview the CSV. Check the database connection and try again.", true);
        }
        finally
        {
            if (ticket == _previewTicket && !IsDisposed)
            {
                _busy = false;
                SetMappingEnabled(true);
                UpdateActionAvailability();
            }
        }
    }

    private async Task ImportAsync()
    {
        if (_csvContent is null || ImportHandler is null || _preview is null)
            return;

        _busy = true;
        SetMappingEnabled(false);
        UpdateActionAvailability();
        ShowStatus("Importing valid rows...", false);
        try
        {
            var result = await ImportHandler(_csvContent, GetMapping());
            if (IsDisposed)
                return;
            if (!result.Ok || result.Value is null)
            {
                ShowStatus(result.Message, true);
                return;
            }

            _summary = result.Value;
            Result = _summary;
            ShowRows(_summary.Rows);
            ShowStatus(
                $"Import complete: {_summary.ContactsCreated} contacts, {_summary.LeadsCreated} leads, " +
                $"{_summary.Duplicates} duplicates, {_summary.Rejected} rejected, {_summary.Warnings} warnings.",
                false);
            btnClose.Text = "Done";
        }
        catch (Exception)
        {
            if (!IsDisposed)
                ShowStatus("The import could not be completed. Check the database connection and try again.", true);
        }
        finally
        {
            if (!IsDisposed)
            {
                _busy = false;
                SetMappingEnabled(true);
                UpdateActionAvailability();
            }
        }
    }

    private void DownloadRejected()
    {
        var rows = (_summary?.Rows ?? _preview?.Rows ?? Array.Empty<ContactImportRow>())
            .Where(row => row.Status == ContactImportRowStatus.Invalid)
            .ToList();
        var columns = _summary?.Columns ?? _preview?.Columns ?? _columns;
        if (rows.Count == 0 || columns is null)
            return;

        using var picker = new SaveFileDialog
        {
            AddExtension = true,
            DefaultExt = "csv",
            FileName = "rejected-contacts.csv",
            Filter = "CSV files (*.csv)|*.csv",
            Title = "Save rejected rows"
        };
        if (picker.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var extraColumns = Math.Max(0, rows.Max(row => row.Fields.Count) - columns.Count);
            var headers = columns.Select(column => column.Header)
                .Concat(Enumerable.Range(1, extraColumns).Select(index => $"Extra column {index}"))
                .Append("Row number")
                .Append("Reason")
                .Append("Warning");
            var output = new List<string> { string.Join(",", headers.Select(EscapeCsv)) };
            foreach (var row in rows)
            {
                var fields = row.Fields.ToList();
                while (fields.Count < columns.Count + extraColumns)
                    fields.Add(string.Empty);
                fields.Add(row.RowNumber.ToString());
                fields.Add(row.Reason ?? string.Empty);
                fields.Add(row.Warning ?? string.Empty);
                output.Add(string.Join(",", fields.Select(EscapeCsv)));
            }
            File.WriteAllText(picker.FileName, string.Join(Environment.NewLine, output), new UTF8Encoding(true));
        }
        catch (IOException)
        {
            ShowStatus("Could not save the rejected rows file.", true);
        }
        catch (UnauthorizedAccessException)
        {
            ShowStatus("You do not have permission to save the rejected rows file.", true);
        }
    }

    private void PopulateMappings(IReadOnlyList<ImportCsvColumn> columns)
    {
        foreach (var combo in MappingCombos())
        {
            combo.Items.Clear();
            combo.Items.Add(new ColumnChoice(null, "(Not mapped)"));
            foreach (var column in columns)
                combo.Items.Add(new ColumnChoice(column.Index, $"{column.Header} (column {column.Index + 1})"));
            combo.SelectedIndex = 0;
        }

        SelectHeader(cboName, "name", "fullname", "contactname", "customername");
        SelectHeader(cboPhone, "phone", "mobile", "phonenumber", "mobilenumber");
        SelectHeader(cboEmail, "email", "emailaddress");
        SelectHeader(cboSource, "source", "leadsource");
        SelectHeader(cboConsent, "consent", "consenttomessage", "consenttobemessaged", "messagingconsent", "smsconsent");
        SelectHeader(cboParcel, "parcel", "parcellabel", "lot", "lotlabel", "parcelcode");
        SelectHeader(cboAgent, "agentusername", "assignedagent", "agent");
        UpdateAgentMapping();
    }

    private void SelectHeader(ComboBox combo, params string[] aliases)
    {
        foreach (ColumnChoice choice in combo.Items)
        {
            if (choice.Index is not { } index || _columns is null)
                continue;
            var header = new string(_columns[index].Header.Where(char.IsLetterOrDigit).ToArray());
            if (aliases.Any(alias => string.Equals(alias, header, StringComparison.OrdinalIgnoreCase)))
            {
                combo.SelectedItem = choice;
                return;
            }
        }
    }

    private ContactImportMapping GetMapping() => new(
        SelectedIndex(cboName) ?? -1,
        SelectedIndex(cboPhone),
        SelectedIndex(cboEmail),
        SelectedIndex(cboSource) ?? -1,
        SelectedIndex(cboConsent),
        SelectedIndex(cboParcel),
        SelectedIndex(cboAgent));

    private static int? SelectedIndex(ComboBox combo) =>
        combo.SelectedItem is ColumnChoice choice ? choice.Index : null;

    private void MappingChanged()
    {
        if (_busy)
            return;
        ResetPreview();
        if (_csvContent is not null)
            ShowStatus("Mapping changed. Preview the rows again before importing.", false);
        UpdateActionAvailability();
    }

    private void ResetPreview()
    {
        _previewTicket++;
        _preview = null;
        _summary = null;
        Result = null;
        ShowRows([]);
        UpdateActionAvailability();
    }

    private void ShowRows(IReadOnlyList<ContactImportRow> rows)
    {
        if (IsDisposed)
            return;
        listRows.BeginUpdate();
        listRows.Items.Clear();
        foreach (var row in rows)
        {
            var item = new ListViewItem(row.RowNumber.ToString());
            item.SubItems.Add(EnumText.Pretty(row.Status));
            item.SubItems.Add(row.Name);
            item.SubItems.Add(row.Phone ?? "—");
            item.SubItems.Add(row.Email ?? "—");
            item.SubItems.Add(row.Parcel ?? "—");
            item.SubItems.Add(row.LeadCreated ? "Created" : row.WillCreateLead ? "Will create" : "No");
            item.SubItems.Add(row.Reason ?? string.Empty);
            item.SubItems.Add(row.Warning ?? string.Empty);
            listRows.Items.Add(item);
        }
        listRows.EndUpdate();
        FitColumns();
    }

    private void ShowStatus(string text, bool isError)
    {
        if (IsDisposed)
            return;
        lblSummary.Text = text;
        lblSummary.ForeColor = isError ? ColorTranslator.FromHtml("#8F2A1B") : Theme.Muted;
    }

    private void UpdateActionAvailability()
    {
        if (btnBrowse is null)
            return;
        var hasFile = _csvContent is not null;
        var hasPreview = _preview is not null;
        var eligible = _preview?.Rows.Any(row =>
            row.Status is ContactImportRowStatus.Valid or ContactImportRowStatus.Duplicate) == true;
        var rejected = (_summary?.Rows ?? _preview?.Rows ?? Array.Empty<ContactImportRow>())
            .Any(row => row.Status == ContactImportRowStatus.Invalid);
        var finished = _summary is not null;

        ButtonStyle.ApplyAvailability(
        [
            new ButtonAvailability(btnBrowse, true, !_busy && !finished,
                finished ? "This import has already finished." : _busy ? "Wait for the current operation to finish." : null, false),
            new ButtonAvailability(btnPreview, true, hasFile && !_busy && !finished,
                !hasFile ? "Choose a CSV file first." : finished ? "This import has already finished." : "Wait for the current operation to finish.", false),
            new ButtonAvailability(btnConfirm, true, hasPreview && eligible && !_busy && !finished,
                finished ? "This import has already finished." : !hasPreview ? "Preview the CSV before importing." : !eligible ? "There are no valid or duplicate rows to import." : "Wait for the current operation to finish.", true),
            new ButtonAvailability(btnDownloadRejected, true, rejected && !_busy,
                rejected ? "Wait for the current operation to finish." : "There are no rejected rows to download.", false),
            new ButtonAvailability(btnClose, true, !_busy,
                "Wait for the import to finish.", false)
        ],
        actionToolTip);
    }

    private void UpdateAgentMapping()
    {
        if (cboAgent is null)
            return;
        cboAgent.Enabled = _canMapAgent && _columns is not null && !_busy;
        actionToolTip.SetToolTip(cboAgent, _canMapAgent
            ? "Optional: match an existing Agent by exact username."
            : "Agents are assigned to themselves; this column is ignored.");
    }

    private void SetMappingEnabled(bool enabled)
    {
        var canEdit = enabled && _summary is null;
        foreach (var combo in MappingCombos())
            combo.Enabled = canEdit && (combo != cboAgent || _canMapAgent);
    }

    private ComboBox[] MappingCombos() =>
        [cboName, cboPhone, cboEmail, cboSource, cboConsent, cboParcel, cboAgent];

    private void FitColumns()
    {
        if (listRows.ClientSize.Width <= 0)
            return;
        colRow.Width = LogicalToDeviceUnits(46);
        colStatus.Width = LogicalToDeviceUnits(82);
        colName.Width = LogicalToDeviceUnits(145);
        colPhone.Width = LogicalToDeviceUnits(100);
        colEmail.Width = LogicalToDeviceUnits(135);
        colParcel.Width = LogicalToDeviceUnits(85);
        colLead.Width = LogicalToDeviceUnits(82);
        var fixedWidth = colRow.Width + colStatus.Width + colName.Width + colPhone.Width
            + colEmail.Width + colParcel.Width + colLead.Width;
        var remaining = Math.Max(240, listRows.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - fixedWidth);
        colReason.Width = remaining / 2;
        colWarning.Width = remaining - colReason.Width;
    }

    private void CloseDialog()
    {
        DialogResult = _summary is null ? DialogResult.Cancel : DialogResult.OK;
        Close();
    }

    private static string EscapeCsv(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\r') && !value.Contains('\n'))
            return value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private sealed record ColumnChoice(int? Index, string Label)
    {
        public override string ToString() => Label;
    }
}

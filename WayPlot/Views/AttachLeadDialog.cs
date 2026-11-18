using System.ComponentModel;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.UI;

namespace WayPlot.Views;

public partial class AttachLeadDialog : Form
{
    private readonly System.Windows.Forms.Timer _searchTimer;
    private int _searchToken;

    public AttachLeadDialog()
    {
        InitializeComponent();
        _searchTimer = new System.Windows.Forms.Timer(components!) { Interval = 350 };
        ApplyTheme();

        btnSearch.Click += async (_, _) => await SearchAsync();
        btnAttach.Click += async (_, _) => await AttachAsync();
        txtSearch.TextChanged += (_, _) => { _searchTimer.Stop(); _searchTimer.Start(); };
        txtSearch.KeyDown += async (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;
            _searchTimer.Stop();
            await SearchAsync();
        };
        _searchTimer.Tick += async (_, _) =>
        {
            _searchTimer.Stop();
            await SearchAsync();
        };
        Shown += async (_, _) => await SearchAsync();
        listContacts.Resize += (_, _) => FitColumns();
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<string?, Task<ServiceResult<IReadOnlyList<ContactListItem>>>>? SearchHandler { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Func<Guid, Task<ServiceResult<LeadDto>>>? SaveHandler { get; set; }

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        Font = Theme.Body;
        lblTitle.Font = Theme.Heading;
        lblTitle.ForeColor = Theme.Navy;
        lblStatus.ForeColor = Theme.Muted;
        txtSearch.Font = Theme.Body;
        listContacts.Font = Theme.Body;
        listContacts.BackColor = Theme.Surface;
        listContacts.ForeColor = Theme.Text;
        ButtonStyle.Apply(btnSearch, primary: false);
        ButtonStyle.Apply(btnAttach, primary: true);
        ButtonStyle.Apply(btnCancel, primary: false);
    }

    private async Task SearchAsync()
    {
        var token = ++_searchToken;
        if (SearchHandler is null) return;

        btnSearch.Enabled = false;
        try
        {
            var result = await SearchHandler(txtSearch.Text);
            if (IsDisposed || token != _searchToken) return;
            if (!result.Ok || result.Value is null)
            {
                ShowStatus(result.Message, true);
                return;
            }

            listContacts.BeginUpdate();
            listContacts.Items.Clear();
            foreach (var contact in result.Value)
            {
                var item = new ListViewItem(contact.FullName) { Tag = contact.Id };
                item.SubItems.Add(Dash(contact.Phone));
                item.SubItems.Add(Dash(contact.Email));
                listContacts.Items.Add(item);
            }
            listContacts.EndUpdate();
            FitColumns();
            ShowStatus(result.Value.Count == 0
                ? "No contacts found."
                : result.Value.Count == 1 ? "1 contact found." : $"{result.Value.Count} contacts found.", false);
        }
        finally
        {
            if (!IsDisposed && token == _searchToken)
                btnSearch.Enabled = true;
        }
    }

    private async Task AttachAsync()
    {
        if (SaveHandler is null) return;
        if (listContacts.SelectedItems.Count == 0 || listContacts.SelectedItems[0].Tag is not Guid contactId)
        {
            ShowStatus("Select a contact.", true);
            return;
        }

        btnAttach.Enabled = false;
        try
        {
            var result = await SaveHandler(contactId);
            if (IsDisposed) return;
            ShowStatus(result.Message, !result.Ok);
        }
        finally
        {
            if (!IsDisposed) btnAttach.Enabled = true;
        }
    }

    private void FitColumns()
    {
        colName.Width = LogicalToDeviceUnits(220);
        colPhone.Width = LogicalToDeviceUnits(150);
        colEmail.Width = Math.Max(LogicalToDeviceUnits(160),
            listContacts.ClientSize.Width - colName.Width - colPhone.Width);
    }

    private void ShowStatus(string text, bool isError)
    {
        if (IsDisposed) return;
        lblStatus.Text = text;
        lblStatus.ForeColor = isError ? ColorTranslator.FromHtml("#8F2A1B") : Theme.Muted;
    }

    private static string Dash(string? text) => string.IsNullOrWhiteSpace(text) ? "—" : text;

    private void txtSearch_TextChanged(object sender, EventArgs e)
    {

    }
}

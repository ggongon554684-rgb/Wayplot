using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.VisualBasic;
using System.ComponentModel;
using WayPlot.domain.Common;
using WayPlot.domain.Contracts;
using WayPlot.domain.Entities;
using WayPlot.Presenters;
using WayPlot.UI;

namespace WayPlot.Views;

/// <summary>The Contacts tab: a searchable list and the New contact dialog. Layout is in ContactsView.Designer.cs.</summary>
public partial class ContactsView : UserControl, IContactsView
{
    private ContactsPresenter? _presenter;

    public ContactsView()
    {
        InitializeComponent();
        ApplyTheme();

        listContacts.Resize += (_, _) => FitColumns();

        // Search waits for a short pause in typing, so every key press does not hit the database.
        txtSearch.TextChanged += (_, _) => { searchTimer.Stop(); searchTimer.Start(); };
        searchTimer.Tick += async (_, _) =>
        {
            searchTimer.Stop();
            if (_presenter is not null)
                await _presenter.LoadAsync(txtSearch.Text);
        };

        btnNew.Click += async (_, _) => await NewContactAsync();
        btnImport.Click += async (_, _) => await ImportCsvAsync();
        UpdateImportAction();
        FitColumns();
    }

    /// <summary>Set by the code that creates the screen (see Program.cs). The Designer never saves it.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ContactsPresenter? Presenter
    {
        set
        {
            _presenter = value;
            UpdateImportAction();
        }
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        UpdateImportAction();
        if (_presenter is not null && !DesignMode)
            await _presenter.LoadAsync();
    }

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        lblTitle.Font = Theme.Heading;
        lblTitle.ForeColor = Theme.Navy;
        lblSummary.Font = Theme.Body;
        lblSummary.ForeColor = Theme.Muted;
        txtSearch.Font = Theme.Body;
        txtSearch.PlaceholderText = "Search name, phone or email";
        listContacts.Font = Theme.Body;
        listContacts.BackColor = Theme.Surface;
        listContacts.ForeColor = Theme.Text;
        ButtonStyle.Apply(btnNew, primary: true);
    }

    private void FitColumns()
    {
        colName.Width = LogicalToDeviceUnits(210);
        colPhone.Width = LogicalToDeviceUnits(140);
        colSource.Width = LogicalToDeviceUnits(120);
        colType.Width = LogicalToDeviceUnits(100);
        colSms.Width = LogicalToDeviceUnits(100);
        colEmailConsent.Width = LogicalToDeviceUnits(110);

        var used = colName.Width + colPhone.Width + colSource.Width + colType.Width + colSms.Width + colEmailConsent.Width;
        colEmail.Width = Math.Max(0, listContacts.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - used);
    }

    private async Task NewContactAsync()
    {
        if (_presenter is null) return;

        var choices = await _presenter.GetReferralOptionsAsync();
        if (IsDisposed) return;
        if (!choices.Ok || choices.Value is null)
        {
            ShowStatus(choices.Message, true);
            return;
        }

        using var dialog = new ContactDialog
        {
            SaveHandler = _presenter.CaptureAsync,
            ReferralOptions = choices.Value
        };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.Result is not { } saved)
            return;

        // Show everyone again, so the new contact (or the matched one) is visible, then say what happened.
        txtSearch.Clear();
        searchTimer.Stop();
        await _presenter.LoadAsync(null, saved.Message);
    }

    private async Task ImportCsvAsync()
    {
        if (_presenter is null || !_presenter.CanImport) return;

        using var dialog = new ContactImportDialog
        {
            ColumnsHandler = _presenter.ReadImportColumns,
            PreviewHandler = _presenter.PreviewImportAsync,
            ImportHandler = _presenter.ImportCsvAsync,
            CanMapAgent = _presenter.CanAssignImportedContacts
        };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK || dialog.Result is not { } summary)
            return;

        await _presenter.LoadAsync(txtSearch.Text, ImportSummaryText(summary));
    }

    private static string ImportSummaryText(ContactImportSummary summary) =>
        $"Import complete: {summary.ContactsCreated} contacts, {summary.LeadsCreated} leads, " +
        $"{summary.Duplicates} duplicates, {summary.Rejected} rejected, {summary.Warnings} warnings.";

    private void UpdateImportAction()
    {
        if (_presenter is null)
        {
            ButtonStyle.ApplyAvailability(
            [
                new ButtonAvailability(btnNew, false, false, null, Primary: true),
                new ButtonAvailability(btnImport, false, false, null, Primary: false)
            ],
                actionToolTip);
            return;
        }

        var canCapture = _presenter.CanCapture;
        var canImport = _presenter.CanImport;
        ButtonStyle.ApplyAvailability(
            [
                new ButtonAvailability(btnNew, canCapture, canCapture,
                    canCapture ? null : _presenter.CaptureUnavailableReason, Primary: true),
                new ButtonAvailability(btnImport, canImport, canImport,
                    canImport ? null : "Only Agents and Tenant Admins can import contacts.", Primary: false)
            ],
            actionToolTip);
    }

    // ---- IContactsView ----

    public void ShowContacts(IReadOnlyList<ContactListItem> contacts)
    {
        if (IsDisposed) return;

        listContacts.BeginUpdate();
        listContacts.Items.Clear();
        foreach (var c in contacts)
        {
            var item = new ListViewItem(c.FullName) { Tag = c.Id };   // Tag: later steps open the contact from it
            item.SubItems.Add(Dash(c.Phone));
            item.SubItems.Add(Dash(c.Email));
            item.SubItems.Add(EnumText.Pretty(c.Source));
            item.SubItems.Add(EnumText.Pretty(c.Type));
            item.SubItems.Add(c.ConsentSms ? "Yes" : "—");
            item.SubItems.Add(c.ConsentEmail ? "Yes" : "—");
            listContacts.Items.Add(item);
        }
        listContacts.EndUpdate();
        FitColumns();
    }

    public void ShowStatus(string text, bool isError)
    {
        if (IsDisposed) return;
        lblSummary.Text = text;
        lblSummary.ForeColor = isError ? ColorTranslator.FromHtml("#8F2A1B") : Theme.Muted;
    }

    private static string Dash(string? text) => string.IsNullOrWhiteSpace(text) ? "—" : text;
}

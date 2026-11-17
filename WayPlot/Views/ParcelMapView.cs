using System.ComponentModel;
using System.Globalization;
using WayPlot.domain.Contracts;
using WayPlot.Presenters;
using WayPlot.UI;

namespace WayPlot.Views;

/// <summary>The Parcel map tab: the map on the left, the parcel inspector on the right. Layout is in ParcelMapView.Designer.cs.
/// The fact rows in the inspector are created in code (BuildFacts) because they are a fixed list of captions.</summary>
public partial class ParcelMapView : UserControl, IParcelMapView
{
    private static readonly string[] FactCaptions =
    [
        "Price", "Per sqm", "Area", "Zoning", "Reservation",
        "Owner", "Tax declaration", "Title no.", "Utilities", "Road access"
    ];

    private readonly List<Label> _factValues = [];
    private ParcelMapPresenter? _presenter;
    private Guid? _currentParcelId;

    public ParcelMapView()
    {
        InitializeComponent();
        BuildFacts();
        ApplyTheme();

        listLeads.Resize += (_, _) => FitLeadColumns();
        listLeads.ItemActivate += async (_, _) => await OpenLeadFileAsync();
        btnAttachLead.Click += (_, _) => AttachLead();
        mapControl.SelectionChanged += async (_, _) =>
        {
            if (_presenter is not null)
                await _presenter.SelectAsync(mapControl.SelectedId);
        };

        ClearParcel();
    }

    /// <summary>Set by the code that creates the screen (see Program.cs). The Designer never saves it.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ParcelMapPresenter? Presenter
    {
        set => _presenter = value;
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (_presenter is not null && !DesignMode)
            await _presenter.LoadAsync();
    }

    private void ApplyTheme()
    {
        BackColor = Theme.Page;
        layoutMain.BackColor = Theme.Page;
        lblPrompt.ForeColor = Theme.Muted;
        lblParcelLabel.ForeColor = Theme.Navy;
        lblDescription.ForeColor = Theme.Muted;
        lblLeadsHeader.ForeColor = Theme.Navy;
        listLeads.ForeColor = Theme.Text;
        listLeads.BackColor = Theme.Surface;
        ButtonStyle.Apply(btnAttachLead, primary: false);
    }

    private void BuildFacts()
    {
        facts.SuspendLayout();
        facts.RowStyles.Clear();
        facts.RowCount = FactCaptions.Length;

        for (var i = 0; i < FactCaptions.Length; i++)
        {
            facts.RowStyles.Add(new RowStyle());

            var caption = new Label
            {
                AutoSize = true,
                Text = FactCaptions[i],
                ForeColor = Theme.Muted,
                Margin = new Padding(0, 0, LogicalToDeviceUnits(14), LogicalToDeviceUnits(6)),
                UseMnemonic = false   // an owner such as "Santos & Sons" must not turn the & into an underline
            };
            var value = new Label
            {
                AutoSize = true,
                Text = string.Empty,
                ForeColor = Theme.Text,
                Margin = new Padding(0, 0, 0, LogicalToDeviceUnits(6)),
                MaximumSize = new Size(LogicalToDeviceUnits(170), 0),
                UseMnemonic = false
            };

            facts.Controls.Add(caption, 0, i);
            facts.Controls.Add(value, 1, i);
            _factValues.Add(value);
        }

        facts.ResumeLayout(true);
    }

    private void FitLeadColumns()
    {
        colStage.Width = LogicalToDeviceUnits(100);
        colContact.Width = Math.Max(LogicalToDeviceUnits(60), listLeads.ClientSize.Width - colStage.Width);
    }

    // ---- IParcelMapView ----

    public void ShowMessage(string? text)
    {
        if (IsDisposed) return;
        mapControl.SetMessage(text);
    }

    public void ShowShapes(IReadOnlyList<MapShape> shapes, int unplotted)
    {
        if (IsDisposed) return;
        mapControl.SetShapes(shapes);
        mapControl.SetNote(unplotted > 0
            ? $"{unplotted} parcel{(unplotted == 1 ? "" : "s")} not shown (no map position)."
            : null);
    }

    public void ShowParcel(ParcelDto parcel, IReadOnlyList<LeadDto>? leads)
    {
        if (IsDisposed) return;

        _currentParcelId = parcel.Id;
        lblPrompt.Visible = false;
        detailsLayout.Visible = true;

        lblParcelLabel.Text = parcel.Label;
        lblParcelStatus.Text = "● " + parcel.Status;
        lblParcelStatus.ForeColor = ParcelColors.For(parcel.Status);

        SetFact(0, Peso(parcel.Price));
        SetFact(1, Peso(parcel.PricePerSqm) + " / sqm");
        SetFact(2, parcel.AreaSqm.ToString("N0", CultureInfo.InvariantCulture) + " sqm");
        SetFact(3, parcel.Zoning);
        SetFact(4, $"{Peso(parcel.ReservationFee)} ({parcel.ReservationPercent.ToString("0.##", CultureInfo.InvariantCulture)}%)");
        SetFact(5, parcel.OwnerName);
        SetFact(6, parcel.TaxDeclarationNo);
        SetFact(7, parcel.TitleNo);
        SetFact(8, parcel.Utilities);
        SetFact(9, parcel.RoadAccess);

        lblDescription.Text = string.IsNullOrWhiteSpace(parcel.Description) ? "No description." : parcel.Description;

        listLeads.BeginUpdate();
        listLeads.Items.Clear();
        if (leads is null)
        {
            lblLeadsHeader.Text = "Leads (loading...)";
        }
        else
        {
            lblLeadsHeader.Text = leads.Count == 1 ? "1 lead" : $"{leads.Count} leads";
            foreach (var lead in leads)
            {
                var item = new ListViewItem(lead.ContactName) { Tag = lead.Id };   // Tag: step 5b opens the lead file from it
                item.SubItems.Add(lead.Stage.ToString());
                listLeads.Items.Add(item);
            }
        }
        listLeads.EndUpdate();
        FitLeadColumns();
    }

    public void ShowLeadsError(string message)
    {
        if (IsDisposed) return;
        listLeads.Items.Clear();
        lblLeadsHeader.Text = message;
    }

    public void ClearParcel()
    {
        if (IsDisposed) return;
        _currentParcelId = null;
        detailsLayout.Visible = false;
        lblPrompt.Visible = true;
    }

    private void AttachLead()
    {
        if (_presenter is null || _currentParcelId is not { } parcelId) return;

        using var dialog = new AttachLeadDialog
        {
            SearchHandler = _presenter.SearchContactsAsync,
            SaveHandler = contactId => _presenter.AttachLeadAsync(contactId, parcelId)
        };
        dialog.ShowDialog(FindForm());
    }

    private async Task OpenLeadFileAsync()
    {
        if (_presenter is null || listLeads.SelectedItems.Count == 0 ||
            listLeads.SelectedItems[0].Tag is not Guid leadId)
            return;

        var result = await _presenter.GetLeadFileAsync(leadId);
        if (IsDisposed) return;
        if (!result.Ok || result.Value is null)
        {
            MessageBox.Show(FindForm(), result.Message, "WayPlot CRM", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new LeadFileDialog(result.Value)
        {
            MoveHandler = _presenter.MoveStageAsync,
            LoadHandler = _presenter.GetLeadFileAsync
        };
        dialog.ShowDialog(FindForm());
    }

    private void SetFact(int index, string? text) =>
        _factValues[index].Text = string.IsNullOrWhiteSpace(text) ? "—" : text;

    private static string Peso(decimal amount) => "₱" + amount.ToString("N0", CultureInfo.InvariantCulture);
}
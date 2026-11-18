using System.ComponentModel;
using WayPlot.domain.Contracts;

namespace WayPlot.UI;

public partial class ParcelPickerControl : UserControl
{
    public ParcelPickerControl()
    {
        InitializeComponent();
        txtSearch.TextChanged += (_, _) => ApplyFilter();
        listParcels.SelectedIndexChanged += (_, _) =>
            SelectedChanged?.Invoke(this, EventArgs.Empty);
        ApplyTheme();
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Guid? ExcludeParcelId { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ParcelDto? SelectedParcel =>
        listParcels.SelectedItem as ParcelDto;

    public event EventHandler? SelectedChanged;

    public void SetParcels(IEnumerable<ParcelDto> parcels)
    {
        _parcels = parcels
            .Where(parcel => parcel.Status == WayPlot.domain.Common.ParcelStatus.Available)
            .Where(parcel => ExcludeParcelId is null || parcel.Id != ExcludeParcelId)
            .ToList();
        ApplyFilter();
    }

    private IReadOnlyList<ParcelDto> _parcels = [];

    private void ApplyTheme()
    {
        txtSearch.Font = Theme.Body;
        txtSearch.PlaceholderText = "Search available parcels";
        listParcels.Font = Theme.Body;
        listParcels.BackColor = Theme.Surface;
    }

    private void ApplyFilter()
    {
        listParcels.Items.Clear();
        var search = txtSearch.Text.Trim();
        foreach (var parcel in _parcels)
        {
            if (!string.IsNullOrEmpty(search)
                && !parcel.Label.Contains(search, StringComparison.OrdinalIgnoreCase))
                continue;
            listParcels.Items.Add(parcel);
        }
    }
}

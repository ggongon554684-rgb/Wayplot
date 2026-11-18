using WayPlot.domain.Contracts;
using WayPlot.UI;

namespace WayPlot.Views;

public partial class ChangePriceDialog : Form
{
    private readonly Guid _parcelId;

    public ChangePriceDialog(Guid parcelId, decimal price, decimal reservationFee)
    {
        _parcelId = parcelId;
        InitializeComponent();
        BackColor = Theme.Page;
        Font = Theme.Body;
        lblTitle.Font = Theme.Heading;
        lblTitle.ForeColor = Theme.Navy;
        lblError.ForeColor = Color.Firebrick;
        numPrice.Value = price;
        numReservationFee.Value = reservationFee;
        ButtonStyle.Apply(btnSave, primary: true);
        ButtonStyle.Apply(btnCancel, primary: false);
        btnSave.Click += (_, _) => Save();
        btnCancel.Click += (_, _) => Close();
    }

    public ChangeParcelPriceRequest? Request { get; private set; }

    private void Save()
    {
        if (numReservationFee.Value > numPrice.Value)
        {
            lblError.Text = "The reservation fee cannot be higher than the price.";
            return;
        }

        Request = new ChangeParcelPriceRequest(_parcelId, numPrice.Value, numReservationFee.Value);
        DialogResult = DialogResult.OK;
        Close();
    }
}

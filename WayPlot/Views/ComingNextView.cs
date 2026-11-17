using System.ComponentModel;
using WayPlot.UI;

namespace WayPlot.Views;

/// <summary>Placeholder card for tabs that are not built yet. Layout is in ComingNextView.Designer.cs.</summary>
public partial class ComingNextView : UserControl
{
    public ComingNextView()
    {
        InitializeComponent();
        lblTitle.ForeColor = Theme.Text;
        lblBody.ForeColor = Theme.Muted;
    }

    /// <summary>Set from code when the view is shown. The Designer never saves it.</summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Title
    {
        set => lblTitle.Text = value;
    }
}
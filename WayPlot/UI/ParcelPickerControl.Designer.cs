using WayPlot.domain.Contracts;

#nullable disable
namespace WayPlot.UI
{
    partial class ParcelPickerControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            layout = new TableLayoutPanel();
            txtSearch = new TextBox();
            listParcels = new ListBox();
            layout.SuspendLayout();
            SuspendLayout();

            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(txtSearch, 0, 0);
            layout.Controls.Add(listParcels, 0, 1);
            layout.Dock = DockStyle.Fill;
            layout.RowCount = 2;
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.Name = "layout";

            txtSearch.Dock = DockStyle.Fill;
            txtSearch.Margin = new Padding(0, 0, 0, 8);
            txtSearch.Name = "txtSearch";

            listParcels.Dock = DockStyle.Fill;
            listParcels.DrawMode = DrawMode.OwnerDrawFixed;
            listParcels.ItemHeight = 44;
            listParcels.Name = "listParcels";
            listParcels.DrawItem += (_, args) =>
            {
                if (args.Index < 0) return;
                args.DrawBackground();
                if (listParcels.Items[args.Index] is ParcelDto parcel)
                {
                    var labelRect = new Rectangle(args.Bounds.X + 8, args.Bounds.Y + 4,
                        args.Bounds.Width - 16, 18);
                    var detailRect = new Rectangle(args.Bounds.X + 8, args.Bounds.Y + 22,
                        args.Bounds.Width - 16, 18);
                    TextRenderer.DrawText(args.Graphics, parcel.Label, Theme.BodyBold, labelRect, Theme.Text,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                    TextRenderer.DrawText(args.Graphics,
                        $"₱{parcel.Price:N0}  |  {parcel.AreaSqm:N1} sqm",
                        Theme.Small, detailRect, Theme.Muted,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                }
                args.DrawFocusRectangle();
            };

            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(layout);
            Name = "ParcelPickerControl";
            Size = new Size(420, 220);
            layout.ResumeLayout(false);
            layout.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel layout;
        private TextBox txtSearch;
        private ListBox listParcels;
    }
}

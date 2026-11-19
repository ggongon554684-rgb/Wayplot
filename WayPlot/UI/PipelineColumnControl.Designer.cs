#nullable disable
namespace WayPlot.UI
{
    partial class PipelineColumnControl
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
            lblHeader = new Label();
            lblNotice = new Label();
            cards = new FlowLayoutPanel();
            layout.SuspendLayout();
            SuspendLayout();
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            // Without these row styles the cards row only grows to its content, so the
            // card list never fills the column and shows stray scrollbars.
            layout.RowCount = 3;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(8);
            layout.Controls.Add(lblHeader, 0, 0);
            layout.Controls.Add(lblNotice, 0, 1);
            layout.Controls.Add(cards, 0, 2);
            lblHeader.AutoSize = true;
            lblHeader.Margin = new Padding(4, 4, 4, 8);
            lblNotice.AutoSize = true;
            lblNotice.MaximumSize = new Size(280, 0);
            lblNotice.Margin = new Padding(4, 0, 4, 6);
            lblNotice.Visible = false;
            cards.Dock = DockStyle.Fill;
            cards.Margin = new Padding(0);
            cards.FlowDirection = FlowDirection.TopDown;
            cards.WrapContents = false;
            cards.AutoScroll = true;
            Controls.Add(layout);
            MinimumSize = new Size(300, 240);
            Size = new Size(300, 500);
            Name = "PipelineColumnControl";
            layout.ResumeLayout(false);
            layout.PerformLayout();
            ResumeLayout(false);
        }
        private TableLayoutPanel layout;
        private Label lblHeader;
        private Label lblNotice;
        private FlowLayoutPanel cards;
    }
}

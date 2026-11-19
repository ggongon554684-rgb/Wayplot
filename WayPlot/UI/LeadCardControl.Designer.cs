#nullable disable
namespace WayPlot.UI
{
    partial class LeadCardControl
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
            lblContact = new Label();
            lblParcel = new Label();
            lblStageInfo = new Label();
            lblActivity = new Label();
            lblAgent = new Label();
            lblClosed = new Label();
            lblDeal = new Label();
            btnMove = new Button();
            layout.SuspendLayout();
            SuspendLayout();
            layout.ColumnCount = 1;
            layout.Dock = DockStyle.Top;
            layout.AutoSize = true;
            layout.Padding = new Padding(10);
            layout.Controls.Add(lblContact, 0, 0);
            layout.Controls.Add(lblParcel, 0, 1);
            layout.Controls.Add(lblStageInfo, 0, 2);
            layout.Controls.Add(lblActivity, 0, 3);
            layout.Controls.Add(lblAgent, 0, 4);
            layout.Controls.Add(lblClosed, 0, 5);
            layout.Controls.Add(lblDeal, 0, 6);
            layout.Controls.Add(btnMove, 0, 7);
            foreach (var label in new[] { lblContact, lblParcel, lblStageInfo, lblActivity, lblAgent, lblClosed, lblDeal })
            {
                label.AutoSize = true;
                label.MaximumSize = new Size(220, 0);
            }
            lblClosed.Text = "Closed requested";
            lblClosed.Visible = false;
            lblDeal.Visible = false;
            btnMove.Text = "Move to...";
            btnMove.Visible = false;
            btnMove.AutoSize = true;
            btnMove.Click += (_, _) => { if (Card is not null) MoveRequested?.Invoke(Card); };
            BackColor = Theme.Surface;
            BorderStyle = BorderStyle.FixedSingle;
            Cursor = Cursors.Hand;
            Controls.Add(layout);
            Name = "LeadCardControl";
            // Height follows the content (admin cards show Agent, Closed and Deal lines); width is set by the column.
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowOnly;
            Size = new Size(260, 130);
            layout.ResumeLayout(false);
            layout.PerformLayout();
            ResumeLayout(false);
        }
        private TableLayoutPanel layout;
        private Label lblContact;
        private Label lblParcel;
        private Label lblStageInfo;
        private Label lblActivity;
        private Label lblAgent;
        private Label lblClosed;
        private Label lblDeal;
        private Button btnMove;
    }
}

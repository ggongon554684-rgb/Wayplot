#nullable disable
using WayPlot.UI;

namespace WayPlot.UI
{
    partial class DealCardControl
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
            header = new TableLayoutPanel();
            lblParcel = new Label();
            lblStatus = new Label();
            btnShowMap = new Button();
            money = new TableLayoutPanel();
            lblNet = new Label();
            lblMoney = new Label();
            moneyBar = new MoneyBarControl();
            lblNext = new Label();
            lblOverdue = new Label();
            lblPenalty = new Label();
            lblTermination = new Label();
            actions = new FlowLayoutPanel();
            btnPayments = new Button();
            paymentsPanel = new Panel();
            paymentsList = new FlowLayoutPanel();
            layout.SuspendLayout();
            header.SuspendLayout();
            money.SuspendLayout();
            actions.SuspendLayout();
            paymentsPanel.SuspendLayout();
            SuspendLayout();

            layout.ColumnCount = 1;
            layout.Dock = DockStyle.Top;
            layout.AutoSize = true;
            layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            layout.Padding = new Padding(12);
            layout.Controls.Add(header, 0, 0);
            layout.Controls.Add(money, 0, 1);
            layout.Controls.Add(lblNext, 0, 2);
            layout.Controls.Add(lblOverdue, 0, 3);
            layout.Controls.Add(lblPenalty, 0, 4);
            layout.Controls.Add(lblTermination, 0, 5);
            layout.Controls.Add(actions, 0, 6);
            layout.Controls.Add(paymentsPanel, 0, 7);
            layout.Margin = new Padding(0, 0, 0, 12);

            header.ColumnCount = 3;
            header.AutoSize = true;
            header.Dock = DockStyle.Fill;
            header.Controls.Add(lblParcel, 0, 0);
            header.Controls.Add(lblStatus, 1, 0);
            header.Controls.Add(btnShowMap, 2, 0);
            lblParcel.AutoSize = true;
            lblStatus.AutoSize = true;
            btnShowMap.AutoSize = true;
            btnShowMap.Text = "Show on map";

            money.ColumnCount = 1;
            money.AutoSize = true;
            money.Dock = DockStyle.Fill;
            money.Controls.Add(lblNet, 0, 0);
            money.Controls.Add(lblMoney, 0, 1);
            money.Controls.Add(moneyBar, 0, 2);
            lblNet.AutoSize = true;
            lblMoney.AutoSize = true;

            lblNext.AutoSize = true;
            lblOverdue.AutoSize = true;
            lblPenalty.AutoSize = true;
            lblTermination.AutoSize = true;
            lblTermination.MaximumSize = new Size(520, 0);

            actions.Dock = DockStyle.Fill;
            actions.AutoSize = true;
            actions.FlowDirection = FlowDirection.LeftToRight;
            actions.Controls.Add(btnPayments);
            btnPayments.Text = "Payments";

            paymentsPanel.Dock = DockStyle.Top;
            paymentsPanel.AutoSize = true;
            paymentsPanel.Visible = false;
            paymentsPanel.Controls.Add(paymentsList);
            paymentsList.Dock = DockStyle.Top;
            paymentsList.AutoSize = true;
            paymentsList.FlowDirection = FlowDirection.TopDown;
            paymentsList.WrapContents = false;

            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(layout);
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            MinimumSize = new Size(320, 220);
            Name = "DealCardControl";
            Size = new Size(520, 260);

            layout.ResumeLayout(false);
            layout.PerformLayout();
            header.ResumeLayout(false);
            header.PerformLayout();
            money.ResumeLayout(false);
            money.PerformLayout();
            actions.ResumeLayout(false);
            actions.PerformLayout();
            paymentsPanel.ResumeLayout(false);
            paymentsPanel.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel layout;
        private TableLayoutPanel header;
        private Label lblParcel;
        private Label lblStatus;
        private Button btnShowMap;
        private TableLayoutPanel money;
        private Label lblNet;
        private Label lblMoney;
        private MoneyBarControl moneyBar;
        private Label lblNext;
        private Label lblOverdue;
        private Label lblPenalty;
        private Label lblTermination;
        private FlowLayoutPanel actions;
        private Button btnPayments;
        private Panel paymentsPanel;
        private FlowLayoutPanel paymentsList;
    }
}

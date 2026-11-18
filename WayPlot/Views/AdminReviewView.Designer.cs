#nullable disable
namespace WayPlot.Views
{
    partial class AdminReviewView
    {
        private System.ComponentModel.IContainer components = null;
        private TableLayoutPanel root;
        private TableLayoutPanel header;
        private Label lblTitle;
        private Button btnRefresh;
        private Label lblStatus;
        private SplitContainer split;
        private ListView listQueue;
        private ColumnHeader colItem;
        private ColumnHeader colStatus;
        private Panel detailHost;
        private WayPlot.UI.DealCardControl detailCard;
        private FlowLayoutPanel actions;
        private Button btnConfirmClosed;
        private Button btnReturnClosed;
        private Button btnConfirmPayment;
        private Button btnReturnPayment;
        private Button btnApprove;
        private Button btnMarkPaid;
        private ToolTip actionToolTip;

        protected override void Dispose(bool disposing)
        {
            if (disposing) components?.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            root = new TableLayoutPanel();
            header = new TableLayoutPanel();
            lblTitle = new Label();
            btnRefresh = new Button();
            lblStatus = new Label();
            split = new SplitContainer();
            listQueue = new ListView();
            colItem = new ColumnHeader();
            colStatus = new ColumnHeader();
            detailHost = new Panel();
            detailCard = new WayPlot.UI.DealCardControl();
            actions = new FlowLayoutPanel();
            btnConfirmClosed = new Button();
            btnReturnClosed = new Button();
            btnConfirmPayment = new Button();
            btnReturnPayment = new Button();
            btnApprove = new Button();
            btnMarkPaid = new Button();
            actionToolTip = new ToolTip(components);
            root.SuspendLayout();
            header.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)split).BeginInit();
            split.Panel1.SuspendLayout();
            split.Panel2.SuspendLayout();
            split.SuspendLayout();
            actions.SuspendLayout();
            SuspendLayout();
            root.ColumnCount = 1;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.Controls.Add(header, 0, 0);
            root.Controls.Add(lblStatus, 0, 1);
            root.Controls.Add(split, 0, 2);
            root.Controls.Add(actions, 0, 3);
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(18);
            root.RowCount = 4;
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            header.ColumnCount = 2;
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.Controls.Add(lblTitle, 0, 0);
            header.Controls.Add(btnRefresh, 1, 0);
            header.Dock = DockStyle.Fill;
            header.Margin = new Padding(0, 0, 0, 8);
            lblTitle.AutoSize = true;
            lblTitle.Text = "Review";
            lblTitle.UseMnemonic = false;
            btnRefresh.AutoSize = true;
            btnRefresh.Text = "Refresh";
            btnRefresh.UseVisualStyleBackColor = false;
            lblStatus.AutoSize = true;
            lblStatus.Margin = new Padding(0, 0, 0, 8);
            lblStatus.Text = "Loading review queue...";
            lblStatus.UseMnemonic = false;
            split.Dock = DockStyle.Fill;
            split.FixedPanel = FixedPanel.Panel2;
            split.SplitterDistance = 650;
            split.Panel1.Controls.Add(listQueue);
            split.Panel2.Controls.Add(detailHost);
            listQueue.BorderStyle = BorderStyle.None;
            listQueue.Columns.AddRange(new ColumnHeader[] { colItem, colStatus });
            listQueue.Dock = DockStyle.Fill;
            listQueue.FullRowSelect = true;
            listQueue.HideSelection = false;
            listQueue.MultiSelect = false;
            listQueue.ShowGroups = true;
            listQueue.View = View.Details;
            colItem.Text = "Review item";
            colItem.Width = 380;
            colStatus.Text = "Status";
            colStatus.Width = 160;
            detailHost.Dock = DockStyle.Fill;
            detailHost.Padding = new Padding(8);
            detailCard.Dock = DockStyle.Fill;
            detailHost.Controls.Add(detailCard);
            actions.AutoSize = true;
            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.RightToLeft;
            actions.WrapContents = true;
            actions.Margin = new Padding(0, 10, 0, 0);
            foreach (var button in new[] { btnConfirmClosed, btnReturnClosed, btnConfirmPayment,
                         btnReturnPayment, btnApprove, btnMarkPaid })
            {
                button.AutoSize = true;
                button.Margin = new Padding(6, 0, 0, 0);
                button.UseVisualStyleBackColor = false;
                actions.Controls.Add(button);
            }
            btnConfirmClosed.Text = "Confirm Closed";
            btnReturnClosed.Text = "Return request";
            btnConfirmPayment.Text = "Confirm payment";
            btnReturnPayment.Text = "Return payment";
            btnApprove.Text = "Approve payout";
            btnMarkPaid.Text = "Mark Paid";
            Controls.Add(root);
            Name = "AdminReviewView";
            Size = new Size(1100, 700);
            root.ResumeLayout(false);
            root.PerformLayout();
            header.ResumeLayout(false);
            header.PerformLayout();
            split.Panel1.ResumeLayout(false);
            split.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)split).EndInit();
            split.ResumeLayout(false);
            actions.ResumeLayout(false);
            actions.PerformLayout();
            ResumeLayout(false);
        }
    }
}

#nullable disable
namespace WayPlot.Views
{
    partial class CommissionsView
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            card = new WayPlot.UI.RoundedPanel();
            layout = new TableLayoutPanel();
            header = new TableLayoutPanel();
            lblTitle = new Label();
            toolbar = new FlowLayoutPanel();
            btnRules = new Button();
            btnApprove = new Button();
            btnMarkPaid = new Button();
            lblHelp = new Label();
            lblSummary = new Label();
            listPayouts = new ListView();
            colKind = new ColumnHeader();
            colRecipient = new ColumnHeader();
            colContact = new ColumnHeader();
            colParcel = new ColumnHeader();
            colAmount = new ColumnHeader();
            colStatus = new ColumnHeader();
            colCreated = new ColumnHeader();
            colPaid = new ColumnHeader();
            actionToolTip = new ToolTip(components);
            card.Controls.Add(layout);
            card.Dock = DockStyle.Fill;
            card.Name = "card";
            card.TabIndex = 0;
            layout.Controls.Add(header, 0, 0);
            layout.Controls.Add(lblHelp, 0, 1);
            layout.Controls.Add(lblSummary, 0, 2);
            layout.Controls.Add(listPayouts, 0, 3);
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Dock = DockStyle.Fill;
            layout.Name = "layout";
            layout.Padding = new Padding(24);
            layout.RowCount = 4;
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            header.Controls.Add(lblTitle, 0, 0);
            header.Controls.Add(toolbar, 1, 0);
            header.ColumnCount = 2;
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.Dock = DockStyle.Fill;
            header.Margin = new Padding(0, 0, 0, 8);
            header.Name = "header";
            header.RowCount = 1;
            header.RowStyles.Add(new RowStyle());
            lblTitle.Anchor = AnchorStyles.Left;
            lblTitle.AutoSize = true;
            lblTitle.Margin = new Padding(0);
            lblTitle.Name = "lblTitle";
            lblTitle.Text = "Commissions & referral bonuses";
            lblTitle.UseMnemonic = false;
            toolbar.Controls.Add(btnRules);
            toolbar.Controls.Add(btnApprove);
            toolbar.Controls.Add(btnMarkPaid);
            toolbar.AutoSize = true;
            toolbar.Dock = DockStyle.Fill;
            toolbar.FlowDirection = FlowDirection.RightToLeft;
            toolbar.Margin = new Padding(0);
            toolbar.Name = "toolbar";
            toolbar.WrapContents = false;
            btnRules.AutoSize = true;
            btnRules.Margin = new Padding(8, 0, 0, 0);
            btnRules.Name = "btnRules";
            btnRules.Text = "Commission rules";
            btnRules.UseVisualStyleBackColor = false;
            btnApprove.AutoSize = true;
            btnApprove.Margin = new Padding(8, 0, 0, 0);
            btnApprove.Name = "btnApprove";
            btnApprove.Text = "Approve";
            btnApprove.UseVisualStyleBackColor = false;
            btnMarkPaid.AutoSize = true;
            btnMarkPaid.Margin = new Padding(8, 0, 0, 0);
            btnMarkPaid.Name = "btnMarkPaid";
            btnMarkPaid.Text = "Mark Paid";
            btnMarkPaid.UseVisualStyleBackColor = false;
            lblHelp.AutoSize = true;
            lblHelp.Margin = new Padding(0, 0, 0, 8);
            lblHelp.Name = "lblHelp";
            lblHelp.Text = "Tenant Admins configure rules and manage payouts. Agents see their own; Branch Managers see their branch. Paid payouts flagged for review need a Tenant Admin decision.";
            lblHelp.UseMnemonic = false;
            lblSummary.Anchor = AnchorStyles.Left;
            lblSummary.AutoSize = true;
            lblSummary.Margin = new Padding(0, 2, 0, 6);
            lblSummary.Name = "lblSummary";
            lblSummary.Text = "Loading commissions...";
            lblSummary.UseMnemonic = false;
            listPayouts.BorderStyle = BorderStyle.None;
            listPayouts.Columns.AddRange(new ColumnHeader[] { colKind, colRecipient, colContact, colParcel, colAmount, colStatus, colCreated, colPaid });
            listPayouts.Dock = DockStyle.Fill;
            listPayouts.FullRowSelect = true;
            listPayouts.HideSelection = false;
            listPayouts.Margin = new Padding(0);
            listPayouts.MultiSelect = false;
            listPayouts.Name = "listPayouts";
            listPayouts.UseCompatibleStateImageBehavior = false;
            listPayouts.View = View.Details;
            colKind.Text = "Payout";
            colKind.Width = 125;
            colRecipient.Text = "Recipient";
            colRecipient.Width = 155;
            colContact.Text = "Contact";
            colContact.Width = 180;
            colParcel.Text = "Parcel";
            colParcel.Width = 135;
            colAmount.Text = "Amount";
            colAmount.Width = 115;
            colStatus.Text = "Status";
            colStatus.Width = 120;
            colCreated.Text = "Created";
            colCreated.Width = 145;
            colPaid.Text = "Paid date";
            colPaid.Width = 145;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(card);
            Name = "CommissionsView";
            Size = new Size(1100, 560);
        }

        private WayPlot.UI.RoundedPanel card;
        private TableLayoutPanel layout;
        private TableLayoutPanel header;
        private Label lblTitle;
        private FlowLayoutPanel toolbar;
        private Button btnRules;
        private Button btnApprove;
        private Button btnMarkPaid;
        private Label lblHelp;
        private Label lblSummary;
        private ListView listPayouts;
        private ColumnHeader colKind;
        private ColumnHeader colRecipient;
        private ColumnHeader colContact;
        private ColumnHeader colParcel;
        private ColumnHeader colAmount;
        private ColumnHeader colStatus;
        private ColumnHeader colCreated;
        private ColumnHeader colPaid;
        private ToolTip actionToolTip;
    }
}

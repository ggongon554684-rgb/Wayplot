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
            btnCompanyProfile = new Button();
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
            card.SuspendLayout();
            layout.SuspendLayout();
            header.SuspendLayout();
            toolbar.SuspendLayout();
            SuspendLayout();
            // 
            // card
            // 
            card.BackColor = Color.FromArgb(245, 247, 251);
            card.BorderColor = Color.FromArgb(227, 232, 242);
            card.Controls.Add(layout);
            card.Dock = DockStyle.Fill;
            card.Location = new Point(0, 0);
            card.Margin = new Padding(4, 4, 4, 4);
            card.Name = "card";
            card.Padding = new Padding(30, 30, 30, 30);
            card.Radius = 24;
            card.Size = new Size(1375, 700);
            card.TabIndex = 0;
            // 
            // layout
            // 
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(header, 0, 0);
            layout.Controls.Add(lblHelp, 0, 1);
            layout.Controls.Add(lblSummary, 0, 2);
            layout.Controls.Add(listPayouts, 0, 3);
            layout.Dock = DockStyle.Fill;
            layout.Location = new Point(30, 30);
            layout.Margin = new Padding(4, 4, 4, 4);
            layout.Name = "layout";
            layout.Padding = new Padding(30, 30, 30, 30);
            layout.RowCount = 4;
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.Size = new Size(1315, 640);
            layout.TabIndex = 0;
            // 
            // header
            // 
            header.ColumnCount = 2;
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.ColumnStyles.Add(new ColumnStyle());
            header.Controls.Add(lblTitle, 0, 0);
            header.Controls.Add(toolbar, 1, 0);
            header.Dock = DockStyle.Fill;
            header.Location = new Point(30, 30);
            header.Margin = new Padding(0, 0, 0, 10);
            header.Name = "header";
            header.RowCount = 1;
            header.RowStyles.Add(new RowStyle());
            header.Size = new Size(1255, 125);
            header.TabIndex = 0;
            // 
            // lblTitle
            // 
            lblTitle.Anchor = AnchorStyles.Left;
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(0, 52);
            lblTitle.Margin = new Padding(0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(222, 20);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Commissions & referral bonuses";
            lblTitle.UseMnemonic = false;
            // 
            // toolbar
            // 
            toolbar.AutoSize = true;
            toolbar.Controls.Add(btnRules);
            toolbar.Controls.Add(btnCompanyProfile);
            toolbar.Controls.Add(btnApprove);
            toolbar.Controls.Add(btnMarkPaid);
            toolbar.Dock = DockStyle.Fill;
            toolbar.FlowDirection = FlowDirection.RightToLeft;
            toolbar.Location = new Point(856, 0);
            toolbar.Margin = new Padding(0);
            toolbar.Name = "toolbar";
            toolbar.Size = new Size(399, 125);
            toolbar.TabIndex = 1;
            toolbar.WrapContents = false;
            // 
            // btnRules
            // 
            btnRules.AutoSize = true;
            btnRules.Location = new Point(230, 0);
            btnRules.Margin = new Padding(10, 0, 0, 0);
            btnRules.Name = "btnRules";
            btnRules.Size = new Size(169, 38);
            btnRules.TabIndex = 0;
            btnRules.Text = "Commission rules";
            btnRules.UseVisualStyleBackColor = false;
            btnCompanyProfile.AutoSize = true;
            btnCompanyProfile.Location = new Point(0, 0);
            btnCompanyProfile.Margin = new Padding(10, 0, 0, 0);
            btnCompanyProfile.Name = "btnCompanyProfile";
            btnCompanyProfile.TabIndex = 3;
            btnCompanyProfile.Text = "Company profile";
            btnCompanyProfile.UseVisualStyleBackColor = false;
            // 
            // btnApprove
            // 
            btnApprove.AutoSize = true;
            btnApprove.Location = new Point(125, 0);
            btnApprove.Margin = new Padding(10, 0, 0, 0);
            btnApprove.Name = "btnApprove";
            btnApprove.Size = new Size(95, 38);
            btnApprove.TabIndex = 1;
            btnApprove.Text = "Approve";
            btnApprove.UseVisualStyleBackColor = false;
            // 
            // btnMarkPaid
            // 
            btnMarkPaid.AutoSize = true;
            btnMarkPaid.Location = new Point(10, 0);
            btnMarkPaid.Margin = new Padding(10, 0, 0, 0);
            btnMarkPaid.Name = "btnMarkPaid";
            btnMarkPaid.Size = new Size(105, 38);
            btnMarkPaid.TabIndex = 2;
            btnMarkPaid.Text = "Mark Paid";
            btnMarkPaid.UseVisualStyleBackColor = false;
            // 
            // lblHelp
            // 
            lblHelp.AutoSize = true;
            lblHelp.Location = new Point(30, 165);
            lblHelp.Margin = new Padding(0, 0, 0, 10);
            lblHelp.Name = "lblHelp";
            lblHelp.Size = new Size(1151, 20);
            lblHelp.TabIndex = 1;
            lblHelp.Text = "Tenant Admins configure rules and manage payouts. Agents see their own; Branch Managers see their branch. Paid payouts flagged for review need a Tenant Admin decision.";
            lblHelp.UseMnemonic = false;
            // 
            // lblSummary
            // 
            lblSummary.Anchor = AnchorStyles.Left;
            lblSummary.AutoSize = true;
            lblSummary.Location = new Point(30, 197);
            lblSummary.Margin = new Padding(0, 2, 0, 8);
            lblSummary.Name = "lblSummary";
            lblSummary.Size = new Size(161, 20);
            lblSummary.TabIndex = 2;
            lblSummary.Text = "Loading commissions...";
            lblSummary.UseMnemonic = false;
            // 
            // listPayouts
            // 
            listPayouts.BorderStyle = BorderStyle.None;
            listPayouts.Columns.AddRange(new ColumnHeader[] { colKind, colRecipient, colContact, colParcel, colAmount, colStatus, colCreated, colPaid });
            listPayouts.Dock = DockStyle.Fill;
            listPayouts.FullRowSelect = true;
            listPayouts.Location = new Point(30, 225);
            listPayouts.Margin = new Padding(0);
            listPayouts.MultiSelect = false;
            listPayouts.Name = "listPayouts";
            listPayouts.Size = new Size(1255, 385);
            listPayouts.TabIndex = 3;
            listPayouts.UseCompatibleStateImageBehavior = false;
            listPayouts.View = View.Details;
            // 
            // colKind
            // 
            colKind.Text = "Payout";
            colKind.Width = 125;
            // 
            // colRecipient
            // 
            colRecipient.Text = "Recipient";
            colRecipient.Width = 155;
            // 
            // colContact
            // 
            colContact.Text = "Contact";
            colContact.Width = 180;
            // 
            // colParcel
            // 
            colParcel.Text = "Parcel";
            colParcel.Width = 135;
            // 
            // colAmount
            // 
            colAmount.Text = "Amount";
            colAmount.Width = 115;
            // 
            // colStatus
            // 
            colStatus.Text = "Status";
            colStatus.Width = 120;
            // 
            // colCreated
            // 
            colCreated.Text = "Created";
            colCreated.Width = 145;
            // 
            // colPaid
            // 
            colPaid.Text = "Paid date";
            colPaid.Width = 145;
            // 
            // CommissionsView
            // 
            AutoScaleDimensions = new SizeF(120F, 120F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(card);
            Margin = new Padding(4, 4, 4, 4);
            Name = "CommissionsView";
            Size = new Size(1375, 700);
            card.ResumeLayout(false);
            layout.ResumeLayout(false);
            layout.PerformLayout();
            header.ResumeLayout(false);
            header.PerformLayout();
            toolbar.ResumeLayout(false);
            toolbar.PerformLayout();
            ResumeLayout(false);
        }

        private WayPlot.UI.RoundedPanel card;
        private TableLayoutPanel layout;
        private TableLayoutPanel header;
        private Label lblTitle;
        private FlowLayoutPanel toolbar;
        private Button btnRules;
        private Button btnCompanyProfile;
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

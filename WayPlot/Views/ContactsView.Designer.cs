#nullable disable
namespace WayPlot.Views
{
    partial class ContactsView
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
            toolbar = new TableLayoutPanel();
            lblTitle = new Label();
            txtSearch = new TextBox();
            btnNew = new Button();
            lblSummary = new Label();
            listContacts = new ListView();
            colName = new ColumnHeader();
            colPhone = new ColumnHeader();
            colEmail = new ColumnHeader();
            colSource = new ColumnHeader();
            colType = new ColumnHeader();
            colSms = new ColumnHeader();
            colEmailConsent = new ColumnHeader();
            searchTimer = new System.Windows.Forms.Timer(components);
            card.SuspendLayout();
            layout.SuspendLayout();
            toolbar.SuspendLayout();
            SuspendLayout();
            // 
            // card
            // 
            card.Controls.Add(layout);
            card.Dock = DockStyle.Fill;
            card.Location = new Point(0, 0);
            card.Name = "card";
            card.Size = new Size(1100, 560);
            card.TabIndex = 0;
            // 
            // layout
            // 
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(toolbar, 0, 0);
            layout.Controls.Add(lblSummary, 0, 1);
            layout.Controls.Add(listContacts, 0, 2);
            layout.Dock = DockStyle.Fill;
            layout.Location = new Point(24, 24);
            layout.Name = "layout";
            layout.RowCount = 3;
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.Size = new Size(1052, 512);
            layout.TabIndex = 0;
            // 
            // toolbar
            // 
            toolbar.AutoSize = true;
            toolbar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            toolbar.ColumnCount = 3;
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300F));
            toolbar.ColumnStyles.Add(new ColumnStyle());
            toolbar.Controls.Add(lblTitle, 0, 0);
            toolbar.Controls.Add(txtSearch, 1, 0);
            toolbar.Controls.Add(btnNew, 2, 0);
            toolbar.Dock = DockStyle.Fill;
            toolbar.Margin = new Padding(0);
            toolbar.Name = "toolbar";
            toolbar.RowCount = 1;
            toolbar.RowStyles.Add(new RowStyle());
            toolbar.Size = new Size(1052, 40);
            toolbar.TabIndex = 0;
            // 
            // lblTitle
            // 
            lblTitle.Anchor = AnchorStyles.Left;
            lblTitle.AutoSize = true;
            lblTitle.Margin = new Padding(0);
            lblTitle.Name = "lblTitle";
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Contacts";
            lblTitle.UseMnemonic = false;
            // 
            // txtSearch
            // 
            txtSearch.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtSearch.Margin = new Padding(0, 0, 12, 0);
            txtSearch.Name = "txtSearch";
            txtSearch.TabIndex = 1;
            // 
            // btnNew
            // 
            btnNew.AutoSize = true;
            btnNew.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnNew.Margin = new Padding(0);
            btnNew.Name = "btnNew";
            btnNew.TabIndex = 2;
            btnNew.Text = "New contact";
            btnNew.UseVisualStyleBackColor = false;
            // 
            // lblSummary
            // 
            lblSummary.Anchor = AnchorStyles.Left;
            lblSummary.AutoSize = true;
            lblSummary.Margin = new Padding(0, 10, 0, 10);
            lblSummary.Name = "lblSummary";
            lblSummary.TabIndex = 1;
            lblSummary.Text = "Loading contacts...";
            lblSummary.UseMnemonic = false;
            // 
            // listContacts
            // 
            listContacts.BorderStyle = BorderStyle.None;
            listContacts.Columns.AddRange(new ColumnHeader[] { colName, colPhone, colEmail, colSource, colType, colSms, colEmailConsent });
            listContacts.Dock = DockStyle.Fill;
            listContacts.FullRowSelect = true;
            listContacts.HideSelection = false;
            listContacts.Margin = new Padding(0);
            listContacts.MultiSelect = false;
            listContacts.Name = "listContacts";
            listContacts.TabIndex = 2;
            listContacts.View = View.Details;
            // 
            // columns
            // 
            colName.Text = "Name";
            colName.Width = 210;
            colPhone.Text = "Phone";
            colPhone.Width = 140;
            colEmail.Text = "Email";
            colEmail.Width = 240;
            colSource.Text = "Source";
            colSource.Width = 120;
            colType.Text = "Type";
            colType.Width = 100;
            colSms.Text = "SMS consent";
            colSms.Width = 100;
            colEmailConsent.Text = "Email consent";
            colEmailConsent.Width = 110;
            // 
            // searchTimer
            // 
            searchTimer.Interval = 300;
            // 
            // ContactsView
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(card);
            Name = "ContactsView";
            Size = new Size(1100, 560);
            card.ResumeLayout(false);
            layout.ResumeLayout(false);
            layout.PerformLayout();
            toolbar.ResumeLayout(false);
            toolbar.PerformLayout();
            ResumeLayout(false);
        }

        private WayPlot.UI.RoundedPanel card;
        private TableLayoutPanel layout;
        private TableLayoutPanel toolbar;
        private Label lblTitle;
        private TextBox txtSearch;
        private Button btnNew;
        private Label lblSummary;
        private ListView listContacts;
        private ColumnHeader colName;
        private ColumnHeader colPhone;
        private ColumnHeader colEmail;
        private ColumnHeader colSource;
        private ColumnHeader colType;
        private ColumnHeader colSms;
        private ColumnHeader colEmailConsent;
        private System.Windows.Forms.Timer searchTimer;
    }
}
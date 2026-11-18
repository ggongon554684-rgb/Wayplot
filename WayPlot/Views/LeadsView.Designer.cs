#nullable disable
namespace WayPlot.Views
{
    partial class LeadsView
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
            btnOpenLeadFile = new Button();
            lblHelp = new Label();
            filters = new TableLayoutPanel();
            cboStage = new ComboBox();
            txtSearch = new TextBox();
            lblSummary = new Label();
            listLeads = new ListView();
            colContact = new ColumnHeader();
            colParcel = new ColumnHeader();
            colStage = new ColumnHeader();
            colOffer = new ColumnHeader();
            colLastActivity = new ColumnHeader();
            searchTimer = new System.Windows.Forms.Timer(components);
            card.SuspendLayout();
            layout.SuspendLayout();
            header.SuspendLayout();
            filters.SuspendLayout();
            SuspendLayout();
            // 
            // card
            // 
            card.BackColor = Color.FromArgb(245, 247, 251);
            card.BorderColor = Color.FromArgb(227, 232, 242);
            card.Controls.Add(layout);
            card.Dock = DockStyle.Fill;
            card.Location = new Point(0, 0);
            card.Margin = new Padding(4);
            card.Name = "card";
            card.Padding = new Padding(30);
            card.Radius = 24;
            card.Size = new Size(1375, 700);
            card.TabIndex = 0;
            // 
            // layout
            // 
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(header, 0, 0);
            layout.Controls.Add(filters, 0, 1);
            layout.Controls.Add(lblSummary, 0, 2);
            layout.Controls.Add(listLeads, 0, 3);
            layout.Dock = DockStyle.Fill;
            layout.Location = new Point(30, 30);
            layout.Margin = new Padding(4);
            layout.Name = "layout";
            layout.Padding = new Padding(30);
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
            header.Controls.Add(btnOpenLeadFile, 1, 0);
            header.Controls.Add(lblHelp, 0, 1);
            header.Dock = DockStyle.Fill;
            header.Location = new Point(30, 30);
            header.Margin = new Padding(0, 0, 0, 15);
            header.Name = "header";
            header.RowCount = 2;
            header.RowStyles.Add(new RowStyle());
            header.RowStyles.Add(new RowStyle());
            header.Size = new Size(1255, 125);
            header.TabIndex = 0;
            // 
            // lblTitle
            // 
            lblTitle.Anchor = AnchorStyles.Left;
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(0, 9);
            lblTitle.Margin = new Padding(0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(47, 20);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Leads";
            lblTitle.UseMnemonic = false;
            // 
            // btnOpenLeadFile
            // 
            btnOpenLeadFile.AutoSize = true;
            btnOpenLeadFile.Location = new Point(1114, 0);
            btnOpenLeadFile.Margin = new Padding(15, 0, 0, 0);
            btnOpenLeadFile.Name = "btnOpenLeadFile";
            btnOpenLeadFile.Size = new Size(141, 38);
            btnOpenLeadFile.TabIndex = 1;
            btnOpenLeadFile.Text = "Open lead file";
            btnOpenLeadFile.UseVisualStyleBackColor = false;
            // 
            // lblHelp
            // 
            lblHelp.AutoSize = true;
            lblHelp.Location = new Point(0, 43);
            lblHelp.Margin = new Padding(0, 5, 0, 0);
            lblHelp.Name = "lblHelp";
            lblHelp.Size = new Size(343, 20);
            lblHelp.TabIndex = 2;
            lblHelp.Text = "To attach a lead, select a parcel on the Parcel map.";
            lblHelp.UseMnemonic = false;
            // 
            // filters
            // 
            filters.ColumnCount = 2;
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 275F));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            filters.Controls.Add(cboStage, 0, 0);
            filters.Controls.Add(txtSearch, 1, 0);
            filters.Dock = DockStyle.Fill;
            filters.Location = new Point(30, 170);
            filters.Margin = new Padding(0, 0, 0, 5);
            filters.Name = "filters";
            filters.RowCount = 1;
            filters.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            filters.Size = new Size(1255, 125);
            filters.TabIndex = 1;
            // 
            // cboStage
            // 
            cboStage.Dock = DockStyle.Fill;
            cboStage.DropDownStyle = ComboBoxStyle.DropDownList;
            cboStage.FormattingEnabled = true;
            cboStage.Location = new Point(0, 0);
            cboStage.Margin = new Padding(0, 0, 15, 0);
            cboStage.Name = "cboStage";
            cboStage.Size = new Size(260, 28);
            cboStage.TabIndex = 0;
            // 
            // txtSearch
            // 
            txtSearch.Dock = DockStyle.Fill;
            txtSearch.Location = new Point(275, 0);
            txtSearch.Margin = new Padding(0);
            txtSearch.Name = "txtSearch";
            txtSearch.PlaceholderText = "Search contact or parcel";
            txtSearch.Size = new Size(980, 27);
            txtSearch.TabIndex = 1;
            // 
            // lblSummary
            // 
            lblSummary.Anchor = AnchorStyles.Left;
            lblSummary.AutoSize = true;
            lblSummary.Location = new Point(30, 302);
            lblSummary.Margin = new Padding(0, 2, 0, 12);
            lblSummary.Name = "lblSummary";
            lblSummary.Size = new Size(111, 20);
            lblSummary.TabIndex = 2;
            lblSummary.Text = "Loading leads...";
            lblSummary.UseMnemonic = false;
            // 
            // listLeads
            // 
            listLeads.BorderStyle = BorderStyle.None;
            listLeads.Columns.AddRange(new ColumnHeader[] { colContact, colParcel, colStage, colOffer, colLastActivity });
            listLeads.Dock = DockStyle.Fill;
            listLeads.FullRowSelect = true;
            listLeads.Location = new Point(30, 334);
            listLeads.Margin = new Padding(0);
            listLeads.MultiSelect = false;
            listLeads.Name = "listLeads";
            listLeads.Size = new Size(1255, 276);
            listLeads.TabIndex = 3;
            listLeads.UseCompatibleStateImageBehavior = false;
            listLeads.View = View.Details;
            // 
            // colContact
            // 
            colContact.Text = "Contact";
            colContact.Width = 210;
            // 
            // colParcel
            // 
            colParcel.Text = "Parcel";
            colParcel.Width = 200;
            // 
            // colStage
            // 
            colStage.Text = "Stage";
            colStage.Width = 130;
            // 
            // colOffer
            // 
            colOffer.Text = "Offer";
            colOffer.Width = 140;
            // 
            // colLastActivity
            // 
            colLastActivity.Text = "Last activity";
            colLastActivity.Width = 190;
            // 
            // searchTimer
            // 
            searchTimer.Interval = 300;
            // 
            // LeadsView
            // 
            AutoScaleDimensions = new SizeF(120F, 120F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(card);
            Margin = new Padding(4);
            Name = "LeadsView";
            Size = new Size(1375, 700);
            card.ResumeLayout(false);
            layout.ResumeLayout(false);
            layout.PerformLayout();
            header.ResumeLayout(false);
            header.PerformLayout();
            filters.ResumeLayout(false);
            filters.PerformLayout();
            ResumeLayout(false);
        }

        private WayPlot.UI.RoundedPanel card;
        private TableLayoutPanel layout;
        private TableLayoutPanel header;
        private Label lblTitle;
        private Label lblHelp;
        private Button btnOpenLeadFile;
        private TableLayoutPanel filters;
        private ComboBox cboStage;
        private TextBox txtSearch;
        private Label lblSummary;
        private ListView listLeads;
        private ColumnHeader colContact;
        private ColumnHeader colParcel;
        private ColumnHeader colStage;
        private ColumnHeader colOffer;
        private ColumnHeader colLastActivity;
        private System.Windows.Forms.Timer searchTimer;
    }
}

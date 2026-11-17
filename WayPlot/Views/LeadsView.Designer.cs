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
            lblHelp = new Label();
            btnOpenLeadFile = new Button();
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
            card.Controls.Add(layout);
            card.Dock = DockStyle.Fill;
            card.Name = "card";
            card.TabIndex = 0;
            layout.Controls.Add(header, 0, 0);
            layout.Controls.Add(filters, 0, 1);
            layout.Controls.Add(lblSummary, 0, 2);
            layout.Controls.Add(listLeads, 0, 3);
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
            header.Controls.Add(btnOpenLeadFile, 1, 0);
            header.Controls.Add(lblHelp, 0, 1);
            header.ColumnCount = 2;
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.Dock = DockStyle.Fill;
            header.Margin = new Padding(0, 0, 0, 12);
            header.Name = "header";
            header.RowCount = 2;
            header.RowStyles.Add(new RowStyle());
            header.RowStyles.Add(new RowStyle());
            lblTitle.Anchor = AnchorStyles.Left;
            lblTitle.AutoSize = true;
            lblTitle.Margin = new Padding(0);
            lblTitle.Name = "lblTitle";
            lblTitle.Text = "Leads";
            lblTitle.UseMnemonic = false;
            lblHelp.AutoSize = true;
            lblHelp.Margin = new Padding(0, 4, 0, 0);
            lblHelp.Name = "lblHelp";
            lblHelp.Text = "To attach a lead, select a parcel on the Parcel map.";
            lblHelp.UseMnemonic = false;
            btnOpenLeadFile.AutoSize = true;
            btnOpenLeadFile.Margin = new Padding(12, 0, 0, 0);
            btnOpenLeadFile.Name = "btnOpenLeadFile";
            btnOpenLeadFile.Text = "Open lead file";
            btnOpenLeadFile.UseVisualStyleBackColor = false;
            filters.Controls.Add(cboStage, 0, 0);
            filters.Controls.Add(txtSearch, 1, 0);
            filters.ColumnCount = 2;
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            filters.Dock = DockStyle.Fill;
            filters.Margin = new Padding(0, 0, 0, 4);
            filters.Name = "filters";
            filters.RowCount = 1;
            filters.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            cboStage.Dock = DockStyle.Fill;
            cboStage.DropDownStyle = ComboBoxStyle.DropDownList;
            cboStage.FormattingEnabled = true;
            cboStage.Margin = new Padding(0, 0, 12, 0);
            cboStage.Name = "cboStage";
            txtSearch.Dock = DockStyle.Fill;
            txtSearch.Margin = new Padding(0);
            txtSearch.Name = "txtSearch";
            txtSearch.PlaceholderText = "Search contact or parcel";
            lblSummary.Anchor = AnchorStyles.Left;
            lblSummary.AutoSize = true;
            lblSummary.Margin = new Padding(0, 8, 0, 10);
            lblSummary.Name = "lblSummary";
            lblSummary.Text = "Loading leads...";
            lblSummary.UseMnemonic = false;
            listLeads.BorderStyle = BorderStyle.None;
            listLeads.Columns.AddRange(new ColumnHeader[] { colContact, colParcel, colStage, colOffer, colLastActivity });
            listLeads.Dock = DockStyle.Fill;
            listLeads.FullRowSelect = true;
            listLeads.HideSelection = false;
            listLeads.Margin = new Padding(0);
            listLeads.MultiSelect = false;
            listLeads.Name = "listLeads";
            listLeads.UseCompatibleStateImageBehavior = false;
            listLeads.View = View.Details;
            colContact.Text = "Contact";
            colContact.Width = 210;
            colParcel.Text = "Parcel";
            colParcel.Width = 200;
            colStage.Text = "Stage";
            colStage.Width = 130;
            colOffer.Text = "Offer";
            colOffer.Width = 140;
            colLastActivity.Text = "Last activity";
            colLastActivity.Width = 190;
            searchTimer.Interval = 300;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(card);
            Name = "LeadsView";
            Size = new Size(1100, 560);
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

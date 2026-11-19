#nullable disable
using WayPlot.UI;
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
            btnImport = new Button();
            btnNew = new Button();
            lblSummary = new Label();
            filters = new TableLayoutPanel();
            btnKindAll = new Button();
            btnKindCustomers = new Button();
            btnKindClients = new Button();
            btnKindLandowners = new Button();
            cboAgent = new ComboBox();
            splitMain = new SplitContainer();
            listContacts = new ListView();
            colName = new ColumnHeader();
            colKind = new ColumnHeader();
            colPhone = new ColumnHeader();
            colEmail = new ColumnHeader();
            colAgent = new ColumnHeader();
            colOpenLeads = new ColumnHeader();
            colDeals = new ColumnHeader();
            colBalance = new ColumnHeader();
            colParcels = new ColumnHeader();
            detailPanel = new Panel();
            detailTop = new TableLayoutPanel();
            lblDetailName = new Label();
            btnCloseDetail = new Button();
            detailScroll = new Panel();
            detailBody = new TableLayoutPanel();
            searchTimer = new System.Windows.Forms.Timer(components);
            actionToolTip = new ToolTip(components);
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
            layout.Controls.Add(filters, 0, 1);
            layout.Controls.Add(lblSummary, 0, 2);
            layout.Controls.Add(splitMain, 0, 3);
            layout.Dock = DockStyle.Fill;
            layout.Location = new Point(24, 24);
            layout.Name = "layout";
            layout.RowCount = 4;
            layout.RowStyles.Add(new RowStyle());
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
            toolbar.ColumnCount = 4;
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300F));
            toolbar.ColumnStyles.Add(new ColumnStyle());
            toolbar.ColumnStyles.Add(new ColumnStyle());
            toolbar.Controls.Add(lblTitle, 0, 0);
            toolbar.Controls.Add(txtSearch, 1, 0);
            toolbar.Controls.Add(btnImport, 2, 0);
            toolbar.Controls.Add(btnNew, 3, 0);
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
            // btnImport
            //
            btnImport.AutoSize = true;
            btnImport.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnImport.Margin = new Padding(0, 0, 10, 0);
            btnImport.Name = "btnImport";
            btnImport.TabIndex = 2;
            btnImport.Text = "Import CSV";
            btnImport.UseVisualStyleBackColor = false;
            //
            // btnNew
            //
            btnNew.AutoSize = true;
            btnNew.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnNew.Margin = new Padding(0);
            btnNew.Name = "btnNew";
            btnNew.TabIndex = 3;
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
            // filters
            //
            filters.ColumnCount = 5;
            filters.Dock = DockStyle.Fill;
            filters.AutoSize = true;
            filters.Controls.Add(btnKindAll, 0, 0);
            filters.Controls.Add(btnKindCustomers, 1, 0);
            filters.Controls.Add(btnKindClients, 2, 0);
            filters.Controls.Add(btnKindLandowners, 3, 0);
            filters.Controls.Add(cboAgent, 4, 0);
            filters.Name = "filters";
            btnKindAll.Text = "All";
            btnKindCustomers.Text = "Customers";
            btnKindClients.Text = "Clients";
            btnKindLandowners.Text = "Landowners";
            // Without AutoSize the buttons stay 75 px wide and the labels clip ("Cust", "Clien", "Land").
            foreach (var kindButton in new[] { btnKindAll, btnKindCustomers, btnKindClients, btnKindLandowners })
            {
                kindButton.AutoSize = true;
                kindButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                kindButton.Margin = new Padding(0, 0, 8, 0);
            }
            cboAgent.DropDownStyle = ComboBoxStyle.DropDownList;
            cboAgent.MinimumSize = new Size(180, 0);
            cboAgent.Visible = false;
            //
            // splitMain
            //
            splitMain.Dock = DockStyle.Fill;
            splitMain.Orientation = Orientation.Vertical;
            splitMain.Size = new Size(1052, 400);
            splitMain.SplitterWidth = 6;
            splitMain.Panel1.Controls.Add(listContacts);
            splitMain.Panel2.Controls.Add(detailPanel);
            splitMain.Panel2Collapsed = true;
            splitMain.Name = "splitMain";
            //
            // listContacts
            //
            listContacts.BorderStyle = BorderStyle.None;
            listContacts.Columns.AddRange(new ColumnHeader[] {
                colName, colKind, colPhone, colEmail, colAgent, colOpenLeads, colDeals, colBalance, colParcels });
            listContacts.Dock = DockStyle.Fill;
            listContacts.FullRowSelect = true;
            listContacts.HideSelection = false;
            listContacts.Margin = new Padding(0);
            listContacts.MultiSelect = false;
            listContacts.Name = "listContacts";
            listContacts.View = View.Details;
            //
            // detailPanel
            //
            detailPanel.Dock = DockStyle.Fill;
            detailPanel.Controls.Add(detailTop);
            detailPanel.Controls.Add(detailScroll);
            detailPanel.Padding = new Padding(1, 0, 0, 0); // 1-px left border gap
            detailTop.Dock = DockStyle.Top;
            detailTop.Height = 52;
            detailTop.Padding = new Padding(12, 8, 8, 8);
            detailTop.ColumnCount = 2;
            detailTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            detailTop.ColumnStyles.Add(new ColumnStyle());
            detailTop.Controls.Add(lblDetailName, 0, 0);
            detailTop.Controls.Add(btnCloseDetail, 1, 0);
            lblDetailName.AutoSize = true;
            lblDetailName.Font = Theme.Heading;
            lblDetailName.Dock = DockStyle.Fill;
            lblDetailName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            btnCloseDetail.Text = "Close";
            btnCloseDetail.AutoSize = true;
            btnCloseDetail.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            detailScroll.Dock = DockStyle.Fill;
            detailScroll.AutoScroll = true;
            detailScroll.Padding = new Padding(0, 4, 0, 0);
            detailScroll.Controls.Add(detailBody);
            detailBody.Dock = DockStyle.Top;
            detailBody.ColumnCount = 1;
            detailBody.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            detailBody.AutoSize = true;
            detailBody.Padding = new Padding(0, 0, 0, 16);
            //
            // columns
            //
            colName.Text = "Name";
            colName.Width = 210;
            colKind.Text = "Kind";
            colKind.Width = 110;
            colPhone.Text = "Phone";
            colPhone.Width = 140;
            colEmail.Text = "Email";
            colEmail.Width = 240;
            colAgent.Text = "Agent";
            colAgent.Width = 140;
            colOpenLeads.Text = "Open leads";
            colOpenLeads.Width = 90;
            colDeals.Text = "Deals";
            colDeals.Width = 70;
            colBalance.Text = "Balance";
            colBalance.Width = 120;
            colParcels.Text = "Parcels";
            colParcels.Width = 80;
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
        private Button btnImport;
        private Button btnNew;
        private Label lblSummary;
        private TableLayoutPanel filters;
        private Button btnKindAll;
        private Button btnKindCustomers;
        private Button btnKindClients;
        private Button btnKindLandowners;
        private ComboBox cboAgent;
        private SplitContainer splitMain;
        private ListView listContacts;
        private ColumnHeader colName;
        private ColumnHeader colKind;
        private ColumnHeader colPhone;
        private ColumnHeader colEmail;
        private ColumnHeader colAgent;
        private ColumnHeader colOpenLeads;
        private ColumnHeader colDeals;
        private ColumnHeader colBalance;
        private ColumnHeader colParcels;
        private Panel detailPanel;
        private TableLayoutPanel detailTop;
        private Label lblDetailName;
        private Button btnCloseDetail;
        private Panel detailScroll;
        private TableLayoutPanel detailBody;
        private System.Windows.Forms.Timer searchTimer;
        private ToolTip actionToolTip;
    }
}

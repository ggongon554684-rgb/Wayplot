#nullable disable
namespace WayPlot.Views
{
    partial class ParcelMapView
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

        #region Component Designer generated code

        private void InitializeComponent()
        {
            layoutMain = new TableLayoutPanel();
            mapControl = new WayPlot.UI.ParcelMapControl();
            inspector = new WayPlot.UI.RoundedPanel();
            lblPrompt = new Label();
            detailsLayout = new TableLayoutPanel();
            lblParcelLabel = new Label();
            lblParcelStatus = new Label();
            facts = new TableLayoutPanel();
            lblDescription = new Label();
            leadHeaderLayout = new TableLayoutPanel();
            lblLeadsHeader = new Label();
            btnAttachLead = new Button();
            listLeads = new ListView();
            colContact = new ColumnHeader();
            colStage = new ColumnHeader();
            layoutMain.SuspendLayout();
            inspector.SuspendLayout();
            detailsLayout.SuspendLayout();
            SuspendLayout();
            // 
            // layoutMain
            // 
            layoutMain.ColumnCount = 2;
            layoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layoutMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340F));
            layoutMain.Controls.Add(mapControl, 0, 0);
            layoutMain.Controls.Add(inspector, 1, 0);
            layoutMain.Dock = DockStyle.Fill;
            layoutMain.Location = new Point(0, 0);
            layoutMain.Margin = new Padding(0);
            layoutMain.Name = "layoutMain";
            layoutMain.RowCount = 1;
            layoutMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layoutMain.Size = new Size(1104, 504);
            layoutMain.TabIndex = 0;
            // 
            // mapControl
            // 
            mapControl.BackColor = Color.FromArgb(245, 247, 251);
            mapControl.Dock = DockStyle.Fill;
            mapControl.Location = new Point(0, 0);
            mapControl.Margin = new Padding(0, 0, 16, 0);
            mapControl.Name = "mapControl";
            mapControl.Size = new Size(748, 504);
            mapControl.TabIndex = 0;
            // 
            // inspector
            // 
            inspector.BackColor = Color.FromArgb(245, 247, 251);
            inspector.BorderColor = Color.FromArgb(227, 232, 242);
            inspector.Controls.Add(detailsLayout);
            inspector.Controls.Add(lblPrompt);
            inspector.Dock = DockStyle.Fill;
            inspector.Location = new Point(764, 0);
            inspector.Margin = new Padding(0);
            inspector.Name = "inspector";
            inspector.Padding = new Padding(20);
            inspector.Radius = 24;
            inspector.Size = new Size(340, 504);
            inspector.TabIndex = 1;
            // 
            // lblPrompt
            // 
            lblPrompt.Dock = DockStyle.Fill;
            lblPrompt.Location = new Point(20, 20);
            lblPrompt.Name = "lblPrompt";
            lblPrompt.Size = new Size(300, 464);
            lblPrompt.TabIndex = 1;
            lblPrompt.Text = "Select a parcel on the map to see its details.";
            lblPrompt.TextAlign = ContentAlignment.MiddleCenter;
            lblPrompt.UseMnemonic = false;
            // 
            // detailsLayout
            // 
            detailsLayout.BackColor = Color.Transparent;
            detailsLayout.ColumnCount = 1;
            detailsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            detailsLayout.Controls.Add(lblParcelLabel, 0, 0);
            detailsLayout.Controls.Add(lblParcelStatus, 0, 1);
            detailsLayout.Controls.Add(facts, 0, 2);
            detailsLayout.Controls.Add(lblDescription, 0, 3);
            detailsLayout.Controls.Add(leadHeaderLayout, 0, 4);
            detailsLayout.Controls.Add(listLeads, 0, 5);
            leadHeaderLayout.Controls.Add(lblLeadsHeader, 0, 0);
            leadHeaderLayout.Controls.Add(btnAttachLead, 1, 0);
            leadHeaderLayout.ColumnCount = 2;
            leadHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leadHeaderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            leadHeaderLayout.Dock = DockStyle.Fill;
            leadHeaderLayout.Margin = new Padding(0, 4, 0, 6);
            leadHeaderLayout.Name = "leadHeaderLayout";
            leadHeaderLayout.RowCount = 1;
            leadHeaderLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            btnAttachLead.AutoSize = true;
            btnAttachLead.Margin = new Padding(8, 0, 0, 0);
            btnAttachLead.Name = "btnAttachLead";
            btnAttachLead.Text = "Attach lead";
            btnAttachLead.UseVisualStyleBackColor = false;
            detailsLayout.Dock = DockStyle.Fill;
            detailsLayout.Location = new Point(20, 20);
            detailsLayout.Margin = new Padding(0);
            detailsLayout.Name = "detailsLayout";
            detailsLayout.RowCount = 6;
            detailsLayout.RowStyles.Add(new RowStyle());
            detailsLayout.RowStyles.Add(new RowStyle());
            detailsLayout.RowStyles.Add(new RowStyle());
            detailsLayout.RowStyles.Add(new RowStyle());
            detailsLayout.RowStyles.Add(new RowStyle());
            detailsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            detailsLayout.Size = new Size(300, 464);
            detailsLayout.TabIndex = 0;
            // 
            // lblParcelLabel
            // 
            lblParcelLabel.AutoSize = true;
            lblParcelLabel.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblParcelLabel.Location = new Point(0, 0);
            lblParcelLabel.Margin = new Padding(0);
            lblParcelLabel.MaximumSize = new Size(296, 0);
            lblParcelLabel.Name = "lblParcelLabel";
            lblParcelLabel.Size = new Size(70, 32);
            lblParcelLabel.TabIndex = 0;
            lblParcelLabel.Text = "Parcel";
            lblParcelLabel.UseMnemonic = false;
            // 
            // lblParcelStatus
            // 
            lblParcelStatus.AutoSize = true;
            lblParcelStatus.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblParcelStatus.Location = new Point(0, 32);
            lblParcelStatus.Margin = new Padding(0, 2, 0, 0);
            lblParcelStatus.Name = "lblParcelStatus";
            lblParcelStatus.Size = new Size(98, 23);
            lblParcelStatus.TabIndex = 1;
            lblParcelStatus.Text = "● Available";
            lblParcelStatus.UseMnemonic = false;
            // 
            // facts
            // 
            facts.AutoSize = true;
            facts.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            facts.BackColor = Color.Transparent;
            facts.ColumnCount = 2;
            facts.ColumnStyles.Add(new ColumnStyle());
            facts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            facts.Dock = DockStyle.Fill;
            facts.Location = new Point(0, 63);
            facts.Margin = new Padding(0, 12, 0, 8);
            facts.Name = "facts";
            facts.RowCount = 1;
            facts.RowStyles.Add(new RowStyle());
            facts.Size = new Size(300, 0);
            facts.TabIndex = 2;
            // 
            // lblDescription
            // 
            lblDescription.AutoSize = true;
            lblDescription.Location = new Point(0, 83);
            lblDescription.Margin = new Padding(0, 0, 0, 8);
            lblDescription.MaximumSize = new Size(296, 66);
            lblDescription.Name = "lblDescription";
            lblDescription.Size = new Size(97, 23);
            lblDescription.TabIndex = 3;
            lblDescription.Text = "Description";
            lblDescription.UseMnemonic = false;
            // 
            // lblLeadsHeader
            // 
            lblLeadsHeader.AutoSize = true;
            lblLeadsHeader.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblLeadsHeader.Location = new Point(0, 117);
            lblLeadsHeader.Margin = new Padding(0);
            lblLeadsHeader.Name = "lblLeadsHeader";
            lblLeadsHeader.Size = new Size(55, 23);
            lblLeadsHeader.TabIndex = 4;
            lblLeadsHeader.Text = "Leads";
            lblLeadsHeader.UseMnemonic = false;
            // 
            // listLeads
            // 
            listLeads.BackColor = Color.FromArgb(245, 247, 251);
            listLeads.BorderStyle = BorderStyle.None;
            listLeads.Columns.AddRange(new ColumnHeader[] { colContact, colStage });
            listLeads.Dock = DockStyle.Fill;
            listLeads.FullRowSelect = true;
            listLeads.HeaderStyle = ColumnHeaderStyle.None;
            listLeads.Location = new Point(0, 146);
            listLeads.Margin = new Padding(0);
            listLeads.MultiSelect = false;
            listLeads.Name = "listLeads";
            listLeads.Size = new Size(300, 318);
            listLeads.TabIndex = 5;
            listLeads.UseCompatibleStateImageBehavior = false;
            listLeads.View = View.Details;
            // 
            // colContact
            // 
            colContact.Text = "Contact";
            colContact.Width = 190;
            // 
            // colStage
            // 
            colStage.Text = "Stage";
            colStage.Width = 100;
            // 
            // ParcelMapView
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            Controls.Add(layoutMain);
            Margin = new Padding(3, 4, 3, 4);
            Name = "ParcelMapView";
            Size = new Size(1104, 504);
            layoutMain.ResumeLayout(false);
            inspector.ResumeLayout(false);
            detailsLayout.ResumeLayout(false);
            detailsLayout.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel layoutMain;
        private WayPlot.UI.ParcelMapControl mapControl;
        private WayPlot.UI.RoundedPanel inspector;
        private Label lblPrompt;
        private TableLayoutPanel detailsLayout;
        private Label lblParcelLabel;
        private Label lblParcelStatus;
        private TableLayoutPanel facts;
        private Label lblDescription;
        private TableLayoutPanel leadHeaderLayout;
        private Label lblLeadsHeader;
        private Button btnAttachLead;
        private ListView listLeads;
        private ColumnHeader colContact;
        private ColumnHeader colStage;
    }
}
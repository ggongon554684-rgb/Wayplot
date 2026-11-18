#nullable disable
namespace WayPlot.Views
{
    partial class ContactImportDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            layout = new TableLayoutPanel();
            lblTitle = new Label();
            lblHelp = new Label();
            filePanel = new TableLayoutPanel();
            txtFilePath = new TextBox();
            btnBrowse = new Button();
            groupMapping = new GroupBox();
            mappingGrid = new TableLayoutPanel();
            lblName = new Label();
            cboName = new ComboBox();
            lblPhone = new Label();
            cboPhone = new ComboBox();
            lblEmail = new Label();
            cboEmail = new ComboBox();
            lblSource = new Label();
            cboSource = new ComboBox();
            lblConsent = new Label();
            cboConsent = new ComboBox();
            lblParcel = new Label();
            cboParcel = new ComboBox();
            lblAgent = new Label();
            cboAgent = new ComboBox();
            actionsPanel = new FlowLayoutPanel();
            btnPreview = new Button();
            btnConfirm = new Button();
            btnDownloadRejected = new Button();
            btnClose = new Button();
            lblSummary = new Label();
            listRows = new ListView();
            colRow = new ColumnHeader();
            colStatus = new ColumnHeader();
            colName = new ColumnHeader();
            colPhone = new ColumnHeader();
            colEmail = new ColumnHeader();
            colParcel = new ColumnHeader();
            colLead = new ColumnHeader();
            colReason = new ColumnHeader();
            colWarning = new ColumnHeader();
            actionToolTip = new ToolTip(components);
            layout.SuspendLayout();
            filePanel.SuspendLayout();
            groupMapping.SuspendLayout();
            mappingGrid.SuspendLayout();
            actionsPanel.SuspendLayout();
            SuspendLayout();
            // 
            // layout
            // 
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(lblTitle, 0, 0);
            layout.Controls.Add(lblHelp, 0, 1);
            layout.Controls.Add(filePanel, 0, 2);
            layout.Controls.Add(groupMapping, 0, 3);
            layout.Controls.Add(actionsPanel, 0, 4);
            layout.Controls.Add(lblSummary, 0, 5);
            layout.Controls.Add(listRows, 0, 6);
            layout.Dock = DockStyle.Fill;
            layout.Location = new Point(0, 0);
            layout.Margin = new Padding(4, 4, 4, 4);
            layout.Name = "layout";
            layout.Padding = new Padding(30, 30, 30, 30);
            layout.RowCount = 7;
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 145F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55F));
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.Size = new Size(1375, 950);
            layout.TabIndex = 0;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(30, 30);
            lblTitle.Margin = new Padding(0, 0, 0, 8);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(179, 20);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Import contacts from CSV";
            // 
            // lblHelp
            // 
            lblHelp.AutoSize = true;
            lblHelp.Location = new Point(30, 58);
            lblHelp.Margin = new Padding(0, 0, 0, 15);
            lblHelp.Name = "lblHelp";
            lblHelp.Size = new Size(701, 20);
            lblHelp.TabIndex = 1;
            lblHelp.Text = "Review the preview before importing. Consent is enabled only for a clear “yes”; blank source uses Import.";
            // 
            // filePanel
            // 
            filePanel.ColumnCount = 2;
            filePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            filePanel.ColumnStyles.Add(new ColumnStyle());
            filePanel.Controls.Add(txtFilePath, 0, 0);
            filePanel.Controls.Add(btnBrowse, 1, 0);
            filePanel.Dock = DockStyle.Fill;
            filePanel.Location = new Point(30, 93);
            filePanel.Margin = new Padding(0, 0, 0, 10);
            filePanel.Name = "filePanel";
            filePanel.RowCount = 1;
            filePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            filePanel.Size = new Size(1315, 40);
            filePanel.TabIndex = 1;
            // 
            // txtFilePath
            // 
            txtFilePath.Dock = DockStyle.Fill;
            txtFilePath.Location = new Point(0, 0);
            txtFilePath.Margin = new Padding(0, 0, 12, 0);
            txtFilePath.Name = "txtFilePath";
            txtFilePath.ReadOnly = true;
            txtFilePath.Size = new Size(1196, 27);
            txtFilePath.TabIndex = 0;
            // 
            // btnBrowse
            // 
            btnBrowse.AutoSize = true;
            btnBrowse.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnBrowse.Location = new Point(1208, 0);
            btnBrowse.Margin = new Padding(0);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new Size(107, 30);
            btnBrowse.TabIndex = 1;
            btnBrowse.Text = "Choose CSV...";
            btnBrowse.UseVisualStyleBackColor = false;
            // 
            // groupMapping
            // 
            groupMapping.Controls.Add(mappingGrid);
            groupMapping.Dock = DockStyle.Fill;
            groupMapping.Location = new Point(30, 143);
            groupMapping.Margin = new Padding(0, 0, 0, 10);
            groupMapping.Name = "groupMapping";
            groupMapping.Padding = new Padding(12, 12, 12, 12);
            groupMapping.Size = new Size(1315, 135);
            groupMapping.TabIndex = 2;
            groupMapping.TabStop = false;
            groupMapping.Text = "Column mapping";
            // 
            // mappingGrid
            // 
            mappingGrid.ColumnCount = 6;
            mappingGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            mappingGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            mappingGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            mappingGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            mappingGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            mappingGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.334F));
            mappingGrid.Controls.Add(lblName, 0, 0);
            mappingGrid.Controls.Add(cboName, 1, 0);
            mappingGrid.Controls.Add(lblPhone, 2, 0);
            mappingGrid.Controls.Add(cboPhone, 3, 0);
            mappingGrid.Controls.Add(lblEmail, 4, 0);
            mappingGrid.Controls.Add(cboEmail, 5, 0);
            mappingGrid.Controls.Add(lblSource, 0, 1);
            mappingGrid.Controls.Add(cboSource, 1, 1);
            mappingGrid.Controls.Add(lblConsent, 2, 1);
            mappingGrid.Controls.Add(cboConsent, 3, 1);
            mappingGrid.Controls.Add(lblParcel, 4, 1);
            mappingGrid.Controls.Add(cboParcel, 5, 1);
            mappingGrid.Controls.Add(lblAgent, 0, 2);
            mappingGrid.Controls.Add(cboAgent, 1, 2);
            mappingGrid.Dock = DockStyle.Fill;
            mappingGrid.Location = new Point(12, 32);
            mappingGrid.Margin = new Padding(4, 4, 4, 4);
            mappingGrid.Name = "mappingGrid";
            mappingGrid.RowCount = 3;
            mappingGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333F));
            mappingGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333F));
            mappingGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.334F));
            mappingGrid.Size = new Size(1291, 91);
            mappingGrid.TabIndex = 0;
            // 
            // lblName
            // 
            lblName.Anchor = AnchorStyles.Left;
            lblName.AutoSize = true;
            lblName.Location = new Point(4, 5);
            lblName.Margin = new Padding(4, 0, 4, 0);
            lblName.Name = "lblName";
            lblName.Size = new Size(49, 20);
            lblName.TabIndex = 0;
            lblName.Text = "Name";
            // 
            // cboName
            // 
            cboName.Dock = DockStyle.Fill;
            cboName.DropDownStyle = ComboBoxStyle.DropDownList;
            cboName.Location = new Point(94, 4);
            cboName.Margin = new Padding(4, 4, 4, 4);
            cboName.Name = "cboName";
            cboName.Size = new Size(332, 28);
            cboName.TabIndex = 0;
            // 
            // lblPhone
            // 
            lblPhone.Anchor = AnchorStyles.Left;
            lblPhone.AutoSize = true;
            lblPhone.Location = new Point(434, 5);
            lblPhone.Margin = new Padding(4, 0, 4, 0);
            lblPhone.Name = "lblPhone";
            lblPhone.Size = new Size(50, 20);
            lblPhone.TabIndex = 1;
            lblPhone.Text = "Phone";
            // 
            // cboPhone
            // 
            cboPhone.Dock = DockStyle.Fill;
            cboPhone.DropDownStyle = ComboBoxStyle.DropDownList;
            cboPhone.Location = new Point(524, 4);
            cboPhone.Margin = new Padding(4, 4, 4, 4);
            cboPhone.Name = "cboPhone";
            cboPhone.Size = new Size(332, 28);
            cboPhone.TabIndex = 1;
            // 
            // lblEmail
            // 
            lblEmail.Anchor = AnchorStyles.Left;
            lblEmail.AutoSize = true;
            lblEmail.Location = new Point(864, 5);
            lblEmail.Margin = new Padding(4, 0, 4, 0);
            lblEmail.Name = "lblEmail";
            lblEmail.Size = new Size(46, 20);
            lblEmail.TabIndex = 2;
            lblEmail.Text = "Email";
            // 
            // cboEmail
            // 
            cboEmail.Dock = DockStyle.Fill;
            cboEmail.DropDownStyle = ComboBoxStyle.DropDownList;
            cboEmail.Location = new Point(954, 4);
            cboEmail.Margin = new Padding(4, 4, 4, 4);
            cboEmail.Name = "cboEmail";
            cboEmail.Size = new Size(333, 28);
            cboEmail.TabIndex = 2;
            // 
            // lblSource
            // 
            lblSource.Anchor = AnchorStyles.Left;
            lblSource.AutoSize = true;
            lblSource.Location = new Point(4, 35);
            lblSource.Margin = new Padding(4, 0, 4, 0);
            lblSource.Name = "lblSource";
            lblSource.Size = new Size(54, 20);
            lblSource.TabIndex = 3;
            lblSource.Text = "Source";
            // 
            // cboSource
            // 
            cboSource.Dock = DockStyle.Fill;
            cboSource.DropDownStyle = ComboBoxStyle.DropDownList;
            cboSource.Location = new Point(94, 34);
            cboSource.Margin = new Padding(4, 4, 4, 4);
            cboSource.Name = "cboSource";
            cboSource.Size = new Size(332, 28);
            cboSource.TabIndex = 3;
            // 
            // lblConsent
            // 
            lblConsent.Anchor = AnchorStyles.Left;
            lblConsent.AutoSize = true;
            lblConsent.Location = new Point(434, 35);
            lblConsent.Margin = new Padding(4, 0, 4, 0);
            lblConsent.Name = "lblConsent";
            lblConsent.Size = new Size(62, 20);
            lblConsent.TabIndex = 4;
            lblConsent.Text = "Consent";
            // 
            // cboConsent
            // 
            cboConsent.Dock = DockStyle.Fill;
            cboConsent.DropDownStyle = ComboBoxStyle.DropDownList;
            cboConsent.Location = new Point(524, 34);
            cboConsent.Margin = new Padding(4, 4, 4, 4);
            cboConsent.Name = "cboConsent";
            cboConsent.Size = new Size(332, 28);
            cboConsent.TabIndex = 4;
            // 
            // lblParcel
            // 
            lblParcel.Anchor = AnchorStyles.Left;
            lblParcel.AutoSize = true;
            lblParcel.Location = new Point(864, 35);
            lblParcel.Margin = new Padding(4, 0, 4, 0);
            lblParcel.Name = "lblParcel";
            lblParcel.Size = new Size(48, 20);
            lblParcel.TabIndex = 5;
            lblParcel.Text = "Parcel";
            // 
            // cboParcel
            // 
            cboParcel.Dock = DockStyle.Fill;
            cboParcel.DropDownStyle = ComboBoxStyle.DropDownList;
            cboParcel.Location = new Point(954, 34);
            cboParcel.Margin = new Padding(4, 4, 4, 4);
            cboParcel.Name = "cboParcel";
            cboParcel.Size = new Size(333, 28);
            cboParcel.TabIndex = 5;
            // 
            // lblAgent
            // 
            lblAgent.Anchor = AnchorStyles.Left;
            lblAgent.AutoSize = true;
            lblAgent.Location = new Point(4, 60);
            lblAgent.Margin = new Padding(4, 0, 4, 0);
            lblAgent.Name = "lblAgent";
            lblAgent.Size = new Size(73, 31);
            lblAgent.TabIndex = 6;
            lblAgent.Text = "Agent username";
            // 
            // cboAgent
            // 
            cboAgent.Dock = DockStyle.Fill;
            cboAgent.DropDownStyle = ComboBoxStyle.DropDownList;
            cboAgent.Location = new Point(94, 64);
            cboAgent.Margin = new Padding(4, 4, 4, 4);
            cboAgent.Name = "cboAgent";
            cboAgent.Size = new Size(332, 28);
            cboAgent.TabIndex = 6;
            // 
            // actionsPanel
            // 
            actionsPanel.Controls.Add(btnPreview);
            actionsPanel.Controls.Add(btnConfirm);
            actionsPanel.Controls.Add(btnDownloadRejected);
            actionsPanel.Controls.Add(btnClose);
            actionsPanel.Dock = DockStyle.Fill;
            actionsPanel.Location = new Point(30, 288);
            actionsPanel.Margin = new Padding(0);
            actionsPanel.Name = "actionsPanel";
            actionsPanel.Size = new Size(1315, 55);
            actionsPanel.TabIndex = 3;
            actionsPanel.WrapContents = false;
            // 
            // btnPreview
            // 
            btnPreview.AutoSize = true;
            btnPreview.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnPreview.Location = new Point(0, 0);
            btnPreview.Margin = new Padding(0, 0, 10, 0);
            btnPreview.Name = "btnPreview";
            btnPreview.Size = new Size(70, 30);
            btnPreview.TabIndex = 0;
            btnPreview.Text = "Preview";
            btnPreview.UseVisualStyleBackColor = false;
            // 
            // btnConfirm
            // 
            btnConfirm.AutoSize = true;
            btnConfirm.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnConfirm.Location = new Point(80, 0);
            btnConfirm.Margin = new Padding(0, 0, 10, 0);
            btnConfirm.Name = "btnConfirm";
            btnConfirm.Size = new Size(135, 30);
            btnConfirm.TabIndex = 1;
            btnConfirm.Text = "Import valid rows";
            btnConfirm.UseVisualStyleBackColor = false;
            // 
            // btnDownloadRejected
            // 
            btnDownloadRejected.AutoSize = true;
            btnDownloadRejected.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnDownloadRejected.Location = new Point(225, 0);
            btnDownloadRejected.Margin = new Padding(0, 0, 10, 0);
            btnDownloadRejected.Name = "btnDownloadRejected";
            btnDownloadRejected.Size = new Size(146, 30);
            btnDownloadRejected.TabIndex = 2;
            btnDownloadRejected.Text = "Download rejected";
            btnDownloadRejected.UseVisualStyleBackColor = false;
            // 
            // btnClose
            // 
            btnClose.AutoSize = true;
            btnClose.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnClose.Location = new Point(381, 0);
            btnClose.Margin = new Padding(0);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(55, 30);
            btnClose.TabIndex = 3;
            btnClose.Text = "Close";
            btnClose.UseVisualStyleBackColor = false;
            // 
            // lblSummary
            // 
            lblSummary.AutoSize = true;
            lblSummary.Location = new Point(30, 353);
            lblSummary.Margin = new Padding(0, 10, 0, 10);
            lblSummary.Name = "lblSummary";
            lblSummary.Size = new Size(188, 20);
            lblSummary.TabIndex = 4;
            lblSummary.Text = "Choose a CSV file to begin.";
            // 
            // listRows
            // 
            listRows.BorderStyle = BorderStyle.None;
            listRows.Columns.AddRange(new ColumnHeader[] { colRow, colStatus, colName, colPhone, colEmail, colParcel, colLead, colReason, colWarning });
            listRows.Dock = DockStyle.Fill;
            listRows.FullRowSelect = true;
            listRows.Location = new Point(34, 387);
            listRows.Margin = new Padding(4, 4, 4, 4);
            listRows.MultiSelect = false;
            listRows.Name = "listRows";
            listRows.Size = new Size(1307, 529);
            listRows.TabIndex = 4;
            listRows.UseCompatibleStateImageBehavior = false;
            listRows.View = View.Details;
            // 
            // colRow
            // 
            colRow.Text = "Row";
            // 
            // colStatus
            // 
            colStatus.Text = "Status";
            // 
            // colName
            // 
            colName.Text = "Name";
            // 
            // colPhone
            // 
            colPhone.Text = "Phone";
            // 
            // colEmail
            // 
            colEmail.Text = "Email";
            // 
            // colParcel
            // 
            colParcel.Text = "Parcel";
            // 
            // colLead
            // 
            colLead.Text = "Lead";
            // 
            // colReason
            // 
            colReason.Text = "Reason";
            // 
            // colWarning
            // 
            colWarning.Text = "Warning";
            // 
            // ContactImportDialog
            // 
            AutoScaleDimensions = new SizeF(120F, 120F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(1375, 950);
            Controls.Add(layout);
            Margin = new Padding(4, 4, 4, 4);
            MinimizeBox = false;
            Name = "ContactImportDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Import contacts";
            layout.ResumeLayout(false);
            layout.PerformLayout();
            filePanel.ResumeLayout(false);
            filePanel.PerformLayout();
            groupMapping.ResumeLayout(false);
            mappingGrid.ResumeLayout(false);
            mappingGrid.PerformLayout();
            actionsPanel.ResumeLayout(false);
            actionsPanel.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel layout;
        private Label lblTitle;
        private Label lblHelp;
        private TableLayoutPanel filePanel;
        private TextBox txtFilePath;
        private Button btnBrowse;
        private GroupBox groupMapping;
        private TableLayoutPanel mappingGrid;
        private Label lblName;
        private ComboBox cboName;
        private Label lblPhone;
        private ComboBox cboPhone;
        private Label lblEmail;
        private ComboBox cboEmail;
        private Label lblSource;
        private ComboBox cboSource;
        private Label lblConsent;
        private ComboBox cboConsent;
        private Label lblParcel;
        private ComboBox cboParcel;
        private Label lblAgent;
        private ComboBox cboAgent;
        private FlowLayoutPanel actionsPanel;
        private Button btnPreview;
        private Button btnConfirm;
        private Button btnDownloadRejected;
        private Button btnClose;
        private Label lblSummary;
        private ListView listRows;
        private ColumnHeader colRow;
        private ColumnHeader colStatus;
        private ColumnHeader colName;
        private ColumnHeader colPhone;
        private ColumnHeader colEmail;
        private ColumnHeader colParcel;
        private ColumnHeader colLead;
        private ColumnHeader colReason;
        private ColumnHeader colWarning;
        private ToolTip actionToolTip;
    }
}

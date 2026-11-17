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
            layout.Padding = new Padding(24);
            layout.RowCount = 7;
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 116F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.TabIndex = 0;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Margin = new Padding(0, 0, 0, 6);
            lblTitle.Text = "Import contacts from CSV";
            // 
            // lblHelp
            // 
            lblHelp.AutoSize = true;
            lblHelp.Margin = new Padding(0, 0, 0, 12);
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
            filePanel.Margin = new Padding(0, 0, 0, 8);
            filePanel.RowCount = 1;
            filePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            filePanel.TabIndex = 1;
            // 
            // txtFilePath
            // 
            txtFilePath.Dock = DockStyle.Fill;
            txtFilePath.Margin = new Padding(0, 0, 10, 0);
            txtFilePath.ReadOnly = true;
            txtFilePath.TabIndex = 0;
            // 
            // btnBrowse
            // 
            btnBrowse.AutoSize = true;
            btnBrowse.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnBrowse.Margin = new Padding(0);
            btnBrowse.TabIndex = 1;
            btnBrowse.Text = "Choose CSV...";
            btnBrowse.UseVisualStyleBackColor = false;
            // 
            // groupMapping
            // 
            groupMapping.Controls.Add(mappingGrid);
            groupMapping.Dock = DockStyle.Fill;
            groupMapping.Margin = new Padding(0, 0, 0, 8);
            groupMapping.Padding = new Padding(10);
            groupMapping.Text = "Column mapping";
            groupMapping.TabIndex = 2;
            groupMapping.TabStop = false;
            // 
            // mappingGrid
            // 
            mappingGrid.ColumnCount = 6;
            mappingGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72F));
            mappingGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            mappingGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72F));
            mappingGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333F));
            mappingGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72F));
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
            mappingGrid.RowCount = 3;
            mappingGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333F));
            mappingGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333F));
            mappingGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.334F));
            mappingGrid.TabIndex = 0;
            // 
            // labels and mapping controls
            // 
            lblName.Anchor = AnchorStyles.Left;
            lblName.AutoSize = true;
            lblName.Text = "Name";
            cboName.Dock = DockStyle.Fill;
            cboName.DropDownStyle = ComboBoxStyle.DropDownList;
            cboName.TabIndex = 0;
            lblPhone.Anchor = AnchorStyles.Left;
            lblPhone.AutoSize = true;
            lblPhone.Text = "Phone";
            cboPhone.Dock = DockStyle.Fill;
            cboPhone.DropDownStyle = ComboBoxStyle.DropDownList;
            cboPhone.TabIndex = 1;
            lblEmail.Anchor = AnchorStyles.Left;
            lblEmail.AutoSize = true;
            lblEmail.Text = "Email";
            cboEmail.Dock = DockStyle.Fill;
            cboEmail.DropDownStyle = ComboBoxStyle.DropDownList;
            cboEmail.TabIndex = 2;
            lblSource.Anchor = AnchorStyles.Left;
            lblSource.AutoSize = true;
            lblSource.Text = "Source";
            cboSource.Dock = DockStyle.Fill;
            cboSource.DropDownStyle = ComboBoxStyle.DropDownList;
            cboSource.TabIndex = 3;
            lblConsent.Anchor = AnchorStyles.Left;
            lblConsent.AutoSize = true;
            lblConsent.Text = "Consent";
            cboConsent.Dock = DockStyle.Fill;
            cboConsent.DropDownStyle = ComboBoxStyle.DropDownList;
            cboConsent.TabIndex = 4;
            lblParcel.Anchor = AnchorStyles.Left;
            lblParcel.AutoSize = true;
            lblParcel.Text = "Parcel";
            cboParcel.Dock = DockStyle.Fill;
            cboParcel.DropDownStyle = ComboBoxStyle.DropDownList;
            cboParcel.TabIndex = 5;
            lblAgent.Anchor = AnchorStyles.Left;
            lblAgent.AutoSize = true;
            lblAgent.Text = "Agent username";
            cboAgent.Dock = DockStyle.Fill;
            cboAgent.DropDownStyle = ComboBoxStyle.DropDownList;
            cboAgent.TabIndex = 6;
            // 
            // actionsPanel
            // 
            actionsPanel.Controls.Add(btnPreview);
            actionsPanel.Controls.Add(btnConfirm);
            actionsPanel.Controls.Add(btnDownloadRejected);
            actionsPanel.Controls.Add(btnClose);
            actionsPanel.Dock = DockStyle.Fill;
            actionsPanel.Margin = new Padding(0);
            actionsPanel.WrapContents = false;
            actionsPanel.TabIndex = 3;
            // 
            // btnPreview
            // 
            btnPreview.AutoSize = true;
            btnPreview.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnPreview.Margin = new Padding(0, 0, 8, 0);
            btnPreview.Text = "Preview";
            btnPreview.UseVisualStyleBackColor = false;
            // 
            // btnConfirm
            // 
            btnConfirm.AutoSize = true;
            btnConfirm.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnConfirm.Margin = new Padding(0, 0, 8, 0);
            btnConfirm.Text = "Import valid rows";
            btnConfirm.UseVisualStyleBackColor = false;
            // 
            // btnDownloadRejected
            // 
            btnDownloadRejected.AutoSize = true;
            btnDownloadRejected.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnDownloadRejected.Margin = new Padding(0, 0, 8, 0);
            btnDownloadRejected.Text = "Download rejected";
            btnDownloadRejected.UseVisualStyleBackColor = false;
            // 
            // btnClose
            // 
            btnClose.AutoSize = true;
            btnClose.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnClose.Margin = new Padding(0);
            btnClose.Text = "Close";
            btnClose.UseVisualStyleBackColor = false;
            // 
            // lblSummary
            // 
            lblSummary.AutoSize = true;
            lblSummary.Margin = new Padding(0, 8, 0, 8);
            lblSummary.Text = "Choose a CSV file to begin.";
            // 
            // listRows
            // 
            listRows.BorderStyle = BorderStyle.None;
            listRows.Columns.AddRange(new ColumnHeader[] { colRow, colStatus, colName, colPhone, colEmail, colParcel, colLead, colReason, colWarning });
            listRows.Dock = DockStyle.Fill;
            listRows.FullRowSelect = true;
            listRows.HideSelection = false;
            listRows.MultiSelect = false;
            listRows.TabIndex = 4;
            listRows.View = View.Details;
            // 
            // columns
            // 
            colRow.Text = "Row";
            colStatus.Text = "Status";
            colName.Text = "Name";
            colPhone.Text = "Phone";
            colEmail.Text = "Email";
            colParcel.Text = "Parcel";
            colLead.Text = "Lead";
            colReason.Text = "Reason";
            colWarning.Text = "Warning";
            // 
            // ContactImportDialog
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(1100, 760);
            Controls.Add(layout);
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

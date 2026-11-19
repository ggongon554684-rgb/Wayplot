#nullable disable
namespace WayPlot.Views
{
    partial class AttachLeadDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                // _searchTimer is not registered in components (no container passed),
                // so dispose it explicitly to avoid a timer leak.
                _searchTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            layout = new TableLayoutPanel();
            lblTitle = new Label();
            searchLayout = new TableLayoutPanel();
            txtSearch = new TextBox();
            btnSearch = new Button();
            listContacts = new ListView();
            colName = new ColumnHeader();
            colPhone = new ColumnHeader();
            colEmail = new ColumnHeader();
            statusLayout = new TableLayoutPanel();
            lblStatus = new Label();
            buttons = new FlowLayoutPanel();
            btnAttach = new Button();
            btnCancel = new Button();
            layout.SuspendLayout();
            searchLayout.SuspendLayout();
            statusLayout.SuspendLayout();
            buttons.SuspendLayout();
            SuspendLayout();
            // 
            // layout
            // 
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(lblTitle, 0, 0);
            layout.Controls.Add(searchLayout, 0, 1);
            layout.Controls.Add(listContacts, 0, 2);
            layout.Controls.Add(statusLayout, 0, 3);
            layout.Dock = DockStyle.Fill;
            layout.Location = new Point(0, 0);
            layout.Margin = new Padding(4);
            layout.Name = "layout";
            layout.Padding = new Padding(30);
            layout.RowCount = 4;
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle());
            layout.Size = new Size(900, 650);
            layout.TabIndex = 0;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(30, 30);
            lblTitle.Margin = new Padding(0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(207, 20);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Attach a contact to this parcel";
            lblTitle.UseMnemonic = false;
            // 
            // searchLayout
            // 
            searchLayout.ColumnCount = 2;
            searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            searchLayout.ColumnStyles.Add(new ColumnStyle());
            searchLayout.Controls.Add(txtSearch, 0, 0);
            searchLayout.Controls.Add(btnSearch, 1, 0);
            searchLayout.Dock = DockStyle.Fill;
            searchLayout.Location = new Point(30, 60);
            searchLayout.Margin = new Padding(0, 10, 0, 15);
            searchLayout.Name = "searchLayout";
            searchLayout.RowCount = 1;
            searchLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            searchLayout.Size = new Size(840, 125);
            searchLayout.TabIndex = 1;
            // 
            // txtSearch
            // 
            txtSearch.Dock = DockStyle.Fill;
            txtSearch.Location = new Point(0, 0);
            txtSearch.Margin = new Padding(0);
            txtSearch.Name = "txtSearch";
            txtSearch.PlaceholderText = "Search name, phone or email";
            txtSearch.Size = new Size(736, 27);
            txtSearch.TabIndex = 0;
            txtSearch.TextChanged += txtSearch_TextChanged;
            // 
            // btnSearch
            // 
            btnSearch.AutoSize = true;
            btnSearch.Location = new Point(746, 0);
            btnSearch.Margin = new Padding(10, 0, 0, 0);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(94, 38);
            btnSearch.TabIndex = 1;
            btnSearch.Text = "Search";
            btnSearch.UseVisualStyleBackColor = false;
            // 
            // listContacts
            // 
            listContacts.BorderStyle = BorderStyle.FixedSingle;
            listContacts.Columns.AddRange(new ColumnHeader[] { colName, colPhone, colEmail });
            listContacts.Dock = DockStyle.Fill;
            listContacts.FullRowSelect = true;
            listContacts.Location = new Point(34, 204);
            listContacts.Margin = new Padding(4);
            listContacts.MultiSelect = false;
            listContacts.Name = "listContacts";
            listContacts.Size = new Size(832, 275);
            listContacts.TabIndex = 2;
            listContacts.UseCompatibleStateImageBehavior = false;
            listContacts.View = View.Details;
            // 
            // colName
            // 
            colName.Text = "Contact";
            colName.Width = 220;
            // 
            // colPhone
            // 
            colPhone.Text = "Phone";
            colPhone.Width = 150;
            // 
            // colEmail
            // 
            colEmail.Text = "Email";
            colEmail.Width = 220;
            // 
            // statusLayout
            // 
            statusLayout.ColumnCount = 2;
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            statusLayout.ColumnStyles.Add(new ColumnStyle());
            statusLayout.Controls.Add(lblStatus, 0, 0);
            statusLayout.Controls.Add(buttons, 1, 0);
            statusLayout.Dock = DockStyle.Fill;
            statusLayout.Location = new Point(30, 495);
            statusLayout.Margin = new Padding(0, 12, 0, 0);
            statusLayout.Name = "statusLayout";
            statusLayout.RowCount = 1;
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            statusLayout.Size = new Size(840, 125);
            statusLayout.TabIndex = 3;
            // 
            // lblStatus
            // 
            lblStatus.Dock = DockStyle.Fill;
            lblStatus.Location = new Point(0, 0);
            lblStatus.Margin = new Padding(0, 0, 15, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(602, 125);
            lblStatus.TabIndex = 0;
            lblStatus.Text = "Loading contacts...";
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.UseMnemonic = false;
            // 
            // buttons
            // 
            buttons.AutoSize = true;
            buttons.Controls.Add(btnAttach);
            buttons.Controls.Add(btnCancel);
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Location = new Point(617, 0);
            buttons.Margin = new Padding(0);
            buttons.Name = "buttons";
            buttons.Size = new Size(223, 125);
            buttons.TabIndex = 1;
            buttons.WrapContents = false;
            // 
            // btnAttach
            // 
            btnAttach.AutoSize = true;
            btnAttach.Location = new Point(104, 0);
            btnAttach.Margin = new Padding(10, 0, 0, 0);
            btnAttach.Name = "btnAttach";
            btnAttach.Size = new Size(119, 38);
            btnAttach.TabIndex = 0;
            btnAttach.Text = "Attach lead";
            btnAttach.UseVisualStyleBackColor = false;
            // 
            // btnCancel
            // 
            btnCancel.AutoSize = true;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(0, 0);
            btnCancel.Margin = new Padding(0);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(94, 38);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = false;
            // 
            // AttachLeadDialog
            // 
            AcceptButton = btnAttach;
            AutoScaleDimensions = new SizeF(120F, 120F);
            AutoScaleMode = AutoScaleMode.Dpi;
            CancelButton = btnCancel;
            ClientSize = new Size(900, 650);
            Controls.Add(layout);
            Margin = new Padding(4);
            MinimizeBox = false;
            Name = "AttachLeadDialog";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Attach lead";
            layout.ResumeLayout(false);
            layout.PerformLayout();
            searchLayout.ResumeLayout(false);
            searchLayout.PerformLayout();
            statusLayout.ResumeLayout(false);
            statusLayout.PerformLayout();
            buttons.ResumeLayout(false);
            buttons.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel layout;
        private Label lblTitle;
        private TableLayoutPanel searchLayout;
        private TextBox txtSearch;
        private Button btnSearch;
        private ListView listContacts;
        private ColumnHeader colName;
        private ColumnHeader colPhone;
        private ColumnHeader colEmail;
        private TableLayoutPanel statusLayout;
        private Label lblStatus;
        private FlowLayoutPanel buttons;
        private Button btnAttach;
        private Button btnCancel;
    }
}

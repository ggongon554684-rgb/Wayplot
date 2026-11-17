#nullable disable
namespace WayPlot.Views
{
    partial class AttachLeadDialog
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
            searchLayout.Controls.Add(txtSearch, 0, 0);
            searchLayout.Controls.Add(btnSearch, 1, 0);
            searchLayout.ColumnCount = 2;
            searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            searchLayout.Dock = DockStyle.Fill;
            searchLayout.Margin = new Padding(0, 8, 0, 12);
            searchLayout.Name = "searchLayout";
            searchLayout.RowCount = 1;
            searchLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            txtSearch.Dock = DockStyle.Fill;
            txtSearch.Margin = new Padding(0);
            txtSearch.Name = "txtSearch";
            txtSearch.PlaceholderText = "Search name, phone or email";
            txtSearch.TabIndex = 0;
            btnSearch.AutoSize = true;
            btnSearch.Margin = new Padding(8, 0, 0, 0);
            btnSearch.Name = "btnSearch";
            btnSearch.Text = "Search";
            btnSearch.TabIndex = 1;
            btnSearch.UseVisualStyleBackColor = false;
            listContacts.BorderStyle = BorderStyle.FixedSingle;
            listContacts.Columns.AddRange(new ColumnHeader[] { colName, colPhone, colEmail });
            listContacts.Dock = DockStyle.Fill;
            listContacts.FullRowSelect = true;
            listContacts.HideSelection = false;
            listContacts.MultiSelect = false;
            listContacts.Name = "listContacts";
            listContacts.TabIndex = 2;
            listContacts.UseCompatibleStateImageBehavior = false;
            listContacts.View = View.Details;
            colName.Text = "Contact";
            colName.Width = 220;
            colPhone.Text = "Phone";
            colPhone.Width = 150;
            colEmail.Text = "Email";
            colEmail.Width = 220;
            statusLayout.Controls.Add(lblStatus, 0, 0);
            statusLayout.Controls.Add(buttons, 1, 0);
            statusLayout.ColumnCount = 2;
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            statusLayout.Dock = DockStyle.Fill;
            statusLayout.Margin = new Padding(0, 10, 0, 0);
            statusLayout.Name = "statusLayout";
            statusLayout.RowCount = 1;
            statusLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            lblStatus.Dock = DockStyle.Fill;
            lblStatus.Margin = new Padding(0, 0, 12, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Text = "Loading contacts...";
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.UseMnemonic = false;
            buttons.AutoSize = true;
            buttons.Controls.Add(btnAttach);
            buttons.Controls.Add(btnCancel);
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Margin = new Padding(0);
            buttons.Name = "buttons";
            buttons.WrapContents = false;
            btnAttach.AutoSize = true;
            btnAttach.Margin = new Padding(8, 0, 0, 0);
            btnAttach.Name = "btnAttach";
            btnAttach.Text = "Attach lead";
            btnAttach.TabIndex = 0;
            btnAttach.UseVisualStyleBackColor = false;
            btnCancel.AutoSize = true;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Margin = new Padding(0);
            btnCancel.Name = "btnCancel";
            btnCancel.Text = "Cancel";
            btnCancel.TabIndex = 1;
            btnCancel.UseVisualStyleBackColor = false;
            layout.Controls.Add(lblTitle, 0, 0);
            layout.Controls.Add(searchLayout, 0, 1);
            layout.Controls.Add(listContacts, 0, 2);
            layout.Controls.Add(statusLayout, 0, 3);
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Dock = DockStyle.Fill;
            layout.Name = "layout";
            layout.Padding = new Padding(24);
            layout.RowCount = 4;
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle());
            layout.TabIndex = 0;
            lblTitle.AutoSize = true;
            lblTitle.Margin = new Padding(0);
            lblTitle.Name = "lblTitle";
            lblTitle.Text = "Attach a contact to this parcel";
            lblTitle.UseMnemonic = false;
            AcceptButton = btnAttach;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            CancelButton = btnCancel;
            ClientSize = new Size(720, 520);
            Controls.Add(layout);
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            Name = "AttachLeadDialog";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Attach lead";
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

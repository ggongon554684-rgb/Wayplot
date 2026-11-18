#nullable disable
namespace WayPlot.Views
{
    partial class TeamAdminView
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing) components?.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            root = new TableLayoutPanel();
            header = new TableLayoutPanel();
            lblTitle = new Label();
            headerButtons = new FlowLayoutPanel();
            btnInvite = new Button();
            btnChangeRole = new Button();
            btnCompanyProfile = new Button();
            lblStatus = new Label();
            listStaff = new ListView();
            colName = new ColumnHeader();
            colUsername = new ColumnHeader();
            colRole = new ColumnHeader();
            colState = new ColumnHeader();
            colLeads = new ColumnHeader();
            colParcels = new ColumnHeader();
            colBranch = new ColumnHeader();
            staffActions = new FlowLayoutPanel();
            btnDeactivate = new Button();
            btnReactivate = new Button();
            lblBranch = new Label();
            cboBranch = new ComboBox();
            btnChangeBranch = new Button();
            reassignment = new GroupBox();
            reassignmentLayout = new TableLayoutPanel();
            lblRecordType = new Label();
            cboRecordType = new ComboBox();
            lblRecord = new Label();
            cboRecord = new ComboBox();
            lblTarget = new Label();
            cboTarget = new ComboBox();
            chkIncludeOpenLeads = new CheckBox();
            btnReassign = new Button();
            lblSource = new Label();
            cboSource = new ComboBox();
            lblDestination = new Label();
            cboDestination = new ComboBox();
            btnReassignAll = new Button();
            toolTip = new ToolTip(components);
            root.SuspendLayout();
            header.SuspendLayout();
            headerButtons.SuspendLayout();
            staffActions.SuspendLayout();
            reassignment.SuspendLayout();
            reassignmentLayout.SuspendLayout();
            SuspendLayout();
            root.ColumnCount = 1;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.Controls.Add(header, 0, 0);
            root.Controls.Add(lblStatus, 0, 1);
            root.Controls.Add(listStaff, 0, 2);
            root.Controls.Add(staffActions, 0, 3);
            root.Controls.Add(reassignment, 0, 4);
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(18);
            root.RowCount = 5;
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 58F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));
            header.ColumnCount = 2;
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            header.Controls.Add(lblTitle, 0, 0);
            header.Controls.Add(headerButtons, 1, 0);
            header.Dock = DockStyle.Fill;
            header.Margin = new Padding(0, 0, 0, 8);
            lblTitle.AutoSize = true;
            lblTitle.Text = "Team";
            lblTitle.UseMnemonic = false;
            headerButtons.AutoSize = true;
            headerButtons.Controls.Add(btnInvite);
            headerButtons.Controls.Add(btnChangeRole);
            headerButtons.Controls.Add(btnCompanyProfile);
            headerButtons.FlowDirection = FlowDirection.RightToLeft;
            headerButtons.WrapContents = false;
            foreach (var button in new[] { btnInvite, btnChangeRole, btnCompanyProfile })
            {
                button.AutoSize = true;
                button.Margin = new Padding(6, 0, 0, 0);
                button.UseVisualStyleBackColor = false;
            }
            btnInvite.Text = "Invite";
            btnChangeRole.Text = "Change role";
            btnCompanyProfile.Text = "Company profile";
            lblStatus.AutoSize = true;
            lblStatus.Margin = new Padding(0, 0, 0, 8);
            lblStatus.Text = "Loading staff...";
            lblStatus.UseMnemonic = false;
            listStaff.BorderStyle = BorderStyle.None;
            listStaff.Columns.AddRange(new ColumnHeader[] { colName, colUsername, colRole, colState, colLeads, colParcels, colBranch });
            listStaff.Dock = DockStyle.Fill;
            listStaff.FullRowSelect = true;
            listStaff.HideSelection = false;
            listStaff.MultiSelect = false;
            listStaff.View = View.Details;
            colName.Text = "Name";
            colName.Width = 180;
            colUsername.Text = "Username";
            colUsername.Width = 150;
            colRole.Text = "Role (read-only)";
            colRole.Width = 130;
            colState.Text = "Account";
            colState.Width = 90;
            colLeads.Text = "Open leads";
            colLeads.Width = 90;
            colParcels.Text = "Available parcels";
            colParcels.Width = 120;
            colBranch.Text = "Branch";
            colBranch.Width = 150;
            staffActions.AutoSize = true;
            staffActions.Controls.Add(btnDeactivate);
            staffActions.Controls.Add(btnReactivate);
            staffActions.Controls.Add(lblBranch);
            staffActions.Controls.Add(cboBranch);
            staffActions.Controls.Add(btnChangeBranch);
            staffActions.Dock = DockStyle.Fill;
            staffActions.Margin = new Padding(0, 8, 0, 10);
            foreach (var button in new[] { btnDeactivate, btnReactivate, btnChangeBranch })
            {
                button.AutoSize = true;
                button.UseVisualStyleBackColor = false;
            }
            btnDeactivate.Text = "Deactivate";
            btnReactivate.Text = "Reactivate";
            lblBranch.Anchor = AnchorStyles.Left;
            lblBranch.AutoSize = true;
            lblBranch.Margin = new Padding(18, 0, 4, 0);
            lblBranch.Text = "Branch";
            cboBranch.DropDownStyle = ComboBoxStyle.DropDownList;
            cboBranch.Width = 180;
            btnChangeBranch.Text = "Change branch";
            reassignment.Dock = DockStyle.Fill;
            reassignment.Text = "Reassign records";
            reassignment.Padding = new Padding(10);
            reassignmentLayout.ColumnCount = 4;
            reassignmentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            reassignmentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            reassignmentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            reassignmentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            reassignmentLayout.RowCount = 5;
            for (var i = 0; i < 5; i++) reassignmentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            reassignmentLayout.Controls.Add(lblRecordType, 0, 0);
            reassignmentLayout.Controls.Add(cboRecordType, 1, 0);
            reassignmentLayout.Controls.Add(lblRecord, 2, 0);
            reassignmentLayout.Controls.Add(cboRecord, 3, 0);
            reassignmentLayout.Controls.Add(lblTarget, 0, 1);
            reassignmentLayout.Controls.Add(cboTarget, 1, 1);
            reassignmentLayout.Controls.Add(chkIncludeOpenLeads, 2, 1);
            reassignmentLayout.SetColumnSpan(chkIncludeOpenLeads, 2);
            reassignmentLayout.Controls.Add(btnReassign, 3, 2);
            reassignmentLayout.Controls.Add(lblSource, 0, 3);
            reassignmentLayout.Controls.Add(cboSource, 1, 3);
            reassignmentLayout.Controls.Add(lblDestination, 2, 3);
            reassignmentLayout.Controls.Add(cboDestination, 3, 3);
            reassignmentLayout.Controls.Add(btnReassignAll, 3, 4);
            reassignmentLayout.Dock = DockStyle.Fill;
            reassignmentLayout.Padding = new Padding(4);
            foreach (var label in new[] { lblRecordType, lblRecord, lblTarget, lblSource, lblDestination })
            {
                label.Anchor = AnchorStyles.Left;
                label.AutoSize = true;
                label.Margin = new Padding(0, 4, 8, 8);
                label.UseMnemonic = false;
            }
            lblRecordType.Text = "Record type";
            lblRecord.Text = "Lead/contact/parcel";
            lblTarget.Text = "Assign to Agent";
            lblSource.Text = "Reassign all from";
            lblDestination.Text = "To Agent";
            foreach (var combo in new[] { cboRecordType, cboRecord, cboTarget, cboSource, cboDestination })
            {
                combo.Dock = DockStyle.Fill;
                combo.DropDownStyle = ComboBoxStyle.DropDownList;
                combo.Margin = new Padding(0, 0, 12, 8);
            }
            chkIncludeOpenLeads.AutoSize = true;
            chkIncludeOpenLeads.Margin = new Padding(4, 4, 0, 8);
            chkIncludeOpenLeads.UseMnemonic = false;
            foreach (var button in new[] { btnReassign, btnReassignAll })
            {
                button.AutoSize = true;
                button.Anchor = AnchorStyles.Right;
                button.UseVisualStyleBackColor = false;
            }
            btnReassign.Text = "Reassign selected record";
            btnReassignAll.Text = "Reassign all from Agent";
            reassignment.Controls.Add(reassignmentLayout);
            Controls.Add(root);
            Name = "TeamAdminView";
            Size = new Size(1100, 700);
            root.ResumeLayout(false);
            root.PerformLayout();
            header.ResumeLayout(false);
            header.PerformLayout();
            headerButtons.ResumeLayout(false);
            headerButtons.PerformLayout();
            staffActions.ResumeLayout(false);
            staffActions.PerformLayout();
            reassignment.ResumeLayout(false);
            reassignment.PerformLayout();
            reassignmentLayout.ResumeLayout(false);
            reassignmentLayout.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel root;
        private TableLayoutPanel header;
        private Label lblTitle;
        private FlowLayoutPanel headerButtons;
        private Button btnInvite;
        private Button btnChangeRole;
        private Button btnCompanyProfile;
        private Label lblStatus;
        private ListView listStaff;
        private ColumnHeader colName;
        private ColumnHeader colUsername;
        private ColumnHeader colRole;
        private ColumnHeader colState;
        private ColumnHeader colLeads;
        private ColumnHeader colParcels;
        private ColumnHeader colBranch;
        private FlowLayoutPanel staffActions;
        private Button btnDeactivate;
        private Button btnReactivate;
        private Label lblBranch;
        private ComboBox cboBranch;
        private Button btnChangeBranch;
        private GroupBox reassignment;
        private TableLayoutPanel reassignmentLayout;
        private Label lblRecordType;
        private ComboBox cboRecordType;
        private Label lblRecord;
        private ComboBox cboRecord;
        private Label lblTarget;
        private ComboBox cboTarget;
        private CheckBox chkIncludeOpenLeads;
        private Button btnReassign;
        private Label lblSource;
        private ComboBox cboSource;
        private Label lblDestination;
        private ComboBox cboDestination;
        private Button btnReassignAll;
        private ToolTip toolTip;
    }
}

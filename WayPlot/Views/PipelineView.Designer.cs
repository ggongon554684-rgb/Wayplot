#nullable disable
namespace WayPlot.Views
{
    partial class PipelineView
    {
        private System.ComponentModel.IContainer components = null;
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            layout = new TableLayoutPanel();
            filters = new TableLayoutPanel();
            txtSearch = new TextBox();
            cboParcel = new ComboBox();
            cboAgent = new ComboBox();
            chkOlder = new CheckBox();
            btnConfigureStages = new Button();
            lblStatus = new Label();
            splitDetail = new SplitContainer();
            boardPanel = new FlowLayoutPanel();
            detailPanel = new Panel();
            detailLayout = new TableLayoutPanel();
            lblContact = new Label();
            lblParcel = new Label();
            lblStage = new Label();
            lblAgent = new Label();
            timelineList = new FlowLayoutPanel();
            detailButtons = new FlowLayoutPanel();
            btnCloseDetail = new Button();
            btnMove = new Button();
            btnLogCall = new Button();
            btnLogText = new Button();
            btnLogEmail = new Button();
            btnLogViewing = new Button();
            btnAddNote = new Button();
            btnRequestClosed = new Button();
            btnSwitchLot = new Button();
            btnOpenContact = new Button();
            btnShowMap = new Button();
            filterTimer = new System.Windows.Forms.Timer(components);
            layout.SuspendLayout();
            filters.SuspendLayout();
            splitDetail.Panel1.SuspendLayout();
            splitDetail.Panel2.SuspendLayout();
            detailPanel.SuspendLayout();
            detailLayout.SuspendLayout();
            detailButtons.SuspendLayout();
            SuspendLayout();

            layout.ColumnCount = 1;
            layout.Dock = DockStyle.Fill;
            layout.Controls.Add(filters, 0, 0);
            layout.Controls.Add(lblStatus, 0, 1);
            layout.Controls.Add(splitDetail, 0, 2);
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            filters.ColumnCount = 5;
            filters.Dock = DockStyle.Fill;
            filters.AutoSize = true;
            filters.Controls.Add(txtSearch, 0, 0);
            filters.Controls.Add(cboParcel, 1, 0);
            filters.Controls.Add(cboAgent, 2, 0);
            filters.Controls.Add(chkOlder, 3, 0);
            filters.Controls.Add(btnConfigureStages, 4, 0);
            txtSearch.MinimumSize = new Size(220, 30);
            cboParcel.MinimumSize = new Size(180, 30);
            cboAgent.MinimumSize = new Size(180, 30);
            cboParcel.DropDownStyle = ComboBoxStyle.DropDownList;
            cboAgent.DropDownStyle = ComboBoxStyle.DropDownList;
            cboAgent.Visible = false;
            chkOlder.Text = "Include older Closed and Lost";
            chkOlder.AutoSize = true;
            btnConfigureStages.AutoSize = true;
            btnConfigureStages.Name = "btnConfigureStages";
            btnConfigureStages.TabIndex = 5;
            btnConfigureStages.Text = "Configure stages";
            btnConfigureStages.UseVisualStyleBackColor = true;

            lblStatus.AutoSize = true;
            lblStatus.Margin = new Padding(0, 6, 0, 6);

            splitDetail.Dock = DockStyle.Fill;
            splitDetail.Orientation = Orientation.Vertical;
            splitDetail.Size = new Size(1100, 500);
            splitDetail.SplitterWidth = 6;
            splitDetail.Panel1.Controls.Add(boardPanel);
            splitDetail.Panel2.Controls.Add(detailPanel);
            splitDetail.Panel2Collapsed = true;
            boardPanel.Dock = DockStyle.Fill;
            boardPanel.FlowDirection = FlowDirection.LeftToRight;
            boardPanel.WrapContents = false;
            boardPanel.AutoScroll = true;
            boardPanel.AllowDrop = true;

            detailPanel.Dock = DockStyle.Fill;
            detailPanel.Controls.Add(detailLayout);
            detailLayout.ColumnCount = 1;
            detailLayout.Dock = DockStyle.Fill;
            detailLayout.AutoScroll = true;
            detailLayout.Controls.Add(lblContact, 0, 0);
            detailLayout.Controls.Add(lblParcel, 0, 1);
            detailLayout.Controls.Add(lblStage, 0, 2);
            detailLayout.Controls.Add(lblAgent, 0, 3);
            detailLayout.Controls.Add(detailButtons, 0, 4);
            detailLayout.Controls.Add(timelineList, 0, 5);
            detailLayout.Controls.Add(btnCloseDetail, 0, 6);
            lblContact.AutoSize = true;
            lblContact.MaximumSize = new Size(360, 0);
            lblParcel.AutoSize = true;
            lblStage.AutoSize = true;
            lblAgent.AutoSize = true;

            timelineList.Dock = DockStyle.Fill;
            timelineList.FlowDirection = FlowDirection.TopDown;
            timelineList.WrapContents = false;
            timelineList.AutoScroll = true;

            detailButtons.Dock = DockStyle.Fill;
            detailButtons.FlowDirection = FlowDirection.LeftToRight;
            detailButtons.WrapContents = true;
            detailButtons.AutoSize = true;
            detailButtons.Controls.Add(btnMove);
            detailButtons.Controls.Add(btnLogCall);
            detailButtons.Controls.Add(btnLogText);
            detailButtons.Controls.Add(btnLogEmail);
            detailButtons.Controls.Add(btnLogViewing);
            detailButtons.Controls.Add(btnAddNote);
            detailButtons.Controls.Add(btnRequestClosed);
            detailButtons.Controls.Add(btnSwitchLot);
            detailButtons.Controls.Add(btnOpenContact);
            detailButtons.Controls.Add(btnShowMap);
            foreach (var button in new[] { btnMove, btnLogCall, btnLogText, btnLogEmail,
                                           btnLogViewing, btnAddNote, btnRequestClosed,
                                           btnSwitchLot, btnOpenContact, btnShowMap })
                button.AutoSize = true;
            btnCloseDetail.Text = "Close";
            btnMove.Text = "Move to...";
            btnLogCall.Text = "Log call";
            btnLogText.Text = "Log text";
            btnLogEmail.Text = "Log email";
            btnLogViewing.Text = "Log viewing";
            btnAddNote.Text = "Add note";
            btnRequestClosed.Text = "Request Closed";
            btnSwitchLot.Text = "Switch lot";
            btnOpenContact.Text = "Open contact";
            btnShowMap.Text = "Show on map";
            btnOpenContact.Click += (_, _) =>
            {
                if (_selectedLeadId is not null)
                {
                    var card = _board?.Columns.SelectMany(c => c.Cards)
                        .FirstOrDefault(c => c.LeadId == _selectedLeadId);
                    if (card is not null)
                        ShowContactRequested?.Invoke(card.ContactId);
                }
            };
            btnShowMap.Click += (_, _) =>
            {
                if (_board is null || _selectedLeadId is not { } leadId) return;
                var card = _board.Columns.SelectMany(c => c.Cards).FirstOrDefault(c => c.LeadId == leadId);
                if (card is not null) ShowParcelOnMap?.Invoke(card.ParcelId);
            };

            filterTimer.Interval = 300;
            Controls.Add(layout);
            Name = "PipelineView";
            Size = new Size(1100, 560);

            layout.ResumeLayout(false);
            filters.ResumeLayout(false);
            splitDetail.Panel1.ResumeLayout(false);
            splitDetail.Panel2.ResumeLayout(false);
            detailPanel.ResumeLayout(false);
            detailLayout.ResumeLayout(false);
            detailButtons.ResumeLayout(false);
            ResumeLayout(false);
        }

        private TableLayoutPanel layout;
        private TableLayoutPanel filters;
        private TextBox txtSearch;
        private ComboBox cboParcel;
        private ComboBox cboAgent;
        private CheckBox chkOlder;
        private Button btnConfigureStages;
        private Label lblStatus;
        private SplitContainer splitDetail;
        private FlowLayoutPanel boardPanel;
        private Panel detailPanel;
        private TableLayoutPanel detailLayout;
        private Label lblContact;
        private Label lblParcel;
        private Label lblStage;
        private Label lblAgent;
        private FlowLayoutPanel timelineList;
        private FlowLayoutPanel detailButtons;
        private Button btnCloseDetail;
        private Button btnMove;
        private Button btnLogCall;
        private Button btnLogText;
        private Button btnLogEmail;
        private Button btnLogViewing;
        private Button btnAddNote;
        private Button btnRequestClosed;
        private Button btnSwitchLot;
        private Button btnOpenContact;
        private Button btnShowMap;
        private System.Windows.Forms.Timer filterTimer;
    }
}

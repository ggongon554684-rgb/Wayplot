#nullable disable
namespace WayPlot.Views
{
    partial class InteractionsView
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
            btnLogInteraction = new Button();
            filters = new TableLayoutPanel();
            cboType = new ComboBox();
            lblSummary = new Label();
            listInteractions = new ListView();
            colWhen = new ColumnHeader();
            colContact = new ColumnHeader();
            colParcel = new ColumnHeader();
            colType = new ColumnHeader();
            colChannel = new ColumnHeader();
            colNote = new ColumnHeader();
            colRating = new ColumnHeader();
            colComplaintStatus = new ColumnHeader();
            txtDetail = new TextBox();
            card.Controls.Add(layout);
            card.Dock = DockStyle.Fill;
            card.Name = "card";
            card.TabIndex = 0;
            layout.Controls.Add(toolbar, 0, 0);
            layout.Controls.Add(filters, 0, 1);
            layout.Controls.Add(lblSummary, 0, 2);
            layout.Controls.Add(listInteractions, 0, 3);
            layout.Controls.Add(txtDetail, 0, 4);
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Dock = DockStyle.Fill;
            layout.Name = "layout";
            layout.Padding = new Padding(24);
            layout.RowCount = 5;
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle());
            toolbar.Controls.Add(lblTitle, 0, 0);
            toolbar.Controls.Add(btnLogInteraction, 1, 0);
            toolbar.ColumnCount = 2;
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.Dock = DockStyle.Fill;
            toolbar.Margin = new Padding(0, 0, 0, 12);
            toolbar.Name = "toolbar";
            toolbar.RowCount = 1;
            toolbar.RowStyles.Add(new RowStyle());
            lblTitle.Anchor = AnchorStyles.Left;
            lblTitle.AutoSize = true;
            lblTitle.Margin = new Padding(0);
            lblTitle.Name = "lblTitle";
            lblTitle.Text = "Interactions";
            lblTitle.UseMnemonic = false;
            btnLogInteraction.AutoSize = true;
            btnLogInteraction.Margin = new Padding(12, 0, 0, 0);
            btnLogInteraction.Name = "btnLogInteraction";
            btnLogInteraction.Text = "Log interaction";
            btnLogInteraction.UseVisualStyleBackColor = false;
            filters.Controls.Add(cboType, 0, 0);
            filters.ColumnCount = 1;
            filters.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250F));
            filters.Dock = DockStyle.Fill;
            filters.Margin = new Padding(0, 0, 0, 4);
            filters.Name = "filters";
            filters.RowCount = 1;
            filters.RowStyles.Add(new RowStyle());
            cboType.Dock = DockStyle.Fill;
            cboType.DropDownStyle = ComboBoxStyle.DropDownList;
            cboType.FormattingEnabled = true;
            cboType.Margin = new Padding(0);
            cboType.Name = "cboType";
            lblSummary.Anchor = AnchorStyles.Left;
            lblSummary.AutoSize = true;
            lblSummary.Margin = new Padding(0, 2, 0, 10);
            lblSummary.Name = "lblSummary";
            lblSummary.Text = "Loading interactions...";
            lblSummary.UseMnemonic = false;
            listInteractions.BorderStyle = BorderStyle.None;
            listInteractions.Columns.AddRange(new ColumnHeader[] { colWhen, colContact, colParcel, colType, colChannel, colNote, colRating, colComplaintStatus });
            listInteractions.Dock = DockStyle.Fill;
            listInteractions.FullRowSelect = true;
            listInteractions.HideSelection = false;
            listInteractions.Margin = new Padding(0);
            listInteractions.MultiSelect = false;
            listInteractions.Name = "listInteractions";
            listInteractions.UseCompatibleStateImageBehavior = false;
            listInteractions.View = View.Details;
            colWhen.Text = "When";
            colWhen.Width = 140;
            colContact.Text = "Contact";
            colContact.Width = 160;
            colParcel.Text = "Parcel";
            colParcel.Width = 160;
            colType.Text = "Type";
            colType.Width = 95;
            colChannel.Text = "Channel";
            colChannel.Width = 90;
            colNote.Text = "Note";
            colNote.Width = 200;
            colRating.Text = "Rating";
            colRating.Width = 70;
            colComplaintStatus.Text = "Complaint status";
            colComplaintStatus.Width = 130;
            txtDetail.Dock = DockStyle.Fill;
            txtDetail.Margin = new Padding(0, 10, 0, 0);
            txtDetail.MinimumSize = new Size(0, 54);
            txtDetail.Multiline = true;
            txtDetail.Name = "txtDetail";
            txtDetail.ReadOnly = true;
            txtDetail.ScrollBars = ScrollBars.Vertical;
            txtDetail.TabIndex = 0;
            txtDetail.Text = "Select an interaction to read its full note.";
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(card);
            Name = "InteractionsView";
            Size = new Size(1100, 560);
        }

        private WayPlot.UI.RoundedPanel card;
        private TableLayoutPanel layout;
        private TableLayoutPanel toolbar;
        private Label lblTitle;
        private Button btnLogInteraction;
        private TableLayoutPanel filters;
        private ComboBox cboType;
        private Label lblSummary;
        private ListView listInteractions;
        private ColumnHeader colWhen;
        private ColumnHeader colContact;
        private ColumnHeader colParcel;
        private ColumnHeader colType;
        private ColumnHeader colChannel;
        private ColumnHeader colNote;
        private ColumnHeader colRating;
        private ColumnHeader colComplaintStatus;
        private TextBox txtDetail;
    }
}

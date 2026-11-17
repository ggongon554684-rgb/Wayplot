#nullable disable
namespace WayPlot.Views
{
    partial class LogInteractionDialog
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
            layout = new TableLayoutPanel();
            lblTitle = new Label();
            lblLead = new Label();
            txtLeadSearch = new TextBox();
            cboLead = new ComboBox();
            lblLeadStatus = new Label();
            pairLayout = new TableLayoutPanel();
            lblType = new Label();
            lblChannel = new Label();
            cboType = new ComboBox();
            cboChannel = new ComboBox();
            dateLayout = new TableLayoutPanel();
            lblWhen = new Label();
            dtpOccurredAt = new DateTimePicker();
            lblNote = new Label();
            txtNote = new TextBox();
            ratingLayout = new TableLayoutPanel();
            lblRating = new Label();
            cboRating = new ComboBox();
            lblError = new Label();
            buttons = new FlowLayoutPanel();
            btnSave = new Button();
            btnCancel = new Button();
            searchTimer = new System.Windows.Forms.Timer(components);
            layout.SuspendLayout();
            pairLayout.SuspendLayout();
            dateLayout.SuspendLayout();
            ratingLayout.SuspendLayout();
            buttons.SuspendLayout();
            SuspendLayout();
            // 
            // layout
            // 
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(lblTitle, 0, 0);
            layout.Controls.Add(lblLead, 0, 1);
            layout.Controls.Add(txtLeadSearch, 0, 2);
            layout.Controls.Add(cboLead, 0, 3);
            layout.Controls.Add(lblLeadStatus, 0, 4);
            layout.Controls.Add(pairLayout, 0, 5);
            layout.Controls.Add(dateLayout, 0, 6);
            layout.Controls.Add(lblNote, 0, 7);
            layout.Controls.Add(txtNote, 0, 8);
            layout.Controls.Add(ratingLayout, 0, 9);
            layout.Controls.Add(lblError, 0, 10);
            layout.Controls.Add(buttons, 0, 11);
            layout.Dock = DockStyle.Fill;
            layout.Location = new Point(0, 0);
            layout.Margin = new Padding(4);
            layout.Name = "layout";
            layout.Padding = new Padding(30);
            layout.RowCount = 12;
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.Size = new Size(750, 875);
            layout.TabIndex = 0;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(30, 30);
            lblTitle.Margin = new Padding(0, 0, 0, 15);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(109, 20);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Log interaction";
            lblTitle.UseMnemonic = false;
            // 
            // lblLead
            // 
            lblLead.AutoSize = true;
            lblLead.Location = new Point(30, 65);
            lblLead.Margin = new Padding(0, 0, 0, 5);
            lblLead.Name = "lblLead";
            lblLead.Size = new Size(41, 20);
            lblLead.TabIndex = 1;
            lblLead.Text = "Lead";
            lblLead.UseMnemonic = false;
            // 
            // txtLeadSearch
            // 
            txtLeadSearch.Dock = DockStyle.Fill;
            txtLeadSearch.Location = new Point(30, 90);
            txtLeadSearch.Margin = new Padding(0, 0, 0, 8);
            txtLeadSearch.Name = "txtLeadSearch";
            txtLeadSearch.PlaceholderText = "Search contact or parcel";
            txtLeadSearch.Size = new Size(690, 27);
            txtLeadSearch.TabIndex = 2;
            // 
            // cboLead
            // 
            cboLead.Dock = DockStyle.Fill;
            cboLead.DropDownStyle = ComboBoxStyle.DropDownList;
            cboLead.FormattingEnabled = true;
            cboLead.Location = new Point(30, 125);
            cboLead.Margin = new Padding(0, 0, 0, 5);
            cboLead.Name = "cboLead";
            cboLead.Size = new Size(690, 28);
            cboLead.TabIndex = 3;
            // 
            // lblLeadStatus
            // 
            lblLeadStatus.AutoSize = true;
            lblLeadStatus.Location = new Point(30, 158);
            lblLeadStatus.Margin = new Padding(0, 0, 0, 12);
            lblLeadStatus.Name = "lblLeadStatus";
            lblLeadStatus.Size = new Size(0, 20);
            lblLeadStatus.TabIndex = 4;
            lblLeadStatus.UseMnemonic = false;
            // 
            // pairLayout
            // 
            pairLayout.ColumnCount = 2;
            pairLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pairLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pairLayout.Controls.Add(lblType, 0, 0);
            pairLayout.Controls.Add(lblChannel, 1, 0);
            pairLayout.Controls.Add(cboType, 0, 1);
            pairLayout.Controls.Add(cboChannel, 1, 1);
            pairLayout.Dock = DockStyle.Fill;
            pairLayout.Location = new Point(30, 190);
            pairLayout.Margin = new Padding(0, 0, 0, 12);
            pairLayout.Name = "pairLayout";
            pairLayout.RowCount = 2;
            pairLayout.RowStyles.Add(new RowStyle());
            pairLayout.RowStyles.Add(new RowStyle());
            pairLayout.Size = new Size(690, 125);
            pairLayout.TabIndex = 5;
            // 
            // lblType
            // 
            lblType.AutoSize = true;
            lblType.Location = new Point(0, 0);
            lblType.Margin = new Padding(0, 0, 0, 5);
            lblType.Name = "lblType";
            lblType.Size = new Size(40, 20);
            lblType.TabIndex = 0;
            lblType.Text = "Type";
            lblType.UseMnemonic = false;
            // 
            // lblChannel
            // 
            lblChannel.AutoSize = true;
            lblChannel.Location = new Point(360, 0);
            lblChannel.Margin = new Padding(15, 0, 0, 5);
            lblChannel.Name = "lblChannel";
            lblChannel.Size = new Size(62, 20);
            lblChannel.TabIndex = 1;
            lblChannel.Text = "Channel";
            lblChannel.UseMnemonic = false;
            // 
            // cboType
            // 
            cboType.Dock = DockStyle.Fill;
            cboType.DropDownStyle = ComboBoxStyle.DropDownList;
            cboType.FormattingEnabled = true;
            cboType.Location = new Point(0, 25);
            cboType.Margin = new Padding(0, 0, 15, 0);
            cboType.Name = "cboType";
            cboType.Size = new Size(330, 28);
            cboType.TabIndex = 2;
            // 
            // cboChannel
            // 
            cboChannel.Dock = DockStyle.Fill;
            cboChannel.DropDownStyle = ComboBoxStyle.DropDownList;
            cboChannel.FormattingEnabled = true;
            cboChannel.Location = new Point(360, 25);
            cboChannel.Margin = new Padding(15, 0, 0, 0);
            cboChannel.Name = "cboChannel";
            cboChannel.Size = new Size(330, 28);
            cboChannel.TabIndex = 3;
            // 
            // dateLayout
            // 
            dateLayout.ColumnCount = 2;
            dateLayout.ColumnStyles.Add(new ColumnStyle());
            dateLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            dateLayout.Controls.Add(lblWhen, 0, 0);
            dateLayout.Controls.Add(dtpOccurredAt, 1, 0);
            dateLayout.Dock = DockStyle.Fill;
            dateLayout.Location = new Point(30, 327);
            dateLayout.Margin = new Padding(0, 0, 0, 12);
            dateLayout.Name = "dateLayout";
            dateLayout.RowCount = 1;
            dateLayout.RowStyles.Add(new RowStyle());
            dateLayout.Size = new Size(690, 125);
            dateLayout.TabIndex = 6;
            // 
            // lblWhen
            // 
            lblWhen.Anchor = AnchorStyles.Left;
            lblWhen.AutoSize = true;
            lblWhen.Location = new Point(0, 52);
            lblWhen.Margin = new Padding(0, 0, 15, 0);
            lblWhen.Name = "lblWhen";
            lblWhen.Size = new Size(47, 20);
            lblWhen.TabIndex = 0;
            lblWhen.Text = "When";
            lblWhen.UseMnemonic = false;
            // 
            // dtpOccurredAt
            // 
            dtpOccurredAt.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            dtpOccurredAt.CustomFormat = "yyyy-MM-dd HH:mm";
            dtpOccurredAt.Format = DateTimePickerFormat.Custom;
            dtpOccurredAt.Location = new Point(66, 49);
            dtpOccurredAt.Margin = new Padding(4);
            dtpOccurredAt.Name = "dtpOccurredAt";
            dtpOccurredAt.ShowUpDown = true;
            dtpOccurredAt.Size = new Size(620, 27);
            dtpOccurredAt.TabIndex = 1;
            // 
            // lblNote
            // 
            lblNote.AutoSize = true;
            lblNote.Location = new Point(30, 464);
            lblNote.Margin = new Padding(0, 0, 0, 5);
            lblNote.Name = "lblNote";
            lblNote.Size = new Size(42, 20);
            lblNote.TabIndex = 7;
            lblNote.Text = "Note";
            lblNote.UseMnemonic = false;
            // 
            // txtNote
            // 
            txtNote.Dock = DockStyle.Fill;
            txtNote.Location = new Point(30, 489);
            txtNote.Margin = new Padding(0, 0, 0, 12);
            txtNote.MaxLength = 2000;
            txtNote.MinimumSize = new Size(4, 136);
            txtNote.Multiline = true;
            txtNote.Name = "txtNote";
            txtNote.ScrollBars = ScrollBars.Vertical;
            txtNote.Size = new Size(690, 141);
            txtNote.TabIndex = 8;
            // 
            // ratingLayout
            // 
            ratingLayout.ColumnCount = 2;
            ratingLayout.ColumnStyles.Add(new ColumnStyle());
            ratingLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            ratingLayout.Controls.Add(lblRating, 0, 0);
            ratingLayout.Controls.Add(cboRating, 1, 0);
            ratingLayout.Dock = DockStyle.Fill;
            ratingLayout.Location = new Point(30, 642);
            ratingLayout.Margin = new Padding(0, 0, 0, 10);
            ratingLayout.Name = "ratingLayout";
            ratingLayout.RowCount = 1;
            ratingLayout.RowStyles.Add(new RowStyle());
            ratingLayout.Size = new Size(690, 125);
            ratingLayout.TabIndex = 9;
            ratingLayout.Paint += ratingLayout_Paint;
            // 
            // lblRating
            // 
            lblRating.Anchor = AnchorStyles.Left;
            lblRating.AutoSize = true;
            lblRating.Location = new Point(0, 52);
            lblRating.Margin = new Padding(0, 0, 15, 0);
            lblRating.Name = "lblRating";
            lblRating.Size = new Size(52, 20);
            lblRating.TabIndex = 0;
            lblRating.Text = "Rating";
            lblRating.UseMnemonic = false;
            // 
            // cboRating
            // 
            cboRating.Dock = DockStyle.Left;
            cboRating.DropDownStyle = ComboBoxStyle.DropDownList;
            cboRating.FormattingEnabled = true;
            cboRating.Location = new Point(71, 4);
            cboRating.Margin = new Padding(4);
            cboRating.Name = "cboRating";
            cboRating.Size = new Size(102, 28);
            cboRating.TabIndex = 1;
            // 
            // lblError
            // 
            lblError.AutoSize = true;
            lblError.Location = new Point(30, 777);
            lblError.Margin = new Padding(0, 0, 0, 10);
            lblError.Name = "lblError";
            lblError.Size = new Size(0, 20);
            lblError.TabIndex = 10;
            lblError.UseMnemonic = false;
            lblError.Visible = false;
            // 
            // buttons
            // 
            buttons.AutoSize = true;
            buttons.Controls.Add(btnSave);
            buttons.Controls.Add(btnCancel);
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Location = new Point(30, 807);
            buttons.Margin = new Padding(0);
            buttons.Name = "buttons";
            buttons.Size = new Size(690, 38);
            buttons.TabIndex = 11;
            buttons.WrapContents = false;
            // 
            // btnSave
            // 
            btnSave.AutoSize = true;
            btnSave.Location = new Point(541, 0);
            btnSave.Margin = new Padding(10, 0, 0, 0);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(149, 38);
            btnSave.TabIndex = 0;
            btnSave.Text = "Log interaction";
            btnSave.UseVisualStyleBackColor = false;
            // 
            // btnCancel
            // 
            btnCancel.AutoSize = true;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(437, 0);
            btnCancel.Margin = new Padding(0);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(94, 38);
            btnCancel.TabIndex = 1;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = false;
            // 
            // searchTimer
            // 
            searchTimer.Interval = 300;
            // 
            // LogInteractionDialog
            // 
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(120F, 120F);
            AutoScaleMode = AutoScaleMode.Dpi;
            CancelButton = btnCancel;
            ClientSize = new Size(750, 875);
            Controls.Add(layout);
            Margin = new Padding(4);
            MinimizeBox = false;
            Name = "LogInteractionDialog";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            layout.ResumeLayout(false);
            layout.PerformLayout();
            pairLayout.ResumeLayout(false);
            pairLayout.PerformLayout();
            dateLayout.ResumeLayout(false);
            dateLayout.PerformLayout();
            ratingLayout.ResumeLayout(false);
            ratingLayout.PerformLayout();
            buttons.ResumeLayout(false);
            buttons.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel layout;
        private Label lblTitle;
        private Label lblLead;
        private TextBox txtLeadSearch;
        private ComboBox cboLead;
        private Label lblLeadStatus;
        private TableLayoutPanel pairLayout;
        private Label lblType;
        private Label lblChannel;
        private ComboBox cboType;
        private ComboBox cboChannel;
        private TableLayoutPanel dateLayout;
        private Label lblWhen;
        private DateTimePicker dtpOccurredAt;
        private Label lblNote;
        private TextBox txtNote;
        private TableLayoutPanel ratingLayout;
        private Label lblRating;
        private ComboBox cboRating;
        private Label lblError;
        private FlowLayoutPanel buttons;
        private Button btnSave;
        private Button btnCancel;
        private System.Windows.Forms.Timer searchTimer;
    }
}

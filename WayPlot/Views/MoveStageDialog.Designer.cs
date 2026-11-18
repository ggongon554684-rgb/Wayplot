#nullable disable

namespace WayPlot.Views
{
    partial class MoveStageDialog
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
            layout = new TableLayoutPanel();
            lblTitle = new Label();
            cboStage = new ComboBox();
            lblNote = new Label();
            txtNote = new TextBox();
            lblHelp = new Label();
            lblStatus = new Label();
            buttons = new FlowLayoutPanel();
            btnCancel = new Button();
            btnSave = new Button();
            layout.SuspendLayout();
            buttons.SuspendLayout();
            SuspendLayout();
            // layout
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(lblTitle, 0, 0);
            layout.Controls.Add(cboStage, 0, 1);
            layout.Controls.Add(lblNote, 0, 2);
            layout.Controls.Add(txtNote, 0, 3);
            layout.Controls.Add(lblHelp, 0, 4);
            layout.Controls.Add(lblStatus, 0, 5);
            layout.Controls.Add(buttons, 0, 6);
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(24);
            layout.RowCount = 7;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.TabIndex = 0;
            // lblTitle
            lblTitle.AutoSize = true;
            lblTitle.Margin = new Padding(0, 0, 0, 12);
            lblTitle.Text = "Move lead stage";
            lblTitle.UseMnemonic = false;
            // cboStage
            cboStage.Dock = DockStyle.Top;
            cboStage.DropDownStyle = ComboBoxStyle.DropDownList;
            cboStage.FormattingEnabled = true;
            cboStage.Margin = new Padding(0, 0, 0, 16);
            cboStage.Name = "cboStage";
            cboStage.TabIndex = 0;
            // lblNote
            lblNote.AutoSize = true;
            lblNote.Margin = new Padding(0, 0, 0, 4);
            lblNote.Name = "lblNote";
            lblNote.Text = "Notes (optional)";
            lblNote.UseMnemonic = false;
            // txtNote
            txtNote.Dock = DockStyle.Fill;
            txtNote.Margin = new Padding(0, 0, 0, 6);
            txtNote.Multiline = true;
            txtNote.Name = "txtNote";
            txtNote.ScrollBars = ScrollBars.Vertical;
            txtNote.TabIndex = 1;
            // lblHelp
            lblHelp.AutoSize = true;
            lblHelp.Margin = new Padding(0, 0, 0, 8);
            lblHelp.Name = "lblHelp";
            lblHelp.Text = "Add an optional note to the stage history.";
            lblHelp.UseMnemonic = false;
            // lblStatus
            lblStatus.AutoSize = true;
            lblStatus.Margin = new Padding(0, 0, 0, 8);
            lblStatus.Name = "lblStatus";
            lblStatus.Text = "";
            lblStatus.UseMnemonic = false;
            // buttons
            buttons.AutoSize = true;
            buttons.Controls.Add(btnCancel);
            buttons.Controls.Add(btnSave);
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Margin = new Padding(0);
            buttons.Name = "buttons";
            buttons.WrapContents = false;
            // btnCancel
            btnCancel.AutoSize = true;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Margin = new Padding(8, 0, 0, 0);
            btnCancel.Name = "btnCancel";
            btnCancel.Text = "Cancel";
            btnCancel.TabIndex = 3;
            btnCancel.UseVisualStyleBackColor = false;
            // btnSave
            btnSave.AutoSize = true;
            btnSave.Name = "btnSave";
            btnSave.Text = "Move stage";
            btnSave.TabIndex = 2;
            btnSave.UseVisualStyleBackColor = false;
            // MoveStageDialog
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            CancelButton = btnCancel;
            ClientSize = new Size(560, 480);
            Controls.Add(layout);
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(480, 420);
            Name = "MoveStageDialog";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Move lead stage";
            layout.ResumeLayout(false);
            layout.PerformLayout();
            buttons.ResumeLayout(false);
            buttons.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel layout;
        private Label lblTitle;
        private ComboBox cboStage;
        private Label lblNote;
        private TextBox txtNote;
        private Label lblHelp;
        private Label lblStatus;
        private FlowLayoutPanel buttons;
        private Button btnCancel;
        private Button btnSave;
    }
}

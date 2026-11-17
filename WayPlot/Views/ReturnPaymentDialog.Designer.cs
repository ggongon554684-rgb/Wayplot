#nullable disable
namespace WayPlot.Views
{
    partial class ReturnPaymentDialog
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
            lblHelp = new Label();
            lblNote = new Label();
            txtNote = new TextBox();
            lblError = new Label();
            buttons = new FlowLayoutPanel();
            btnSave = new Button();
            btnCancel = new Button();
            layout.SuspendLayout();
            buttons.SuspendLayout();
            SuspendLayout();
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(lblTitle, 0, 0);
            layout.Controls.Add(lblHelp, 0, 1);
            layout.Controls.Add(lblNote, 0, 2);
            layout.Controls.Add(txtNote, 0, 3);
            layout.Controls.Add(lblError, 0, 4);
            layout.Controls.Add(buttons, 0, 5);
            layout.Dock = DockStyle.Fill;
            layout.Name = "layout";
            layout.Padding = new Padding(24);
            layout.RowCount = 6;
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            lblTitle.AutoSize = true;
            lblTitle.Margin = new Padding(0, 0, 0, 12);
            lblTitle.Name = "lblTitle";
            lblTitle.Text = "Return payment";
            lblTitle.UseMnemonic = false;
            lblHelp.AutoSize = true;
            lblHelp.Margin = new Padding(0, 0, 0, 12);
            lblHelp.Name = "lblHelp";
            lblHelp.Text = "This returns a pending payment entry to the Agent; it does not issue a refund.";
            lblHelp.UseMnemonic = false;
            lblNote.AutoSize = true;
            lblNote.Margin = new Padding(0, 0, 0, 4);
            lblNote.Name = "lblNote";
            lblNote.Text = "Review note (optional)";
            lblNote.UseMnemonic = false;
            txtNote.Dock = DockStyle.Fill;
            txtNote.Margin = new Padding(0, 0, 0, 12);
            txtNote.Multiline = true;
            txtNote.Name = "txtNote";
            txtNote.ScrollBars = ScrollBars.Vertical;
            lblError.AutoSize = true;
            lblError.Margin = new Padding(0, 0, 0, 8);
            lblError.MaximumSize = new Size(440, 0);
            lblError.Name = "lblError";
            lblError.Visible = false;
            lblError.UseMnemonic = false;
            buttons.AutoSize = true;
            buttons.Controls.Add(btnSave);
            buttons.Controls.Add(btnCancel);
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Margin = new Padding(0);
            buttons.Name = "buttons";
            btnSave.AutoSize = true;
            btnSave.Margin = new Padding(8, 0, 0, 0);
            btnSave.Name = "btnSave";
            btnSave.Text = "Return payment";
            btnSave.UseVisualStyleBackColor = false;
            btnCancel.AutoSize = true;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Margin = new Padding(0);
            btnCancel.Name = "btnCancel";
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = false;
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            CancelButton = btnCancel;
            ClientSize = new Size(520, 420);
            Controls.Add(layout);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ReturnPaymentDialog";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Return payment";
            layout.ResumeLayout(false);
            layout.PerformLayout();
            buttons.ResumeLayout(false);
            buttons.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel layout;
        private Label lblTitle;
        private Label lblHelp;
        private Label lblNote;
        private TextBox txtNote;
        private Label lblError;
        private FlowLayoutPanel buttons;
        private Button btnSave;
        private Button btnCancel;
    }
}

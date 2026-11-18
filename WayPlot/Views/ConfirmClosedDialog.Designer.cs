#nullable disable
namespace WayPlot.Views
{
    partial class ConfirmClosedDialog
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
            lblStatus = new Label();
            cboStatus = new ComboBox();
            buttons = new FlowLayoutPanel();
            btnConfirm = new Button();
            btnCancel = new Button();
            layout.SuspendLayout();
            buttons.SuspendLayout();
            SuspendLayout();
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(lblTitle, 0, 0);
            layout.Controls.Add(lblStatus, 0, 1);
            layout.Controls.Add(cboStatus, 0, 2);
            layout.Controls.Add(buttons, 0, 3);
            layout.Dock = DockStyle.Fill;
            layout.Name = "layout";
            layout.Padding = new Padding(24);
            layout.RowCount = 4;
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            lblTitle.AutoSize = true;
            lblTitle.Margin = new Padding(0, 0, 0, 16);
            lblTitle.Name = "lblTitle";
            lblTitle.Text = "Confirm Closed";
            lblTitle.UseMnemonic = false;
            lblStatus.AutoSize = true;
            lblStatus.Margin = new Padding(0, 0, 0, 4);
            lblStatus.Name = "lblStatus";
            lblStatus.Text = "Set the parcel status to";
            lblStatus.UseMnemonic = false;
            cboStatus.Dock = DockStyle.Fill;
            cboStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            cboStatus.FormattingEnabled = true;
            cboStatus.Margin = new Padding(0, 0, 0, 16);
            cboStatus.Name = "cboStatus";
            buttons.AutoSize = true;
            buttons.Controls.Add(btnConfirm);
            buttons.Controls.Add(btnCancel);
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Margin = new Padding(0);
            buttons.Name = "buttons";
            btnConfirm.AutoSize = true;
            btnConfirm.Margin = new Padding(8, 0, 0, 0);
            btnConfirm.Name = "btnConfirm";
            btnConfirm.Text = "Confirm";
            btnConfirm.UseVisualStyleBackColor = false;
            btnCancel.AutoSize = true;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Margin = new Padding(0);
            btnCancel.Name = "btnCancel";
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = false;
            AcceptButton = btnConfirm;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            CancelButton = btnCancel;
            ClientSize = new Size(420, 220);
            Controls.Add(layout);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ConfirmClosedDialog";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Confirm Closed";
            layout.ResumeLayout(false);
            layout.PerformLayout();
            buttons.ResumeLayout(false);
            buttons.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel layout;
        private Label lblTitle;
        private Label lblStatus;
        private ComboBox cboStatus;
        private FlowLayoutPanel buttons;
        private Button btnConfirm;
        private Button btnCancel;
    }
}

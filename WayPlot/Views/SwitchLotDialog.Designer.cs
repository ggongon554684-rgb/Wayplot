#nullable disable
using WayPlot.UI;

namespace WayPlot.Views
{
    partial class SwitchLotDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            layout = new TableLayoutPanel();
            lblCurrent = new Label();
            lblEffect = new Label();
            lblParcel = new Label();
            picker = new ParcelPickerControl();
            lblReason = new Label();
            txtReason = new TextBox();
            buttons = new FlowLayoutPanel();
            btnSave = new Button();
            btnCancel = new Button();
            layout.SuspendLayout();
            buttons.SuspendLayout();
            SuspendLayout();

            layout.ColumnCount = 1;
            layout.Controls.Add(lblCurrent, 0, 0);
            layout.Controls.Add(lblEffect, 0, 1);
            layout.Controls.Add(lblParcel, 0, 2);
            layout.Controls.Add(picker, 0, 3);
            layout.Controls.Add(lblReason, 0, 4);
            layout.Controls.Add(txtReason, 0, 5);
            layout.Controls.Add(buttons, 0, 6);
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(24);
            layout.RowCount = 7;
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 220F));
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
            layout.RowStyles.Add(new RowStyle());

            lblCurrent.AutoSize = true;
            lblEffect.AutoSize = true;
            lblEffect.MaximumSize = new Size(460, 0);
            lblParcel.Text = "Choose a new available lot";
            lblParcel.AutoSize = true;
            lblReason.Text = "Reason (optional)";
            lblReason.AutoSize = true;
            txtReason.Multiline = true;
            txtReason.ScrollBars = ScrollBars.Vertical;
            txtReason.Dock = DockStyle.Fill;

            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Controls.Add(btnSave);
            buttons.Controls.Add(btnCancel);
            btnSave.Text = "Switch lot";
            btnCancel.Text = "Cancel";
            btnCancel.DialogResult = DialogResult.Cancel;

            AcceptButton = btnSave;
            CancelButton = btnCancel;
            ClientSize = new Size(520, 560);
            Controls.Add(layout);
            Text = "Switch lot";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowIcon = false;
            layout.ResumeLayout(false);
            layout.PerformLayout();
            buttons.ResumeLayout(false);
            buttons.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel layout;
        private Label lblCurrent;
        private Label lblEffect;
        private Label lblParcel;
        private ParcelPickerControl picker;
        private Label lblReason;
        private TextBox txtReason;
        private FlowLayoutPanel buttons;
        private Button btnSave;
        private Button btnCancel;
    }
}

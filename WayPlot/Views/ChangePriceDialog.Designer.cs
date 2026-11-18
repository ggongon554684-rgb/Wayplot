#nullable disable
namespace WayPlot.Views
{
    partial class ChangePriceDialog
    {
        private System.ComponentModel.IContainer components = null;
        private TableLayoutPanel layout;
        private Label lblTitle;
        private Label lblPrice;
        private NumericUpDown numPrice;
        private Label lblReservationFee;
        private NumericUpDown numReservationFee;
        private Label lblError;
        private FlowLayoutPanel buttons;
        private Button btnSave;
        private Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                components?.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            layout = new TableLayoutPanel();
            lblTitle = new Label();
            lblPrice = new Label();
            numPrice = new NumericUpDown();
            lblReservationFee = new Label();
            numReservationFee = new NumericUpDown();
            lblError = new Label();
            buttons = new FlowLayoutPanel();
            btnSave = new Button();
            btnCancel = new Button();
            ((System.ComponentModel.ISupportInitialize)numPrice).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numReservationFee).BeginInit();
            layout.SuspendLayout();
            buttons.SuspendLayout();
            SuspendLayout();
            layout.ColumnCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(lblTitle, 0, 0);
            layout.SetColumnSpan(lblTitle, 2);
            layout.Controls.Add(lblPrice, 0, 1);
            layout.Controls.Add(numPrice, 1, 1);
            layout.Controls.Add(lblReservationFee, 0, 2);
            layout.Controls.Add(numReservationFee, 1, 2);
            layout.Controls.Add(lblError, 0, 3);
            layout.SetColumnSpan(lblError, 2);
            layout.Controls.Add(buttons, 0, 4);
            layout.SetColumnSpan(buttons, 2);
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(20);
            layout.RowCount = 5;
            for (var i = 0; i < 5; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            lblTitle.AutoSize = true;
            lblTitle.Margin = new Padding(0, 0, 0, 12);
            lblTitle.Text = "Change parcel price";
            lblTitle.UseMnemonic = false;
            lblPrice.Anchor = AnchorStyles.Left;
            lblPrice.AutoSize = true;
            lblPrice.Margin = new Padding(0, 0, 12, 8);
            lblPrice.Text = "Price";
            lblPrice.UseMnemonic = false;
            numPrice.Dock = DockStyle.Fill;
            numPrice.DecimalPlaces = 2;
            numPrice.Increment = 1000;
            numPrice.Maximum = decimal.MaxValue;
            numPrice.Margin = new Padding(0, 0, 0, 8);
            lblReservationFee.Anchor = AnchorStyles.Left;
            lblReservationFee.AutoSize = true;
            lblReservationFee.Margin = new Padding(0, 0, 12, 8);
            lblReservationFee.Text = "Reservation fee";
            lblReservationFee.UseMnemonic = false;
            numReservationFee.Dock = DockStyle.Fill;
            numReservationFee.DecimalPlaces = 2;
            numReservationFee.Increment = 1000;
            numReservationFee.Maximum = decimal.MaxValue;
            numReservationFee.Margin = new Padding(0, 0, 0, 8);
            lblError.AutoSize = true;
            lblError.Margin = new Padding(0, 2, 0, 8);
            lblError.UseMnemonic = false;
            buttons.AutoSize = true;
            buttons.Controls.Add(btnCancel);
            buttons.Controls.Add(btnSave);
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.WrapContents = false;
            btnCancel.AutoSize = true;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = false;
            btnSave.AutoSize = true;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = false;
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            CancelButton = btnCancel;
            ClientSize = new Size(480, 260);
            Controls.Add(layout);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ChangePriceDialog";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Change price";
            ((System.ComponentModel.ISupportInitialize)numPrice).EndInit();
            ((System.ComponentModel.ISupportInitialize)numReservationFee).EndInit();
            layout.ResumeLayout(false);
            layout.PerformLayout();
            buttons.ResumeLayout(false);
            buttons.PerformLayout();
            ResumeLayout(false);
        }
    }
}

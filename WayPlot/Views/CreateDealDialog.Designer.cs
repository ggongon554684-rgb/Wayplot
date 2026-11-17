#nullable disable
namespace WayPlot.Views
{
    partial class CreateDealDialog
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
            lblLead = new Label();
            cboLead = new ComboBox();
            lblPrice = new Label();
            nudPrice = new NumericUpDown();
            lblDownPayment = new Label();
            nudDownPayment = new NumericUpDown();
            lblReservation = new Label();
            lblError = new Label();
            buttons = new FlowLayoutPanel();
            btnSave = new Button();
            btnCancel = new Button();
            layout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudPrice).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudDownPayment).BeginInit();
            buttons.SuspendLayout();
            SuspendLayout();
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(lblTitle, 0, 0);
            layout.Controls.Add(lblLead, 0, 1);
            layout.Controls.Add(cboLead, 0, 2);
            layout.Controls.Add(lblPrice, 0, 3);
            layout.Controls.Add(nudPrice, 0, 4);
            layout.Controls.Add(lblDownPayment, 0, 5);
            layout.Controls.Add(nudDownPayment, 0, 6);
            layout.Controls.Add(lblReservation, 0, 7);
            layout.Controls.Add(lblError, 0, 8);
            layout.Controls.Add(buttons, 0, 9);
            layout.Dock = DockStyle.Fill;
            layout.Name = "layout";
            layout.Padding = new Padding(24);
            layout.RowCount = 10;
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            lblTitle.AutoSize = true;
            lblTitle.Margin = new Padding(0, 0, 0, 16);
            lblTitle.Name = "lblTitle";
            lblTitle.Text = "Request Closed";
            lblTitle.UseMnemonic = false;
            lblLead.AutoSize = true;
            lblLead.Margin = new Padding(0, 0, 0, 4);
            lblLead.Name = "lblLead";
            lblLead.Text = "Negotiation lead";
            lblLead.UseMnemonic = false;
            cboLead.Dock = DockStyle.Fill;
            cboLead.DropDownStyle = ComboBoxStyle.DropDownList;
            cboLead.FormattingEnabled = true;
            cboLead.Margin = new Padding(0, 0, 0, 16);
            cboLead.Name = "cboLead";
            lblPrice.AutoSize = true;
            lblPrice.Margin = new Padding(0, 0, 0, 4);
            lblPrice.Name = "lblPrice";
            lblPrice.Text = "Final deal price";
            lblPrice.UseMnemonic = false;
            nudPrice.DecimalPlaces = 2;
            nudPrice.Dock = DockStyle.Fill;
            nudPrice.Maximum = 1000000000M;
            nudPrice.Minimum = 0M;
            nudPrice.Name = "nudPrice";
            nudPrice.ThousandsSeparator = true;
            nudPrice.Margin = new Padding(0, 0, 0, 12);
            lblDownPayment.AutoSize = true;
            lblDownPayment.Margin = new Padding(0, 0, 0, 4);
            lblDownPayment.Name = "lblDownPayment";
            lblDownPayment.Text = "Down payment amount";
            lblDownPayment.UseMnemonic = false;
            nudDownPayment.DecimalPlaces = 2;
            nudDownPayment.Dock = DockStyle.Fill;
            nudDownPayment.Maximum = 1000000000M;
            nudDownPayment.Minimum = 0M;
            nudDownPayment.Name = "nudDownPayment";
            nudDownPayment.ThousandsSeparator = true;
            nudDownPayment.Margin = new Padding(0, 0, 0, 12);
            lblReservation.AutoSize = true;
            lblReservation.Margin = new Padding(0, 0, 0, 12);
            lblReservation.Name = "lblReservation";
            lblReservation.Text = "Reservation fee";
            lblReservation.UseMnemonic = false;
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
            btnSave.Text = "Request Closed";
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
            ClientSize = new Size(520, 360);
            Controls.Add(layout);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "CreateDealDialog";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Request Closed";
            layout.ResumeLayout(false);
            layout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudPrice).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudDownPayment).EndInit();
            buttons.ResumeLayout(false);
            buttons.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel layout;
        private Label lblTitle;
        private Label lblLead;
        private ComboBox cboLead;
        private Label lblPrice;
        private NumericUpDown nudPrice;
        private Label lblDownPayment;
        private NumericUpDown nudDownPayment;
        private Label lblReservation;
        private Label lblError;
        private FlowLayoutPanel buttons;
        private Button btnSave;
        private Button btnCancel;
    }
}

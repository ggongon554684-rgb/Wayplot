#nullable disable
namespace WayPlot.Views
{
    partial class RecordPaymentDialog
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
            lblDeal = new Label();
            lblBalance = new Label();
            lblAmount = new Label();
            nudAmount = new NumericUpDown();
            lblMethod = new Label();
            cboMethod = new ComboBox();
            lblError = new Label();
            buttons = new FlowLayoutPanel();
            btnSave = new Button();
            btnCancel = new Button();
            layout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudAmount).BeginInit();
            buttons.SuspendLayout();
            SuspendLayout();
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(lblTitle, 0, 0);
            layout.Controls.Add(lblDeal, 0, 1);
            layout.Controls.Add(lblBalance, 0, 2);
            layout.Controls.Add(lblAmount, 0, 3);
            layout.Controls.Add(nudAmount, 0, 4);
            layout.Controls.Add(lblMethod, 0, 5);
            layout.Controls.Add(cboMethod, 0, 6);
            layout.Controls.Add(lblError, 0, 7);
            layout.Controls.Add(buttons, 0, 8);
            layout.Dock = DockStyle.Fill;
            layout.Name = "layout";
            layout.Padding = new Padding(24);
            layout.RowCount = 9;
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
            lblTitle.Margin = new Padding(0, 0, 0, 12);
            lblTitle.Name = "lblTitle";
            lblTitle.Text = "Record payment";
            lblTitle.UseMnemonic = false;
            lblDeal.AutoSize = true;
            lblDeal.Margin = new Padding(0, 0, 0, 4);
            lblDeal.Name = "lblDeal";
            lblDeal.Text = "Selected deal";
            lblDeal.UseMnemonic = false;
            lblBalance.AutoSize = true;
            lblBalance.Margin = new Padding(0, 0, 0, 16);
            lblBalance.Name = "lblBalance";
            lblBalance.Text = "Remaining balance";
            lblBalance.UseMnemonic = false;
            lblAmount.AutoSize = true;
            lblAmount.Margin = new Padding(0, 0, 0, 4);
            lblAmount.Name = "lblAmount";
            lblAmount.Text = "Amount (partial payments allowed)";
            lblAmount.UseMnemonic = false;
            nudAmount.DecimalPlaces = 2;
            nudAmount.Dock = DockStyle.Fill;
            nudAmount.Maximum = 1000000000M;
            nudAmount.Minimum = 0M;
            nudAmount.Margin = new Padding(0, 0, 0, 12);
            nudAmount.Name = "nudAmount";
            nudAmount.ThousandsSeparator = true;
            lblMethod.AutoSize = true;
            lblMethod.Margin = new Padding(0, 0, 0, 4);
            lblMethod.Name = "lblMethod";
            lblMethod.Text = "Payment method";
            lblMethod.UseMnemonic = false;
            cboMethod.Dock = DockStyle.Fill;
            cboMethod.DropDownStyle = ComboBoxStyle.DropDownList;
            cboMethod.FormattingEnabled = true;
            cboMethod.Margin = new Padding(0, 0, 0, 12);
            cboMethod.Name = "cboMethod";
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
            btnSave.Text = "Record payment";
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
            ClientSize = new Size(520, 440);
            Controls.Add(layout);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "RecordPaymentDialog";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Record payment";
            layout.ResumeLayout(false);
            layout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudAmount).EndInit();
            buttons.ResumeLayout(false);
            buttons.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel layout;
        private Label lblTitle;
        private Label lblDeal;
        private Label lblBalance;
        private Label lblAmount;
        private NumericUpDown nudAmount;
        private Label lblMethod;
        private ComboBox cboMethod;
        private Label lblError;
        private FlowLayoutPanel buttons;
        private Button btnSave;
        private Button btnCancel;
    }
}

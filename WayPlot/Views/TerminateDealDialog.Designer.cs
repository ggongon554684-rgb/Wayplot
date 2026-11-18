#nullable disable
namespace WayPlot.Views
{
    partial class TerminateDealDialog
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
            lblPaidTotal = new Label();
            outcomeGroup = new GroupBox();
            outcomeLayout = new FlowLayoutPanel();
            rdoRefunded = new RadioButton();
            rdoPartlyRefunded = new RadioButton();
            rdoKept = new RadioButton();
            lblRefundAmount = new Label();
            nudRefundAmount = new NumericUpDown();
            lblReason = new Label();
            txtReason = new TextBox();
            lblEffects = new Label();
            lblError = new Label();
            buttons = new FlowLayoutPanel();
            btnSave = new Button();
            btnCancel = new Button();
            layout.SuspendLayout();
            outcomeGroup.SuspendLayout();
            outcomeLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudRefundAmount).BeginInit();
            buttons.SuspendLayout();
            SuspendLayout();
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(lblTitle, 0, 0);
            layout.Controls.Add(lblPaidTotal, 0, 1);
            layout.Controls.Add(outcomeGroup, 0, 2);
            layout.Controls.Add(lblRefundAmount, 0, 3);
            layout.Controls.Add(nudRefundAmount, 0, 4);
            layout.Controls.Add(lblReason, 0, 5);
            layout.Controls.Add(txtReason, 0, 6);
            layout.Controls.Add(lblEffects, 0, 7);
            layout.Controls.Add(lblError, 0, 8);
            layout.Controls.Add(buttons, 0, 9);
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(24);
            layout.RowCount = 10;
            for (var row = 0; row < 9; row++)
                layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            lblTitle.AutoSize = true;
            lblTitle.Margin = new Padding(0, 0, 0, 12);
            lblTitle.Text = "Terminate deal";
            lblTitle.UseMnemonic = false;
            lblPaidTotal.AutoSize = true;
            lblPaidTotal.Margin = new Padding(0, 0, 0, 12);
            lblPaidTotal.Text = "Confirmed payments:";
            lblPaidTotal.UseMnemonic = false;
            outcomeGroup.Controls.Add(outcomeLayout);
            outcomeGroup.Dock = DockStyle.Fill;
            outcomeGroup.Margin = new Padding(0, 0, 0, 12);
            outcomeGroup.Padding = new Padding(12);
            outcomeGroup.Text = "Money already paid";
            outcomeLayout.Controls.Add(rdoRefunded);
            outcomeLayout.Controls.Add(rdoPartlyRefunded);
            outcomeLayout.Controls.Add(rdoKept);
            outcomeLayout.Dock = DockStyle.Fill;
            outcomeLayout.FlowDirection = FlowDirection.TopDown;
            outcomeLayout.WrapContents = false;
            rdoRefunded.AutoSize = true;
            rdoRefunded.Margin = new Padding(3, 4, 3, 8);
            rdoRefunded.Text = "Refunded in full";
            rdoRefunded.UseVisualStyleBackColor = true;
            rdoPartlyRefunded.AutoSize = true;
            rdoPartlyRefunded.Margin = new Padding(3, 4, 3, 8);
            rdoPartlyRefunded.Text = "Partly refunded";
            rdoPartlyRefunded.UseVisualStyleBackColor = true;
            rdoKept.AutoSize = true;
            rdoKept.Margin = new Padding(3, 4, 3, 4);
            rdoKept.Text = "Kept";
            rdoKept.UseVisualStyleBackColor = true;
            lblRefundAmount.AutoSize = true;
            lblRefundAmount.Margin = new Padding(0, 0, 0, 4);
            lblRefundAmount.Text = "Refund amount";
            lblRefundAmount.UseMnemonic = false;
            nudRefundAmount.DecimalPlaces = 2;
            nudRefundAmount.Dock = DockStyle.Fill;
            nudRefundAmount.Maximum = 9999999999999999.99M;
            nudRefundAmount.ThousandsSeparator = true;
            nudRefundAmount.Margin = new Padding(0, 0, 0, 12);
            nudRefundAmount.Enabled = false;
            lblReason.AutoSize = true;
            lblReason.Margin = new Padding(0, 0, 0, 4);
            lblReason.Text = "Reason (required, maximum 500 characters)";
            lblReason.UseMnemonic = false;
            txtReason.Dock = DockStyle.Fill;
            txtReason.MaxLength = 500;
            txtReason.Multiline = true;
            txtReason.ScrollBars = ScrollBars.Vertical;
            txtReason.Height = 88;
            txtReason.Margin = new Padding(0, 0, 0, 12);
            lblEffects.AutoSize = true;
            lblEffects.Margin = new Padding(0, 0, 0, 8);
            lblEffects.MaximumSize = new Size(480, 0);
            lblEffects.Text = "The parcel returns to Available.";
            lblEffects.UseMnemonic = false;
            lblError.AutoSize = true;
            lblError.Margin = new Padding(0, 0, 0, 8);
            lblError.MaximumSize = new Size(480, 0);
            lblError.Visible = false;
            lblError.UseMnemonic = false;
            buttons.AutoSize = true;
            buttons.Controls.Add(btnSave);
            buttons.Controls.Add(btnCancel);
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            btnSave.AutoSize = true;
            btnSave.Margin = new Padding(8, 0, 0, 0);
            btnSave.Text = "Terminate";
            btnSave.UseVisualStyleBackColor = false;
            btnCancel.AutoSize = true;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Margin = new Padding(0);
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = false;
            AcceptButton = btnSave;
            CancelButton = btnCancel;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(560, 610);
            Controls.Add(layout);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            layout.ResumeLayout(false);
            layout.PerformLayout();
            outcomeGroup.ResumeLayout(false);
            outcomeLayout.ResumeLayout(false);
            outcomeLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudRefundAmount).EndInit();
            buttons.ResumeLayout(false);
            buttons.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel layout;
        private Label lblTitle;
        private Label lblPaidTotal;
        private GroupBox outcomeGroup;
        private FlowLayoutPanel outcomeLayout;
        private RadioButton rdoRefunded;
        private RadioButton rdoPartlyRefunded;
        private RadioButton rdoKept;
        private Label lblRefundAmount;
        private NumericUpDown nudRefundAmount;
        private Label lblReason;
        private TextBox txtReason;
        private Label lblEffects;
        private Label lblError;
        private FlowLayoutPanel buttons;
        private Button btnSave;
        private Button btnCancel;
    }
}

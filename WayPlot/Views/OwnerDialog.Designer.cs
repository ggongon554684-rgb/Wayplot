#nullable disable
namespace WayPlot.Views
{
    partial class OwnerDialog
    {
        private System.ComponentModel.IContainer components = null;
        private TableLayoutPanel layout;
        private Label lblTitle;
        private Label lblName;
        private TextBox txtName;
        private Label lblPhone;
        private TextBox txtPhone;
        private Label lblEmail;
        private TextBox txtEmail;
        private CheckBox chkListingAgreement;
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
            lblName = new Label();
            txtName = new TextBox();
            lblPhone = new Label();
            txtPhone = new TextBox();
            lblEmail = new Label();
            txtEmail = new TextBox();
            chkListingAgreement = new CheckBox();
            lblError = new Label();
            buttons = new FlowLayoutPanel();
            btnSave = new Button();
            btnCancel = new Button();
            layout.SuspendLayout();
            buttons.SuspendLayout();
            SuspendLayout();
            layout.ColumnCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(lblTitle, 0, 0);
            layout.SetColumnSpan(lblTitle, 2);
            layout.Controls.Add(lblName, 0, 1);
            layout.Controls.Add(txtName, 1, 1);
            layout.Controls.Add(lblPhone, 0, 2);
            layout.Controls.Add(txtPhone, 1, 2);
            layout.Controls.Add(lblEmail, 0, 3);
            layout.Controls.Add(txtEmail, 1, 3);
            layout.Controls.Add(chkListingAgreement, 0, 4);
            layout.SetColumnSpan(chkListingAgreement, 2);
            layout.Controls.Add(lblError, 0, 5);
            layout.SetColumnSpan(lblError, 2);
            layout.Controls.Add(buttons, 0, 6);
            layout.SetColumnSpan(buttons, 2);
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(20);
            layout.RowCount = 7;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            lblTitle.AutoSize = true;
            lblTitle.Margin = new Padding(0, 0, 0, 12);
            lblTitle.Text = "New owner";
            lblTitle.UseMnemonic = false;
            lblName.Anchor = AnchorStyles.Left;
            lblName.AutoSize = true;
            lblName.Margin = new Padding(0, 0, 12, 8);
            lblName.Text = "Name";
            lblName.UseMnemonic = false;
            txtName.Dock = DockStyle.Fill;
            txtName.Margin = new Padding(0, 0, 0, 8);
            lblPhone.Anchor = AnchorStyles.Left;
            lblPhone.AutoSize = true;
            lblPhone.Margin = new Padding(0, 0, 12, 8);
            lblPhone.Text = "Phone";
            lblPhone.UseMnemonic = false;
            txtPhone.Dock = DockStyle.Fill;
            txtPhone.Margin = new Padding(0, 0, 0, 8);
            lblEmail.Anchor = AnchorStyles.Left;
            lblEmail.AutoSize = true;
            lblEmail.Margin = new Padding(0, 0, 12, 8);
            lblEmail.Text = "Email";
            lblEmail.UseMnemonic = false;
            txtEmail.Dock = DockStyle.Fill;
            txtEmail.Margin = new Padding(0, 0, 0, 8);
            chkListingAgreement.AutoSize = true;
            chkListingAgreement.Text = "Listing agreement on file";
            chkListingAgreement.UseVisualStyleBackColor = true;
            chkListingAgreement.UseMnemonic = false;
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
            btnSave.Text = "Save owner";
            btnSave.UseVisualStyleBackColor = false;
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            CancelButton = btnCancel;
            ClientSize = new Size(480, 320);
            Controls.Add(layout);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "OwnerDialog";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Owner";
            layout.ResumeLayout(false);
            layout.PerformLayout();
            buttons.ResumeLayout(false);
            buttons.PerformLayout();
            ResumeLayout(false);
        }
    }
}

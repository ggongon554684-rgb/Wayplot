#nullable disable
namespace WayPlot.Views
{
    partial class CompanyProfileDialog
    {
        private System.ComponentModel.IContainer components = null;
        private TableLayoutPanel layout;
        private Label lblTitle;
        private Label lblVersion;
        private Label lblName;
        private TextBox txtName;
        private Label lblContactNumbers;
        private TextBox txtContactNumbers;
        private Label lblEmail;
        private TextBox txtEmail;
        private Label lblAddress;
        private TextBox txtAddress;
        private Label lblError;
        private FlowLayoutPanel buttons;
        private Button btnSave;
        private Button btnCancel;
        private ToolTip toolTip;

        protected override void Dispose(bool disposing)
        {
            if (disposing) components?.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            layout = new TableLayoutPanel();
            lblTitle = new Label();
            lblVersion = new Label();
            lblName = new Label();
            txtName = new TextBox();
            lblContactNumbers = new Label();
            txtContactNumbers = new TextBox();
            lblEmail = new Label();
            txtEmail = new TextBox();
            lblAddress = new Label();
            txtAddress = new TextBox();
            lblError = new Label();
            buttons = new FlowLayoutPanel();
            btnSave = new Button();
            btnCancel = new Button();
            toolTip = new ToolTip(components);
            layout.SuspendLayout();
            buttons.SuspendLayout();
            SuspendLayout();
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(lblTitle, 0, 0);
            layout.Controls.Add(lblVersion, 0, 1);
            layout.Controls.Add(lblName, 0, 2);
            layout.Controls.Add(txtName, 0, 3);
            layout.Controls.Add(lblContactNumbers, 0, 4);
            layout.Controls.Add(txtContactNumbers, 0, 5);
            layout.Controls.Add(lblEmail, 0, 6);
            layout.Controls.Add(txtEmail, 0, 7);
            layout.Controls.Add(lblAddress, 0, 8);
            layout.Controls.Add(txtAddress, 0, 9);
            layout.Controls.Add(lblError, 0, 10);
            layout.Controls.Add(buttons, 0, 11);
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(24);
            layout.RowCount = 12;
            for (var index = 0; index < 12; index++)
                layout.RowStyles.Add(new RowStyle(index == 9 ? SizeType.Percent : SizeType.AutoSize, index == 9 ? 100F : 0F));
            foreach (var label in new[] { lblTitle, lblVersion, lblName, lblContactNumbers, lblEmail, lblAddress, lblError })
                label.AutoSize = true;
            lblTitle.Text = "Company profile";
            lblTitle.Margin = new Padding(0, 0, 0, 8);
            lblVersion.Margin = new Padding(0, 0, 0, 18);
            lblName.Text = "Company name";
            lblName.Margin = new Padding(0, 0, 0, 4);
            lblContactNumbers.Text = "Contact numbers";
            lblContactNumbers.Margin = new Padding(0, 10, 0, 4);
            lblEmail.Text = "Email";
            lblEmail.Margin = new Padding(0, 10, 0, 4);
            lblAddress.Text = "Address shown on documents";
            lblAddress.Margin = new Padding(0, 10, 0, 4);
            foreach (var textBox in new[] { txtName, txtContactNumbers, txtEmail })
            {
                textBox.Dock = DockStyle.Top;
                textBox.MaxLength = 500;
            }
            txtName.MaxLength = 150;
            txtContactNumbers.MaxLength = 200;
            txtEmail.MaxLength = 254;
            txtAddress.Dock = DockStyle.Fill;
            txtAddress.Multiline = true;
            txtAddress.MaxLength = 500;
            txtAddress.ScrollBars = ScrollBars.Vertical;
            lblError.Visible = false;
            lblError.MaximumSize = new Size(600, 0);
            lblError.Margin = new Padding(0, 10, 0, 8);
            buttons.AutoSize = true;
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Controls.Add(btnSave);
            buttons.Controls.Add(btnCancel);
            btnSave.AutoSize = true;
            btnSave.Text = "Save profile";
            btnSave.UseVisualStyleBackColor = false;
            btnCancel.AutoSize = true;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = false;
            AcceptButton = btnSave;
            CancelButton = btnCancel;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(620, 620);
            Controls.Add(layout);
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(560, 560);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "CompanyProfileDialog";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Company profile";
            layout.ResumeLayout(false);
            layout.PerformLayout();
            buttons.ResumeLayout(false);
            buttons.PerformLayout();
            ResumeLayout(false);
        }
    }
}

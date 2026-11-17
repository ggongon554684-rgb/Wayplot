#nullable disable
namespace WayPlot.Views
{
    partial class ContactDialog
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
            lblName = new Label();
            txtName = new TextBox();
            lblPhone = new Label();
            txtPhone = new TextBox();
            lblEmail = new Label();
            txtEmail = new TextBox();
            pairLayout = new TableLayoutPanel();
            lblSource = new Label();
            lblType = new Label();
            cboSource = new ComboBox();
            cboType = new ComboBox();
            lblReferrerType = new Label();
            cboReferrerType = new ComboBox();
            lblReferrerChoice = new Label();
            cboReferrerChoice = new ComboBox();
            lblReferrerName = new Label();
            txtReferrerName = new TextBox();
            lblReferrerPhone = new Label();
            txtReferrerPhone = new TextBox();
            chkSms = new CheckBox();
            chkEmail = new CheckBox();
            lblNote = new Label();
            txtNote = new TextBox();
            lblError = new Label();
            buttons = new FlowLayoutPanel();
            btnSave = new Button();
            btnCancel = new Button();
            layout.SuspendLayout();
            pairLayout.SuspendLayout();
            buttons.SuspendLayout();
            SuspendLayout();
            // 
            // layout
            // 
            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(lblName, 0, 0);
            layout.Controls.Add(txtName, 0, 1);
            layout.Controls.Add(lblPhone, 0, 2);
            layout.Controls.Add(txtPhone, 0, 3);
            layout.Controls.Add(lblEmail, 0, 4);
            layout.Controls.Add(txtEmail, 0, 5);
            layout.Controls.Add(pairLayout, 0, 6);
            layout.Controls.Add(lblReferrerType, 0, 7);
            layout.Controls.Add(cboReferrerType, 0, 8);
            layout.Controls.Add(lblReferrerChoice, 0, 9);
            layout.Controls.Add(cboReferrerChoice, 0, 10);
            layout.Controls.Add(lblReferrerName, 0, 11);
            layout.Controls.Add(txtReferrerName, 0, 12);
            layout.Controls.Add(lblReferrerPhone, 0, 13);
            layout.Controls.Add(txtReferrerPhone, 0, 14);
            layout.Controls.Add(chkSms, 0, 15);
            layout.Controls.Add(chkEmail, 0, 16);
            layout.Controls.Add(lblNote, 0, 17);
            layout.Controls.Add(txtNote, 0, 18);
            layout.Controls.Add(lblError, 0, 19);
            layout.Controls.Add(buttons, 0, 20);
            layout.Dock = DockStyle.Fill;
            layout.Name = "layout";
            layout.Padding = new Padding(24);
            layout.RowCount = 21;
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
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
            layout.TabIndex = 0;
            // 
            // lblName
            // 
            lblName.Anchor = AnchorStyles.Left;
            lblName.AutoSize = true;
            lblName.Margin = new Padding(0, 0, 0, 4);
            lblName.Name = "lblName";
            lblName.Text = "Full name";
            lblName.UseMnemonic = false;
            // 
            // txtName
            // 
            txtName.Dock = DockStyle.Fill;
            txtName.Margin = new Padding(0, 0, 0, 12);
            txtName.MaxLength = 150;
            txtName.Name = "txtName";
            txtName.TabIndex = 1;
            // 
            // lblPhone
            // 
            lblPhone.Anchor = AnchorStyles.Left;
            lblPhone.AutoSize = true;
            lblPhone.Margin = new Padding(0, 0, 0, 4);
            lblPhone.Name = "lblPhone";
            lblPhone.Text = "Mobile number (example: 0917 123 4567)";
            lblPhone.UseMnemonic = false;
            // 
            // txtPhone
            // 
            txtPhone.Dock = DockStyle.Fill;
            txtPhone.Margin = new Padding(0, 0, 0, 12);
            txtPhone.Name = "txtPhone";
            txtPhone.TabIndex = 2;
            // 
            // lblEmail
            // 
            lblEmail.Anchor = AnchorStyles.Left;
            lblEmail.AutoSize = true;
            lblEmail.Margin = new Padding(0, 0, 0, 4);
            lblEmail.Name = "lblEmail";
            lblEmail.Text = "Email";
            lblEmail.UseMnemonic = false;
            // 
            // txtEmail
            // 
            txtEmail.Dock = DockStyle.Fill;
            txtEmail.Margin = new Padding(0, 0, 0, 12);
            txtEmail.MaxLength = 200;
            txtEmail.Name = "txtEmail";
            txtEmail.TabIndex = 3;
            // 
            // pairLayout
            // 
            pairLayout.AutoSize = true;
            pairLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            pairLayout.ColumnCount = 2;
            pairLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pairLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            pairLayout.Controls.Add(lblSource, 0, 0);
            pairLayout.Controls.Add(lblType, 1, 0);
            pairLayout.Controls.Add(cboSource, 0, 1);
            pairLayout.Controls.Add(cboType, 1, 1);
            pairLayout.Dock = DockStyle.Fill;
            pairLayout.Margin = new Padding(0, 0, 0, 8);
            pairLayout.Name = "pairLayout";
            pairLayout.RowCount = 2;
            pairLayout.RowStyles.Add(new RowStyle());
            pairLayout.RowStyles.Add(new RowStyle());
            pairLayout.TabIndex = 4;
            // 
            // lblSource
            // 
            lblSource.Anchor = AnchorStyles.Left;
            lblSource.AutoSize = true;
            lblSource.Margin = new Padding(0, 0, 0, 4);
            lblSource.Name = "lblSource";
            lblSource.Text = "How they found you";
            lblSource.UseMnemonic = false;
            // 
            // lblType
            // 
            lblType.Anchor = AnchorStyles.Left;
            lblType.AutoSize = true;
            lblType.Margin = new Padding(12, 0, 0, 4);
            lblType.Name = "lblType";
            lblType.Text = "Contact type";
            lblType.UseMnemonic = false;
            // 
            // cboSource
            // 
            cboSource.Dock = DockStyle.Fill;
            cboSource.DropDownStyle = ComboBoxStyle.DropDownList;
            cboSource.FormattingEnabled = true;
            cboSource.Margin = new Padding(0, 0, 0, 4);
            cboSource.Name = "cboSource";
            cboSource.TabIndex = 0;
            // 
            // cboType
            // 
            cboType.Dock = DockStyle.Fill;
            cboType.DropDownStyle = ComboBoxStyle.DropDownList;
            cboType.FormattingEnabled = true;
            cboType.Margin = new Padding(12, 0, 0, 4);
            cboType.Name = "cboType";
            cboType.TabIndex = 1;
            //
            // lblReferrerType
            //
            lblReferrerType.Anchor = AnchorStyles.Left;
            lblReferrerType.AutoSize = true;
            lblReferrerType.Margin = new Padding(0, 0, 0, 4);
            lblReferrerType.Name = "lblReferrerType";
            lblReferrerType.Text = "Referred by (optional)";
            lblReferrerType.UseMnemonic = false;
            //
            // cboReferrerType
            //
            cboReferrerType.Dock = DockStyle.Fill;
            cboReferrerType.DropDownStyle = ComboBoxStyle.DropDownList;
            cboReferrerType.FormattingEnabled = true;
            cboReferrerType.Margin = new Padding(0, 0, 0, 8);
            cboReferrerType.Name = "cboReferrerType";
            //
            // lblReferrerChoice
            //
            lblReferrerChoice.Anchor = AnchorStyles.Left;
            lblReferrerChoice.AutoSize = true;
            lblReferrerChoice.Margin = new Padding(0, 0, 0, 4);
            lblReferrerChoice.Name = "lblReferrerChoice";
            lblReferrerChoice.Text = "Choose referrer";
            lblReferrerChoice.UseMnemonic = false;
            lblReferrerChoice.Visible = false;
            //
            // cboReferrerChoice
            //
            cboReferrerChoice.Dock = DockStyle.Fill;
            cboReferrerChoice.DropDownStyle = ComboBoxStyle.DropDownList;
            cboReferrerChoice.FormattingEnabled = true;
            cboReferrerChoice.Margin = new Padding(0, 0, 0, 8);
            cboReferrerChoice.Name = "cboReferrerChoice";
            cboReferrerChoice.Visible = false;
            //
            // lblReferrerName
            //
            lblReferrerName.Anchor = AnchorStyles.Left;
            lblReferrerName.AutoSize = true;
            lblReferrerName.Margin = new Padding(0, 0, 0, 4);
            lblReferrerName.Name = "lblReferrerName";
            lblReferrerName.Text = "Outside referrer's name";
            lblReferrerName.UseMnemonic = false;
            lblReferrerName.Visible = false;
            //
            // txtReferrerName
            //
            txtReferrerName.Dock = DockStyle.Fill;
            txtReferrerName.Margin = new Padding(0, 0, 0, 8);
            txtReferrerName.MaxLength = 150;
            txtReferrerName.Name = "txtReferrerName";
            txtReferrerName.Visible = false;
            //
            // lblReferrerPhone
            //
            lblReferrerPhone.Anchor = AnchorStyles.Left;
            lblReferrerPhone.AutoSize = true;
            lblReferrerPhone.Margin = new Padding(0, 0, 0, 4);
            lblReferrerPhone.Name = "lblReferrerPhone";
            lblReferrerPhone.Text = "Outside referrer's mobile number";
            lblReferrerPhone.UseMnemonic = false;
            lblReferrerPhone.Visible = false;
            //
            // txtReferrerPhone
            //
            txtReferrerPhone.Dock = DockStyle.Fill;
            txtReferrerPhone.Margin = new Padding(0, 0, 0, 12);
            txtReferrerPhone.Name = "txtReferrerPhone";
            txtReferrerPhone.Visible = false;
            //
            // chkSms
            //
            chkSms.AutoSize = true;
            chkSms.Margin = new Padding(0, 0, 0, 4);
            chkSms.Name = "chkSms";
            chkSms.TabIndex = 5;
            chkSms.Text = "OK to send SMS (needs a mobile number)";
            chkSms.UseVisualStyleBackColor = true;
            //
            // chkEmail
            //
            chkEmail.AutoSize = true;
            chkEmail.Margin = new Padding(0, 0, 0, 12);
            chkEmail.Name = "chkEmail";
            chkEmail.TabIndex = 6;
            chkEmail.Text = "OK to send email (needs an email address)";
            chkEmail.UseVisualStyleBackColor = true;
            //
            // lblNote
            //
            lblNote.Anchor = AnchorStyles.Left;
            lblNote.AutoSize = true;
            lblNote.Margin = new Padding(0, 0, 0, 4);
            lblNote.Name = "lblNote";
            lblNote.Text = "Note (kept only if this person already exists)";
            lblNote.UseMnemonic = false;
            //
            // txtNote
            //
            txtNote.Dock = DockStyle.Fill;
            txtNote.Margin = new Padding(0, 0, 0, 12);
            txtNote.MinimumSize = new Size(0, 72);
            txtNote.Multiline = true;
            txtNote.Name = "txtNote";
            txtNote.ScrollBars = ScrollBars.Vertical;
            txtNote.Size = new Size(392, 72);
            txtNote.TabIndex = 7;
            //
            // lblError
            //
            lblError.AutoSize = true;
            lblError.Margin = new Padding(0, 0, 0, 8);
            lblError.MaximumSize = new Size(392, 0);
            lblError.Name = "lblError";
            lblError.Visible = false;
            lblError.UseMnemonic = false;
            //
            // buttons
            //
            buttons.AutoSize = true;
            buttons.Controls.Add(btnSave);
            buttons.Controls.Add(btnCancel);
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Margin = new Padding(0);
            buttons.Name = "buttons";
            buttons.TabIndex = 8;
            //
            // btnSave
            //
            btnSave.AutoSize = true;
            btnSave.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnSave.Margin = new Padding(8, 0, 0, 0);
            btnSave.Name = "btnSave";
            btnSave.TabIndex = 0;
            btnSave.Text = "Save contact";
            btnSave.UseVisualStyleBackColor = false;
            // 
            // btnCancel
            // 
            btnCancel.AutoSize = true;
            btnCancel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Margin = new Padding(0);
            btnCancel.Name = "btnCancel";
            btnCancel.TabIndex = 1;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = false;
            // 
            // ContactDialog
            // 
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            CancelButton = btnCancel;
            ClientSize = new Size(440, 760);
            Controls.Add(layout);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ContactDialog";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "New contact";
            layout.ResumeLayout(false);
            layout.PerformLayout();
            pairLayout.ResumeLayout(false);
            pairLayout.PerformLayout();
            buttons.ResumeLayout(false);
            buttons.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel layout;
        private Label lblName;
        private TextBox txtName;
        private Label lblPhone;
        private TextBox txtPhone;
        private Label lblEmail;
        private TextBox txtEmail;
        private TableLayoutPanel pairLayout;
        private Label lblSource;
        private Label lblType;
        private ComboBox cboSource;
        private ComboBox cboType;
        private Label lblReferrerType;
        private ComboBox cboReferrerType;
        private Label lblReferrerChoice;
        private ComboBox cboReferrerChoice;
        private Label lblReferrerName;
        private TextBox txtReferrerName;
        private Label lblReferrerPhone;
        private TextBox txtReferrerPhone;
        private CheckBox chkSms;
        private CheckBox chkEmail;
        private Label lblNote;
        private TextBox txtNote;
        private Label lblError;
        private FlowLayoutPanel buttons;
        private Button btnSave;
        private Button btnCancel;
    }
}
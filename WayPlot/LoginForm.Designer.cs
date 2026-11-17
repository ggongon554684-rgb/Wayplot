#nullable disable

namespace WayPlot
{
    partial class LoginForm
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

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            lblTitle = new Label();
            txtUser = new TextBox();
            txtPass = new TextBox();
            btnSignIn = new Button();
            lblMessage = new Label();
            SuspendLayout();
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            lblTitle.Location = new Point(32, 28);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(117, 37);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Sign in";
            // 
            // txtUser
            // 
            txtUser.Location = new Point(32, 100);
            txtUser.Name = "txtUser";
            txtUser.PlaceholderText = "Username";
            txtUser.Size = new Size(296, 29);
            txtUser.TabIndex = 1;
            // 
            // txtPass
            // 
            txtPass.Location = new Point(32, 148);
            txtPass.Name = "txtPass";
            txtPass.PlaceholderText = "Password";
            txtPass.Size = new Size(296, 29);
            txtPass.TabIndex = 2;
            txtPass.UseSystemPasswordChar = true;
            // 
            // btnSignIn
            // 
            btnSignIn.Location = new Point(32, 200);
            btnSignIn.Name = "btnSignIn";
            btnSignIn.Size = new Size(296, 40);
            btnSignIn.TabIndex = 3;
            btnSignIn.Text = "Sign in";
            btnSignIn.UseVisualStyleBackColor = true;
            btnSignIn.Click += btnSignIn_Click;
            // 
            // lblMessage
            // 
            lblMessage.Location = new Point(32, 256);
            lblMessage.Name = "lblMessage";
            lblMessage.Size = new Size(296, 80);
            lblMessage.TabIndex = 4;
            // 
            // LoginForm
            // 
            AcceptButton = btnSignIn;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.White;
            ClientSize = new Size(360, 360);
            Controls.Add(lblTitle);
            Controls.Add(txtUser);
            Controls.Add(txtPass);
            Controls.Add(btnSignIn);
            Controls.Add(lblMessage);
            Font = new Font("Segoe UI", 10F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Name = "LoginForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "WayPlot CRM";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblTitle;
        private TextBox txtUser;
        private TextBox txtPass;
        private Button btnSignIn;
        private Label lblMessage;
    }
}
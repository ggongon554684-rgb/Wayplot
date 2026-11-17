#nullable disable

namespace WayPlot.Views
{
    partial class MainShell
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
            layoutRoot = new TableLayoutPanel();
            pnlHeader = new Panel();
            lblWorkspace = new Label();
            lblAppName = new Label();
            tabBar = new WayPlot.UI.PillTabBar();
            pnlContent = new Panel();
            layoutRoot.SuspendLayout();
            pnlHeader.SuspendLayout();
            SuspendLayout();
            // 
            // layoutRoot
            // 
            layoutRoot.ColumnCount = 1;
            layoutRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layoutRoot.Controls.Add(pnlHeader, 0, 0);
            layoutRoot.Controls.Add(tabBar, 0, 1);
            layoutRoot.Controls.Add(pnlContent, 0, 2);
            layoutRoot.Dock = DockStyle.Fill;
            layoutRoot.Location = new Point(28, 12);
            layoutRoot.Name = "layoutRoot";
            layoutRoot.RowCount = 3;
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            layoutRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layoutRoot.Size = new Size(1104, 624);
            layoutRoot.TabIndex = 0;
            // 
            // pnlHeader
            // 
            pnlHeader.Controls.Add(lblWorkspace);
            pnlHeader.Controls.Add(lblAppName);
            pnlHeader.Dock = DockStyle.Fill;
            pnlHeader.Location = new Point(0, 0);
            pnlHeader.Margin = new Padding(0);
            pnlHeader.Name = "pnlHeader";
            pnlHeader.Size = new Size(1104, 64);
            pnlHeader.TabIndex = 0;
            // 
            // lblWorkspace
            // 
            lblWorkspace.AutoSize = true;
            lblWorkspace.Dock = DockStyle.Right;
            lblWorkspace.Location = new Point(1012, 0);
            lblWorkspace.Name = "lblWorkspace";
            lblWorkspace.Size = new Size(92, 23);
            lblWorkspace.TabIndex = 1;
            lblWorkspace.Text = "Workspace";
            lblWorkspace.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblAppName
            // 
            lblAppName.AutoSize = true;
            lblAppName.Dock = DockStyle.Left;
            lblAppName.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
            lblAppName.Location = new Point(0, 0);
            lblAppName.Name = "lblAppName";
            lblAppName.Size = new Size(154, 46);
            lblAppName.TabIndex = 0;
            lblAppName.Text = "WayPlot";
            lblAppName.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // tabBar
            // 
            tabBar.BackColor = Color.FromArgb(255, 255, 255);
            tabBar.Dock = DockStyle.Fill;
            tabBar.Font = new Font("Segoe UI", 10F);
            tabBar.Location = new Point(0, 64);
            tabBar.Margin = new Padding(0);
            tabBar.Name = "tabBar";
            tabBar.Size = new Size(1104, 56);
            tabBar.TabIndex = 1;
            tabBar.TabStop = false;
            // 
            // pnlContent
            // 
            pnlContent.BackColor = Color.White;
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.Location = new Point(0, 120);
            pnlContent.Margin = new Padding(0);
            pnlContent.Name = "pnlContent";
            pnlContent.Padding = new Padding(0, 16, 0, 0);
            pnlContent.Size = new Size(1104, 504);
            pnlContent.TabIndex = 2;
            // 
            // MainShell
            // 
            AutoScaleDimensions = new SizeF(120F, 120F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.White;
            ClientSize = new Size(1160, 660);
            Controls.Add(layoutRoot);
            Font = new Font("Segoe UI", 10F);
            MinimumSize = new Size(1100, 640);
            Name = "MainShell";
            Padding = new Padding(28, 12, 28, 24);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "WayPlot CRM";
            layoutRoot.ResumeLayout(false);
            pnlHeader.ResumeLayout(false);
            pnlHeader.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel layoutRoot;
        private Panel pnlHeader;
        private Label lblWorkspace;
        private Label lblAppName;
        private WayPlot.UI.PillTabBar tabBar;
        private Panel pnlContent;
    }
}
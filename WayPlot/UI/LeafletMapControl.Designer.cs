#nullable disable
namespace WayPlot.UI
{
    partial class LeafletMapControl
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
            webView = new Microsoft.Web.WebView2.WinForms.WebView2();
            btnFitAll = new Button();
            lblUnavailable = new Label();
            ((System.ComponentModel.ISupportInitialize)webView).BeginInit();
            SuspendLayout();
            webView.AllowExternalDrop = false;
            webView.CreationProperties = null;
            webView.DefaultBackgroundColor = Color.FromArgb(235, 240, 244);
            webView.Dock = DockStyle.Fill;
            webView.Location = new Point(0, 0);
            webView.Name = "webView";
            webView.Size = new Size(800, 600);
            webView.TabIndex = 0;
            webView.ZoomFactor = 1D;
            btnFitAll.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnFitAll.Location = new Point(680, 12);
            btnFitAll.Name = "btnFitAll";
            btnFitAll.Size = new Size(108, 40);
            btnFitAll.TabIndex = 1;
            btnFitAll.Text = "Fit parcels";
            btnFitAll.UseVisualStyleBackColor = false;
            lblUnavailable.Dock = DockStyle.Fill;
            lblUnavailable.Location = new Point(0, 0);
            lblUnavailable.Name = "lblUnavailable";
            lblUnavailable.Padding = new Padding(30);
            lblUnavailable.Size = new Size(800, 600);
            lblUnavailable.TabIndex = 2;
            lblUnavailable.Text = "The parcel map is unavailable.";
            lblUnavailable.TextAlign = ContentAlignment.MiddleCenter;
            lblUnavailable.UseMnemonic = false;
            lblUnavailable.Visible = false;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Controls.Add(webView);
            Controls.Add(btnFitAll);
            Controls.Add(lblUnavailable);
            Name = "LeafletMapControl";
            Size = new Size(800, 600);
            ((System.ComponentModel.ISupportInitialize)webView).EndInit();
            ResumeLayout(false);
        }

        private Microsoft.Web.WebView2.WinForms.WebView2 webView;
        private Button btnFitAll;
        private Label lblUnavailable;
    }
}

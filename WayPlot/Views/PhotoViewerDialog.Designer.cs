#nullable enable
namespace WayPlot.Views;

partial class PhotoViewerDialog
{
    private System.ComponentModel.IContainer? components = null;
    private TableLayoutPanel layout = null!;
    private PictureBox picturePhoto = null!;
    private Label lblFileName = null!;
    private Label lblStatus = null!;
    private ProgressBar progressPhoto = null!;
    private FlowLayoutPanel buttons = null!;
    private Button btnPrevious = null!;
    private Button btnNext = null!;
    private Button btnClose = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisposeViewerImage();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        layout = new TableLayoutPanel();
        picturePhoto = new PictureBox();
        lblFileName = new Label();
        lblStatus = new Label();
        progressPhoto = new ProgressBar();
        buttons = new FlowLayoutPanel();
        btnPrevious = new Button();
        btnNext = new Button();
        btnClose = new Button();
        layout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)picturePhoto).BeginInit();
        buttons.SuspendLayout();
        SuspendLayout();
        layout.ColumnCount = 1;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.Controls.Add(lblFileName, 0, 0);
        layout.Controls.Add(picturePhoto, 0, 1);
        layout.Controls.Add(lblStatus, 0, 2);
        layout.Controls.Add(progressPhoto, 0, 3);
        layout.Controls.Add(buttons, 0, 4);
        layout.Dock = DockStyle.Fill;
        layout.Name = "layout";
        layout.Padding = new Padding(16);
        layout.RowCount = 5;
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        lblFileName.AutoEllipsis = true;
        lblFileName.Dock = DockStyle.Fill;
        lblFileName.Margin = new Padding(0, 0, 0, 8);
        lblFileName.Name = "lblFileName";
        lblFileName.Text = "Photo";
        lblFileName.UseMnemonic = false;
        picturePhoto.Dock = DockStyle.Fill;
        picturePhoto.Name = "picturePhoto";
        picturePhoto.SizeMode = PictureBoxSizeMode.Zoom;
        lblStatus.AutoSize = true;
        lblStatus.Dock = DockStyle.Fill;
        lblStatus.Margin = new Padding(0, 6, 0, 6);
        lblStatus.Name = "lblStatus";
        lblStatus.Text = "";
        lblStatus.UseMnemonic = false;
        progressPhoto.Dock = DockStyle.Fill;
        progressPhoto.MarqueeAnimationSpeed = 30;
        progressPhoto.Name = "progressPhoto";
        progressPhoto.Style = ProgressBarStyle.Marquee;
        progressPhoto.Visible = false;
        buttons.AutoSize = true;
        buttons.Controls.Add(btnPrevious);
        buttons.Controls.Add(btnNext);
        buttons.Controls.Add(btnClose);
        buttons.Dock = DockStyle.Fill;
        buttons.FlowDirection = FlowDirection.RightToLeft;
        buttons.Name = "buttons";
        buttons.WrapContents = false;
        btnPrevious.AutoSize = true;
        btnPrevious.Name = "btnPrevious";
        btnPrevious.Text = "Previous";
        btnPrevious.UseVisualStyleBackColor = false;
        btnNext.AutoSize = true;
        btnNext.Name = "btnNext";
        btnNext.Text = "Next";
        btnNext.UseVisualStyleBackColor = false;
        btnClose.AutoSize = true;
        btnClose.DialogResult = DialogResult.Cancel;
        btnClose.Name = "btnClose";
        btnClose.Text = "Close";
        btnClose.UseVisualStyleBackColor = false;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        CancelButton = btnClose;
        ClientSize = new Size(960, 720);
        Controls.Add(layout);
        MinimizeBox = false;
        Name = "PhotoViewerDialog";
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Parcel photo";
        layout.ResumeLayout(false);
        layout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)picturePhoto).EndInit();
        buttons.ResumeLayout(false);
        buttons.PerformLayout();
        ResumeLayout(false);
    }
}

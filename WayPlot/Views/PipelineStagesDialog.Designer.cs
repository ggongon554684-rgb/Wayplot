#nullable disable
namespace WayPlot.Views
{
    partial class PipelineStagesDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            layout = new TableLayoutPanel();
            lblTitle = new Label();
            lblInstructions = new Label();
            lblVersion = new Label();
            lblNewFixed = new Label();
            columnHeaders = new TableLayoutPanel();
            lblSlotHeader = new Label();
            lblActiveHeader = new Label();
            lblNameHeader = new Label();
            lblCountHeader = new Label();
            lblOrderHeader = new Label();
            flowStages = new FlowLayoutPanel();
            lblClosedFixed = new Label();
            lblLostFixed = new Label();
            lblStatus = new Label();
            buttons = new FlowLayoutPanel();
            btnSave = new Button();
            btnCancel = new Button();
            layout.SuspendLayout();
            columnHeaders.SuspendLayout();
            buttons.SuspendLayout();
            SuspendLayout();

            layout.ColumnCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.Controls.Add(lblTitle, 0, 0);
            layout.Controls.Add(lblInstructions, 0, 1);
            layout.Controls.Add(lblVersion, 0, 2);
            layout.Controls.Add(lblNewFixed, 0, 3);
            layout.Controls.Add(columnHeaders, 0, 4);
            layout.Controls.Add(flowStages, 0, 5);
            layout.Controls.Add(lblClosedFixed, 0, 6);
            layout.Controls.Add(lblLostFixed, 0, 7);
            layout.Controls.Add(lblStatus, 0, 8);
            layout.Controls.Add(buttons, 0, 9);
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(24);
            layout.RowCount = 10;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            lblTitle.AutoSize = true;
            lblTitle.Margin = new Padding(0, 0, 0, 8);
            lblTitle.Text = "Configure pipeline stages";
            lblTitle.UseMnemonic = false;
            lblInstructions.AutoSize = true;
            lblInstructions.Margin = new Padding(0, 0, 0, 8);
            lblInstructions.Text = "New, Closed and Lost are fixed. Keep at least one configurable stage active.";
            lblInstructions.UseMnemonic = false;
            lblVersion.AutoSize = true;
            lblVersion.Margin = new Padding(0, 0, 0, 8);
            lblNewFixed.AutoSize = true;
            lblNewFixed.Text = "New (fixed; always first)";
            lblNewFixed.Margin = new Padding(0, 0, 0, 6);

            columnHeaders.ColumnCount = 5;
            columnHeaders.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            columnHeaders.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F));
            columnHeaders.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
            columnHeaders.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
            columnHeaders.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
            columnHeaders.Controls.Add(lblSlotHeader, 0, 0);
            columnHeaders.Controls.Add(lblActiveHeader, 1, 0);
            columnHeaders.Controls.Add(lblNameHeader, 2, 0);
            columnHeaders.Controls.Add(lblCountHeader, 3, 0);
            columnHeaders.Controls.Add(lblOrderHeader, 4, 0);
            columnHeaders.Dock = DockStyle.Fill;
            foreach (var label in new[] { lblSlotHeader, lblActiveHeader, lblNameHeader, lblCountHeader, lblOrderHeader })
            {
                label.AutoSize = true;
                label.Margin = new Padding(0, 0, 0, 4);
            }
            lblSlotHeader.Text = "Slot";
            lblActiveHeader.Text = "Active";
            lblNameHeader.Text = "Display name";
            lblCountHeader.Text = "Open leads";
            lblOrderHeader.Text = "Order";

            flowStages.AutoScroll = true;
            flowStages.Dock = DockStyle.Fill;
            flowStages.FlowDirection = FlowDirection.TopDown;
            flowStages.WrapContents = false;
            flowStages.TabIndex = 0;
            lblClosedFixed.AutoSize = true;
            lblClosedFixed.Text = "Closed (fixed; always before Lost)";
            lblClosedFixed.Margin = new Padding(0, 6, 0, 2);
            lblLostFixed.AutoSize = true;
            lblLostFixed.Text = "Lost (fixed; always last)";
            lblLostFixed.Margin = new Padding(0, 0, 0, 6);
            lblStatus.AutoSize = true;
            lblStatus.Margin = new Padding(0, 0, 0, 8);

            buttons.AutoSize = true;
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Controls.Add(btnCancel);
            buttons.Controls.Add(btnSave);
            btnSave.AutoSize = true;
            btnSave.Text = "Save";
            btnSave.TabIndex = 1;
            btnCancel.AutoSize = true;
            btnCancel.Text = "Cancel";
            btnCancel.TabIndex = 2;

            Controls.Add(layout);
            AcceptButton = btnSave;
            CancelButton = btnCancel;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(800, 560);
            Name = "PipelineStagesDialog";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Configure pipeline stages";
            Size = new Size(1000, 720);
            layout.ResumeLayout(false);
            layout.PerformLayout();
            columnHeaders.ResumeLayout(false);
            columnHeaders.PerformLayout();
            buttons.ResumeLayout(false);
            buttons.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel layout;
        private Label lblTitle;
        private Label lblInstructions;
        private Label lblVersion;
        private Label lblNewFixed;
        private TableLayoutPanel columnHeaders;
        private Label lblSlotHeader;
        private Label lblActiveHeader;
        private Label lblNameHeader;
        private Label lblCountHeader;
        private Label lblOrderHeader;
        private FlowLayoutPanel flowStages;
        private Label lblClosedFixed;
        private Label lblLostFixed;
        private Label lblStatus;
        private FlowLayoutPanel buttons;
        private Button btnSave;
        private Button btnCancel;
    }
}

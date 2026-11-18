#nullable disable
namespace WayPlot.Views
{
    partial class ReassignDialog
    {
        private System.ComponentModel.IContainer components = null;
        private TableLayoutPanel layout;
        private Label lblTitle;
        private Label lblAgent;
        private ComboBox cboAgent;
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
            lblAgent = new Label();
            cboAgent = new ComboBox();
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
            layout.Controls.Add(lblAgent, 0, 1);
            layout.Controls.Add(cboAgent, 1, 1);
            layout.Controls.Add(lblError, 0, 2);
            layout.SetColumnSpan(lblError, 2);
            layout.Controls.Add(buttons, 0, 3);
            layout.SetColumnSpan(buttons, 2);
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(20);
            layout.RowCount = 4;
            for (var i = 0; i < 4; i++)
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            lblTitle.AutoSize = true;
            lblTitle.Margin = new Padding(0, 0, 0, 12);
            lblTitle.Text = "Reassign parcel";
            lblTitle.UseMnemonic = false;
            lblAgent.Anchor = AnchorStyles.Left;
            lblAgent.AutoSize = true;
            lblAgent.Margin = new Padding(0, 0, 12, 8);
            lblAgent.Text = "Active Agent";
            lblAgent.UseMnemonic = false;
            cboAgent.Dock = DockStyle.Fill;
            cboAgent.DropDownStyle = ComboBoxStyle.DropDownList;
            cboAgent.Margin = new Padding(0, 0, 0, 8);
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
            btnSave.Text = "Reassign";
            btnSave.UseVisualStyleBackColor = false;
            AcceptButton = btnSave;
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            CancelButton = btnCancel;
            ClientSize = new Size(480, 220);
            Controls.Add(layout);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "ReassignDialog";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Reassign parcel";
            layout.ResumeLayout(false);
            layout.PerformLayout();
            buttons.ResumeLayout(false);
            buttons.PerformLayout();
            ResumeLayout(false);
        }
    }
}

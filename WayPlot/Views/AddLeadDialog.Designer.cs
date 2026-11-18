#nullable disable
using WayPlot.UI;

namespace WayPlot.Views
{
    partial class AddLeadDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            layout = new TableLayoutPanel();
            lblParcel = new Label();
            picker = new ParcelPickerControl();
            lblAgent = new Label();
            cboAgent = new ComboBox();
            lblNote = new Label();
            txtNote = new TextBox();
            buttons = new FlowLayoutPanel();
            btnSave = new Button();
            btnCancel = new Button();
            layout.SuspendLayout();
            buttons.SuspendLayout();
            SuspendLayout();

            layout.ColumnCount = 1;
            layout.Controls.Add(lblParcel, 0, 0);
            layout.Controls.Add(picker, 0, 1);
            layout.Controls.Add(lblAgent, 0, 2);
            layout.Controls.Add(cboAgent, 0, 3);
            layout.Controls.Add(lblNote, 0, 4);
            layout.Controls.Add(txtNote, 0, 5);
            layout.Controls.Add(buttons, 0, 6);
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(24);
            layout.RowCount = 7;
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 220F));
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle());
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
            layout.RowStyles.Add(new RowStyle());

            lblParcel.Text = "Available parcel";
            lblParcel.AutoSize = true;
            lblAgent.Text = "Assign to agent";
            lblAgent.AutoSize = true;
            lblAgent.Visible = false;
            cboAgent.DropDownStyle = ComboBoxStyle.DropDownList;
            cboAgent.Visible = false;
            cboAgent.DisplayMember = "DisplayName";
            lblNote.Text = "Note (optional)";
            lblNote.AutoSize = true;
            txtNote.Multiline = true;
            txtNote.ScrollBars = ScrollBars.Vertical;
            txtNote.Dock = DockStyle.Fill;

            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Controls.Add(btnSave);
            buttons.Controls.Add(btnCancel);
            btnSave.Text = "Add lead";
            btnCancel.Text = "Cancel";
            btnCancel.DialogResult = DialogResult.Cancel;

            AcceptButton = btnSave;
            CancelButton = btnCancel;
            ClientSize = new Size(520, 520);
            Controls.Add(layout);
            Text = "Add lead";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowIcon = false;

            layout.ResumeLayout(false);
            layout.PerformLayout();
            buttons.ResumeLayout(false);
            buttons.PerformLayout();
            ResumeLayout(false);
        }

        private TableLayoutPanel layout;
        private Label lblParcel;
        private ParcelPickerControl picker;
        private Label lblAgent;
        private ComboBox cboAgent;
        private Label lblNote;
        private TextBox txtNote;
        private FlowLayoutPanel buttons;
        private Button btnSave;
        private Button btnCancel;
    }
}

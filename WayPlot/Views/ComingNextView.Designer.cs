#nullable disable
namespace WayPlot.Views
{
    partial class ComingNextView
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

        #region Component Designer generated code

        private void InitializeComponent()
        {
            card = new WayPlot.UI.RoundedPanel();
            lblTitle = new Label();
            chip = new WayPlot.UI.StatusChip();
            lblBody = new Label();
            card.SuspendLayout();
            SuspendLayout();
            // 
            // card
            // 
            card.BackColor = Color.FromArgb(245, 247, 251);
            card.BorderColor = Color.FromArgb(227, 232, 242);
            card.Controls.Add(lblTitle);
            card.Controls.Add(chip);
            card.Controls.Add(lblBody);
            card.Location = new Point(21, 24);
            card.Margin = new Padding(3, 4, 3, 4);
            card.Name = "card";
            card.Padding = new Padding(27, 32, 27, 32);
            card.Radius = 24;
            card.Size = new Size(640, 227);
            card.TabIndex = 0;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblTitle.Location = new Point(27, 32);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(64, 32);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Title";
            // 
            // chip
            // 
            chip.BackColor = Color.Transparent;
            chip.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            chip.Kind = UI.ChipKind.Violet;
            chip.Location = new Point(27, 85);
            chip.Margin = new Padding(3, 4, 3, 4);
            chip.Name = "chip";
            chip.Size = new Size(129, 35);
            chip.TabIndex = 1;
            chip.TabStop = false;
            chip.Text = "Coming next";
            // 
            // lblBody
            // 
            lblBody.AutoSize = true;
            lblBody.Location = new Point(27, 139);
            lblBody.Name = "lblBody";
            lblBody.Size = new Size(339, 20);
            lblBody.TabIndex = 2;
            lblBody.Text = "This screen is not built yet. It arrives in a later step.";
            // 
            // ComingNextView
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            Controls.Add(card);
            Margin = new Padding(3, 4, 3, 4);
            Name = "ComingNextView";
            Size = new Size(686, 267);
            card.ResumeLayout(false);
            card.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private WayPlot.UI.RoundedPanel card;
        private Label lblTitle;
        private WayPlot.UI.StatusChip chip;
        private Label lblBody;
    }
}
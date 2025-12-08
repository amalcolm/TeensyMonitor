namespace TeensyMonitor
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            cbPorts = new ComboBox();
            labPorts = new Label();
            dbg = new TeensyMonitor.Plotter.UserControls.MyDebugPane();
            chart0 = new TeensyMonitor.Plotter.UserControls.MyChart();
            SuspendLayout();
            // 
            // cbPorts
            // 
            cbPorts.FormattingEnabled = true;
            cbPorts.Location = new Point(1050, 6);
            cbPorts.Name = "cbPorts";
            cbPorts.Size = new Size(121, 23);
            cbPorts.TabIndex = 3;
            cbPorts.SelectedIndexChanged += cbPorts_SelectedIndexChanged;
            // 
            // labPorts
            // 
            labPorts.AutoSize = true;
            labPorts.Location = new Point(981, 9);
            labPorts.Name = "labPorts";
            labPorts.Size = new Size(63, 15);
            labPorts.TabIndex = 4;
            labPorts.Text = "COM Port:";
            // 
            // dbg
            // 
            dbg.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dbg.AutoClear = true;
            dbg.BackColor = Color.WhiteSmoke;
            dbg.BorderStyle = BorderStyle.FixedSingle;
            dbg.Location = new Point(12, 585);
            dbg.Name = "dbg";
            dbg.Size = new Size(1160, 382);
            dbg.TabIndex = 5;
            // 
            // chart0
            // 
            chart0.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            chart0.AutoClear = true;
            chart0.BackColor = Color.PapayaWhip;
            chart0.BorderStyle = BorderStyle.FixedSingle;
            chart0.Location = new Point(12, 47);
            chart0.Name = "chart0";
            chart0.Size = new Size(1160, 525);
            chart0.TabIndex = 6;
            chart0.TimeWindowSeconds = 0.5F;
            chart0.Yscale = 1F;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1184, 979);
            Controls.Add(chart0);
            Controls.Add(dbg);
            Controls.Add(labPorts);
            Controls.Add(cbPorts);
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Form1";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            Shown += Form1_Shown;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private ComboBox cbPorts;
        private Label labPorts;
        private Plotter.UserControls.MyDebugPane dbg;
        private Plotter.UserControls.MyChart chart0;
    }
}

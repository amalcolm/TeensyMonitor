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
            chart = new TeensyMonitor.Plotter.UserControls.MyChart();
            SuspendLayout();
            // 
            // cbPorts
            // 
            cbPorts.FormattingEnabled = true;
            cbPorts.Location = new Point(84, 18);
            cbPorts.Name = "cbPorts";
            cbPorts.Size = new Size(121, 23);
            cbPorts.TabIndex = 3;
            cbPorts.SelectedIndexChanged += cbPorts_SelectedIndexChanged;
            // 
            // labPorts
            // 
            labPorts.AutoSize = true;
            labPorts.Location = new Point(15, 21);
            labPorts.Name = "labPorts";
            labPorts.Size = new Size(63, 15);
            labPorts.TabIndex = 4;
            labPorts.Text = "COM Port:";
            // 
            // dbg
            // 
            dbg.AutoClear = true;
            dbg.BackColor = Color.WhiteSmoke;
            dbg.BorderStyle = BorderStyle.FixedSingle;
            dbg.Location = new Point(12, 609);
            dbg.Name = "dbg";
            dbg.Size = new Size(1160, 351);
            dbg.TabIndex = 5;
            // 
            // chart
            // 
            chart.AutoClear = true;
            chart.BackColor = Color.PapayaWhip;
            chart.BorderStyle = BorderStyle.FixedSingle;
            chart.ChannelScale = 0.05F;
            chart.Location = new Point(12, 47);
            chart.Name = "chart";
            chart.Size = new Size(1160, 525);
            chart.TabIndex = 6;
            chart.TimeWindowSeconds = 5F;
            chart.Yscale = 0.01F;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1184, 972);
            Controls.Add(chart);
            Controls.Add(dbg);
            Controls.Add(labPorts);
            Controls.Add(cbPorts);
            Name = "Form1";
            Text = "Form1";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private ComboBox cbPorts;
        private Label labPorts;
        private Plotter.UserControls.MyDebugPane dbg;
        private Plotter.UserControls.MyChart chart;
    }
}

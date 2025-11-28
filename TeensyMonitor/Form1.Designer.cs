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
            myDebugPane1 = new TeensyMonitor.Plotter.UserControls.MyDebugPane();
            myChart1 = new TeensyMonitor.Plotter.UserControls.MyChart();
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
            // myDebugPane1
            // 
            myDebugPane1.BackColor = Color.WhiteSmoke;
            myDebugPane1.BorderStyle = BorderStyle.FixedSingle;
            myDebugPane1.Location = new Point(12, 609);
            myDebugPane1.Name = "myDebugPane1";
            myDebugPane1.Size = new Size(1160, 351);
            myDebugPane1.TabIndex = 5;
            // 
            // myChart1
            // 
            myChart1.BackColor = Color.PapayaWhip;
            myChart1.BorderStyle = BorderStyle.FixedSingle;
            myChart1.Location = new Point(12, 47);
            myChart1.Name = "myChart1";
            myChart1.Size = new Size(1160, 525);
            myChart1.TabIndex = 6;
            myChart1.TimeWindowSeconds = 10F;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1184, 972);
            Controls.Add(myChart1);
            Controls.Add(myDebugPane1);
            Controls.Add(labPorts);
            Controls.Add(cbPorts);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private ComboBox cbPorts;
        private Label labPorts;
        private Plotter.UserControls.MyDebugPane myDebugPane1;
        private Plotter.UserControls.MyChart myChart1;
    }
}

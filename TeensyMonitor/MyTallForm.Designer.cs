namespace TeensyMonitor.Plotter.UserControls
{
    partial class MyTallForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            chart = new MyChart();
            SuspendLayout();
            // 
            // chart
            // 
            chart.AllowPause = true;
            chart.AutoClear = true;
            chart.BackColor = Color.DarkSlateGray;
            chart.BorderStyle = BorderStyle.FixedSingle;
            chart.Dock = DockStyle.Fill;
            chart.EnableLabels = true;
            chart.EnablePlots = true;
            chart.Location = new Point(0, 0);
            chart.Name = "chart";
            chart.Size = new Size(800, 1235);
            chart.TabIndex = 0;
            chart.Yscale = 1F;
            chart.MouseDown += MyTallForm_MouseDown;
            chart.MouseMove += MyTallForm_MouseMove;
            chart.MouseUp += MyTallForm_MouseUp;
            // 
            // MyTallForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 1235);
            Controls.Add(chart);
            Location = new Point(3840, -400);
            Name = "MyTallForm";
            StartPosition = FormStartPosition.Manual;
            Text = "MyTallForm";
            WindowState = FormWindowState.Maximized;
            MouseDown += MyTallForm_MouseDown;
            MouseLeave += MyTallForm_MouseLeave;
            MouseMove += MyTallForm_MouseMove;
            MouseUp += MyTallForm_MouseUp;
            ResumeLayout(false);
        }

        #endregion

        private MyChart chart;
    }
}
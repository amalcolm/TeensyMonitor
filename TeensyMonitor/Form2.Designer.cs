namespace TeensyMonitor
{
    partial class Form2
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
            myChart1 = new TeensyMonitor.Plotter.UserControls.MyChart();
            SuspendLayout();
            // 
            // myChart1
            // 
            myChart1.BackColor = Color.PapayaWhip;
            myChart1.BorderStyle = BorderStyle.FixedSingle;
            myChart1.Location = new Point(12, 12);
            myChart1.Name = "myChart1";
            myChart1.Size = new Size(776, 426);
            myChart1.TabIndex = 0;
            myChart1.TimeWindowSeconds = 1000f;
            // 
            // Form2
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(myChart1);
            Name = "Form2";
            Text = "Form2";
            ResumeLayout(false);
        }

        #endregion

        private Plotter.UserControls.MyChart myChart1;
    }
}
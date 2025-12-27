namespace TeensyMonitor
{
    partial class MainForm
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
            myTelemetryPane1 = new TeensyMonitor.Plotter.UserControls.MyTelemetryPane();
            tlpCharts = new TableLayoutPanel();
            butDBG = new Button();
            tlpCharts.SuspendLayout();
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
            dbg.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            dbg.AutoClear = true;
            dbg.BackColor = Color.AliceBlue;
            dbg.BorderStyle = BorderStyle.FixedSingle;
            dbg.Location = new Point(5, 986);
            dbg.Name = "dbg";
            dbg.Size = new Size(1040, 306);
            dbg.TabIndex = 5;
            // 
            // chart0
            // 
            chart0.AutoClear = true;
            chart0.BackColor = Color.Cornsilk;
            chart0.BorderStyle = BorderStyle.FixedSingle;
            chart0.Dock = DockStyle.Fill;
            chart0.EnableLabels = true;
            chart0.EnablePlots = true;
            chart0.Location = new Point(3, 3);
            chart0.Name = "chart0";
            chart0.Size = new Size(1040, 929);
            chart0.TabIndex = 6;
            chart0.Yscale = 1F;
            // 
            // myTelemetryPane1
            // 
            myTelemetryPane1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            myTelemetryPane1.AutoClear = true;
            myTelemetryPane1.BackColor = Color.PapayaWhip;
            myTelemetryPane1.BorderStyle = BorderStyle.FixedSingle;
            myTelemetryPane1.Location = new Point(1050, 47);
            myTelemetryPane1.Name = "myTelemetryPane1";
            myTelemetryPane1.Size = new Size(342, 1245);
            myTelemetryPane1.TabIndex = 7;
            // 
            // tlpCharts
            // 
            tlpCharts.ColumnCount = 1;
            tlpCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCharts.Controls.Add(chart0, 0, 0);
            tlpCharts.Location = new Point(2, 45);
            tlpCharts.Name = "tlpCharts";
            tlpCharts.RowCount = 1;
            tlpCharts.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCharts.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCharts.Size = new Size(1046, 935);
            tlpCharts.TabIndex = 8;
            // 
            // butDBG
            // 
            butDBG.Location = new Point(5, 5);
            butDBG.Name = "butDBG";
            butDBG.Size = new Size(75, 23);
            butDBG.TabIndex = 9;
            butDBG.Text = "DBG";
            butDBG.UseVisualStyleBackColor = true;
            butDBG.Click += butDBG_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1395, 1304);
            Controls.Add(butDBG);
            Controls.Add(tlpCharts);
            Controls.Add(myTelemetryPane1);
            Controls.Add(dbg);
            Controls.Add(labPorts);
            Controls.Add(cbPorts);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Form1";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            Shown += Form1_Shown;
            tlpCharts.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private ComboBox cbPorts;
        private Label labPorts;
        private Plotter.UserControls.MyDebugPane dbg;
        private Plotter.UserControls.MyChart chart0;
        private Plotter.UserControls.MyTelemetryPane myTelemetryPane1;
        private TableLayoutPanel tlpCharts;
        private Button butDBG;
    }
}

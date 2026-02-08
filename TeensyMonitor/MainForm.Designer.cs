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
            chart0 = new TeensyMonitor.Plotter.UserControls.MyChart();
            tlpCharts = new TableLayoutPanel();
            butDBG = new Button();
            pHeader = new Panel();
            pDebugPane = new Panel();
            dbg = new TeensyMonitor.Plotter.UserControls.MyDebugPane();
            pTelemetryPane = new Panel();
            TelemetryPane = new TeensyMonitor.Plotter.UserControls.MyTelemetryPane();
            tlpCharts.SuspendLayout();
            pHeader.SuspendLayout();
            pDebugPane.SuspendLayout();
            pTelemetryPane.SuspendLayout();
            SuspendLayout();
            // 
            // cbPorts
            // 
            cbPorts.FormattingEnabled = true;
            cbPorts.Location = new Point(1060, 10);
            cbPorts.Name = "cbPorts";
            cbPorts.Size = new Size(121, 23);
            cbPorts.TabIndex = 3;
            cbPorts.SelectedIndexChanged += cbPorts_SelectedIndexChanged;
            // 
            // labPorts
            // 
            labPorts.AutoSize = true;
            labPorts.Location = new Point(991, 13);
            labPorts.Name = "labPorts";
            labPorts.Size = new Size(63, 15);
            labPorts.TabIndex = 4;
            labPorts.Text = "COM Port:";
            // 
            // chart0
            // 
            chart0.AllowPause = true;
            chart0.AutoClear = true;
            chart0.BackColor = Color.Cornsilk;
            chart0.BorderStyle = BorderStyle.FixedSingle;
            chart0.Dock = DockStyle.Fill;
            chart0.EnableLabels = true;
            chart0.EnablePlots = true;
            chart0.Location = new Point(3, 3);
            chart0.Name = "chart0";
            chart0.Padding = new Padding(4);
            chart0.Size = new Size(1054, 948);
            chart0.TabIndex = 6;
            chart0.Yscale = 1F;
            // 
            // tlpCharts
            // 
            tlpCharts.ColumnCount = 1;
            tlpCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCharts.Controls.Add(chart0, 0, 0);
            tlpCharts.Dock = DockStyle.Fill;
            tlpCharts.Location = new Point(0, 42);
            tlpCharts.Name = "tlpCharts";
            tlpCharts.RowCount = 1;
            tlpCharts.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCharts.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tlpCharts.Size = new Size(1060, 954);
            tlpCharts.TabIndex = 8;
            // 
            // butDBG
            // 
            butDBG.Location = new Point(15, 9);
            butDBG.Name = "butDBG";
            butDBG.Size = new Size(75, 23);
            butDBG.TabIndex = 9;
            butDBG.Text = "DBG";
            butDBG.UseVisualStyleBackColor = true;
            butDBG.Click += butDBG_Click;
            // 
            // pHeader
            // 
            pHeader.Controls.Add(butDBG);
            pHeader.Controls.Add(cbPorts);
            pHeader.Controls.Add(labPorts);
            pHeader.Dock = DockStyle.Top;
            pHeader.Location = new Point(0, 0);
            pHeader.Name = "pHeader";
            pHeader.Size = new Size(1395, 42);
            pHeader.TabIndex = 10;
            // 
            // pDebugPane
            // 
            pDebugPane.Controls.Add(dbg);
            pDebugPane.Dock = DockStyle.Bottom;
            pDebugPane.Location = new Point(0, 996);
            pDebugPane.Name = "pDebugPane";
            pDebugPane.Padding = new Padding(4);
            pDebugPane.Size = new Size(1395, 308);
            pDebugPane.TabIndex = 11;
            // 
            // dbg
            // 
            dbg.AllowPause = true;
            dbg.AutoClear = true;
            dbg.BackColor = Color.AliceBlue;
            dbg.BorderStyle = BorderStyle.FixedSingle;
            dbg.Dock = DockStyle.Fill;
            dbg.Location = new Point(4, 4);
            dbg.Name = "dbg";
            dbg.Size = new Size(1387, 300);
            dbg.TabIndex = 6;
            // 
            // pTelemetryPane
            // 
            pTelemetryPane.Controls.Add(TelemetryPane);
            pTelemetryPane.Dock = DockStyle.Right;
            pTelemetryPane.Location = new Point(1060, 42);
            pTelemetryPane.Name = "pTelemetryPane";
            pTelemetryPane.Padding = new Padding(3);
            pTelemetryPane.Size = new Size(335, 954);
            pTelemetryPane.TabIndex = 12;
            // 
            // TelemetryPane
            // 
            TelemetryPane.AllowPause = true;
            TelemetryPane.AutoClear = true;
            TelemetryPane.BackColor = Color.PapayaWhip;
            TelemetryPane.BorderStyle = BorderStyle.FixedSingle;
            TelemetryPane.Dock = DockStyle.Fill;
            TelemetryPane.Location = new Point(3, 3);
            TelemetryPane.Name = "TelemetryPane";
            TelemetryPane.Padding = new Padding(4);
            TelemetryPane.Size = new Size(329, 948);
            TelemetryPane.TabIndex = 8;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1395, 1304);
            Controls.Add(tlpCharts);
            Controls.Add(pTelemetryPane);
            Controls.Add(pDebugPane);
            Controls.Add(pHeader);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "fNIRS Prototype Data Monitor";
            Load += Form1_Load;
            tlpCharts.ResumeLayout(false);
            pHeader.ResumeLayout(false);
            pHeader.PerformLayout();
            pDebugPane.ResumeLayout(false);
            pTelemetryPane.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private ComboBox cbPorts;
        private Label labPorts;
        private Plotter.UserControls.MyChart chart0;
        private TableLayoutPanel tlpCharts;
        private Button butDBG;
        private Panel pHeader;
        private Panel pDebugPane;
        private Plotter.UserControls.MyDebugPane dbg;
        private Panel pTelemetryPane;
        private Plotter.UserControls.MyTelemetryPane TelemetryPane;
    }
}

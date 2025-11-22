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
            components = new System.ComponentModel.Container();
            tb = new TextBox();
            cbPorts = new ComboBox();
            labPorts = new Label();
            labIR = new Label();
            tbIR_value = new TextBox();
            tbRed_value = new TextBox();
            labRed = new Label();
            labAmbient = new Label();
            tbAmbient_value = new TextBox();
            SuspendLayout();
            // 
            // tb
            // 
            tb.Location = new Point(73, 629);
            tb.Multiline = true;
            tb.Name = "tb";
            tb.Size = new Size(830, 76);
            tb.TabIndex = 1;
            // 
            // cbPorts
            // 
            cbPorts.FormattingEnabled = true;
            cbPorts.Location = new Point(157, 12);
            cbPorts.Name = "cbPorts";
            cbPorts.Size = new Size(121, 23);
            cbPorts.TabIndex = 3;
            cbPorts.SelectedIndexChanged += cbPorts_SelectedIndexChanged;
            // 
            // labPorts
            // 
            labPorts.AutoSize = true;
            labPorts.Location = new Point(88, 15);
            labPorts.Name = "labPorts";
            labPorts.Size = new Size(63, 15);
            labPorts.TabIndex = 4;
            labPorts.Text = "COM Port:";
            // 
            // labIR
            // 
            labIR.BackColor = Color.Transparent;
            labIR.Location = new Point(928, 132);
            labIR.Name = "labIR";
            labIR.Size = new Size(40, 15);
            labIR.TabIndex = 5;
            labIR.Text = "IR:";
            labIR.TextAlign = ContentAlignment.MiddleRight;
            // 
            // tbIR_value
            // 
            tbIR_value.Location = new Point(971, 129);
            tbIR_value.Name = "tbIR_value";
            tbIR_value.ReadOnly = true;
            tbIR_value.Size = new Size(100, 23);
            tbIR_value.TabIndex = 6;
            tbIR_value.TextAlign = HorizontalAlignment.Right;
            // 
            // tbRed_value
            // 
            tbRed_value.Location = new Point(971, 168);
            tbRed_value.Name = "tbRed_value";
            tbRed_value.ReadOnly = true;
            tbRed_value.Size = new Size(100, 23);
            tbRed_value.TabIndex = 8;
            tbRed_value.TextAlign = HorizontalAlignment.Right;
            // 
            // labRed
            // 
            labRed.BackColor = Color.Transparent;
            labRed.Location = new Point(925, 171);
            labRed.Name = "labRed";
            labRed.Size = new Size(40, 15);
            labRed.TabIndex = 9;
            labRed.Text = "Red:";
            labRed.TextAlign = ContentAlignment.MiddleRight;
            // 
            // labAmbient
            // 
            labAmbient.BackColor = Color.Transparent;
            labAmbient.Location = new Point(910, 212);
            labAmbient.Name = "labAmbient";
            labAmbient.Size = new Size(55, 18);
            labAmbient.TabIndex = 11;
            labAmbient.Text = "Ambient:";
            labAmbient.TextAlign = ContentAlignment.MiddleRight;
            // 
            // tbAmbient_value
            // 
            tbAmbient_value.Location = new Point(971, 211);
            tbAmbient_value.Name = "tbAmbient_value";
            tbAmbient_value.ReadOnly = true;
            tbAmbient_value.Size = new Size(100, 23);
            tbAmbient_value.TabIndex = 10;
            tbAmbient_value.TextAlign = HorizontalAlignment.Right;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1184, 972);
            Controls.Add(labAmbient);
            Controls.Add(tbAmbient_value);
            Controls.Add(labRed);
            Controls.Add(tbRed_value);
            Controls.Add(tbIR_value);
            Controls.Add(labIR);
            Controls.Add(labPorts);
            Controls.Add(cbPorts);
            Controls.Add(tb);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private TextBox tb;
        private ComboBox cbPorts;
        private Label labPorts;
        private Label labIR;
        private TextBox tbIR_value;
        private TextBox tbRed_value;
        private Label labRed;
        private Label labAmbient;
        private TextBox tbAmbient_value;
    }
}



namespace TeensyMonitor
{
    using PsycSerial;
    using System.Text;
    using TeensyMonitor.Plotter.Helpers;
    using TeensyMonitor.Plotter.UserControls;

    public partial class MainForm : Form
    {
        readonly TeensySerial? SP = Program.serialPort;
        readonly CancellationTokenSource cts = new();

        readonly Dictionary<HeadState, MyChart> charts = [];

        readonly System.Windows.Forms.Timer uiTimer = new() { Interval = 1000 / 20, Enabled = true };

        public MainForm()
        {
            InitializeComponent();

            switch (Environment.MachineName)
            {
                case "BOX":
                    this.StartPosition = FormStartPosition.Manual;
                    this.Location = new Point(-1420, -100);
                    break;

                case "PSYC-ANDREW":
                    this.StartPosition = FormStartPosition.Manual;
                    this.Location = new Point(180, 100);
                    break;
            }

            uiTimer.Tick += (s, e) =>
            {
                StringBuilder sb = new();
                foreach (var chart in charts.Values)
                    sb.Append($"{chart.Tag}: {chart.FPS:00.000} FPS   ");
                Text = sb.ToString();
            };
            if (SP == null) return;

            SP.DataReceived      += SP_DataReceived;
            SP.ConnectionChanged += SP_ConnectionChanged;
            SP.ErrorOccurred     += SP_ErrorOccurred;
        }


        readonly MyPool<Dictionary<string, double>> parsedPool = new();

        private void SP_DataReceived(IPacket packet)
        {
            if (IsHandleCreated == false) return;

            if (packet is     BlockPacket blockPacket) { AddBlockPacket(blockPacket); return; }
            if (packet is      TextPacket  textPacket) {  AddTextPacket( textPacket); return; }
            if (packet is TelemetryPacket  telePacket) {  AddTelePacket( telePacket); return; }

        }

        private void AddBlockPacket(BlockPacket blockPacket)
        {
            if (charts.Count == 0)
            {
                charts[blockPacket.State] = chart0;
                chart0.Tag = blockPacket.State.Description();
            }
            if (charts.TryGetValue(blockPacket.State, out MyChart? chart) && chart != null)
                chart.SP_DataReceived(blockPacket);
            else
            {
                MyChart newChart = new()
                {
                    BackColor = chart0.BackColor,
                    Dock      = chart0.Dock,
                    Tag       = blockPacket.State.Description(),
                };

                charts[blockPacket.State] = newChart;
                newChart.SP_DataReceived(blockPacket);

                AddNewChart(newChart);
            }
        }

        private void AddTextPacket(TextPacket textPacket)
        {
            var parsedValues = parsedPool.Rent();
            if (MyTextParser.Parse(textPacket.Text, parsedValues))
            {
                chart0.AddData(parsedValues);
                parsedPool.Return(parsedValues);
            }
            else
                dbg.Log(textPacket.Text);
        }

        private void AddTelePacket(TelemetryPacket telePacket)
        {
            myTelemetryPane1.SP_DataReceived(telePacket);
        }

        private async void SP_ErrorOccurred(Exception exception)
        {
            if (IsHandleCreated == false) return;

            dbg.Log(AString.FromString(exception.Message + Environment.NewLine));

            while (!cts.Token.IsCancellationRequested && SP?.IsOpen == false) // null check here
            {
                await Task.Delay(500, cts.Token); // Wait before retrying
                if (cts.Token.IsCancellationRequested) return;

                var ports = SerialHelper.GetUSBSerialPorts();
                if (ports?.Length > 0)
                {
                    await Task.Delay(200, cts.Token);
                    if (cts.Token.IsCancellationRequested) return;

                    if (SP?.IsOpen == false) // check again before opening
                        this.Invoker(() =>
                        {
                            cbPorts.Items.Clear();
                            cbPorts.Items.AddRange(ports);
                            cbPorts.SelectedIndex = cbPorts.Items.Count - 1;
                        });
                }
            }
        }
        private void SP_ConnectionChanged(ConnectionState state)
        {
            if (IsHandleCreated == false) return;

            AString? str = state switch
            {
                ConnectionState.Connected => AString.FromString("Connected " + SP?.PortName),
                ConnectionState.HandshakeInProgress => AString.FromString("Handshake in progress"),
                ConnectionState.Disconnected => AString.FromString("Disconnected"),
                ConnectionState.HandshakeSuccessful => null,  // string comes from the device
                _ => null
            };

            bool enableDropdown = state == ConnectionState.Disconnected;

            if (cbPorts.Enabled != enableDropdown)
                this.Invoker(() => cbPorts.Enabled = enableDropdown);

            if (state == ConnectionState.Connected)
                dbg.Clear();

            if (str != null)
                dbg.Log(str);
        }

        

        private void cbPorts_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbPorts.SelectedItem == null) return;

            SP?.Open(cbPorts.SelectedItem.ToString());
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            var ports = SerialHelper.GetUSBSerialPorts();

            if (ports.Length == 0)
            {
                MessageBox.Show("No serial ports found.");
                Close();
                return;
            }
            else
            {
                cbPorts.Items.AddRange(ports);
                cbPorts.SelectedIndex = cbPorts.Items.Count - 1;
            }
        }



        private void AddNewChart(MyChart newChart)
        {
            if (IsHandleCreated == false) return;

            this.Invoker(() =>
            {
                SuspendLayout();

                tlpCharts.RowCount += 1;
                tlpCharts.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                tlpCharts.Controls.Add(newChart, 0, tlpCharts.RowCount - 1);

                ResumeLayout(true);

                this.Focus();
            });
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
            => cts.Cancel();

        private void Form1_Shown(object sender, EventArgs e)
            => this.Focus();
    }
}

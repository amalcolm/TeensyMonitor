

namespace TeensyMonitor
{
    using PsycSerial;
    using TeensyMonitor.Plotter.Helpers;
    using TeensyMonitor.Plotter.UserControls;

    public partial class Form1 : Form
    {
        readonly TeensySerial? SP = Program.serialPort;
        readonly CancellationTokenSource cts = new();

        readonly Dictionary<HeadState, MyChart> charts = [];
        public Form1()
        {
            InitializeComponent();

            if (Environment.MachineName == "BOX")
            {
                this.StartPosition = FormStartPosition.Manual;
                this.Location = new Point(-1280, -100);
            }

            if (SP == null) return;

            SP.DataReceived += SP_DataReceived;
            SP.ConnectionChanged += SP_ConnectionChanged;
            SP.ErrorOccurred += SP_ErrorOccurred;
        }


        readonly MyPool<Dictionary<string, double>> parsedPool = new();

        private void SP_DataReceived(IPacket packet)
        {
            if (IsHandleCreated == false) return;


            if (packet is BlockPacket blockPacket)
            {
                if (charts.Count == 0)
                    charts[blockPacket.State] = chart0;

                if (charts.TryGetValue(blockPacket.State, out MyChart? chart) && chart != null)
                    chart.SP_DataReceived(blockPacket);
                else
                {
                    MyChart newChart = new() { TimeWindowSeconds = chart0.TimeWindowSeconds };
                    charts[blockPacket.State] = newChart;
                    newChart.SP_DataReceived(blockPacket);

                    AddNewChart(newChart);
                }
            }

            if (packet is TextPacket textPacket == false) return;

            var parsedValues = parsedPool.Rent();
            if (MyTextParser.Parse(textPacket.Text, parsedValues))
            {
                chart0.AddData(parsedValues);
                parsedPool.Return(parsedValues);
            }
            else
                dbg.Log(textPacket.Text);
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

                int dbgTop = this.ClientSize.Height - dbg.Location.Y;

                this.ClientSize = new Size(this.ClientSize.Width, this.ClientSize.Height + chart0.Height + 10);

                newChart.Location = new Point(chart0.Location.X, chart0.Location.Y + (chart0.Height + 10) * (charts.Count - 1));
                newChart.Size = chart0.Size;

                Controls.Add(newChart);

                dbg.Location = new Point(dbg.Location.X, this.ClientSize.Height - dbgTop);

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

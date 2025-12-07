

namespace TeensyMonitor
{
    using PsycSerial;
    using System.Security.Policy;
    using TeensyMonitor.Plotter.Helpers;

    public partial class Form1 : Form
    {
        readonly TeensySerial? SP = Program.serialPort;
        readonly CancellationTokenSource cts = new();
        public Form1()
        {
            InitializeComponent();

            if (Environment.MachineName == "BOX")
            {
                this.StartPosition = FormStartPosition.Manual;
                this.Location = new Point(-1280, 1100);
            }

            if (SP == null) return;

            SP.DataReceived += DataReceived;
            SP.ConnectionChanged += SerialPort_ConnectionChanged;
            SP.ErrorOccurred += SerialPort_ErrorOccurred;
        }

        private async void SerialPort_ErrorOccurred(Exception exception)
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
        private void SerialPort_ConnectionChanged(ConnectionState state)
        {
            if (IsHandleCreated == false) return;

            AString? str = state switch
            {
                ConnectionState.Connected           => AString.FromString("Connected " + SP?.PortName),
                ConnectionState.HandshakeInProgress => AString.FromString("Handshake in progress"    ),
                ConnectionState.Disconnected        => AString.FromString("Disconnected"             ),
                ConnectionState.HandshakeSuccessful => null,  // string comes from the device
                _ => null
            };

            bool enableDropdown = state == ConnectionState.Disconnected;

            if (cbPorts.Enabled != enableDropdown)
                this.Invoker(() => cbPorts.Enabled = enableDropdown );

            if (str != null)
               dbg.Log(str);
        }

        readonly MyPool<Dictionary<string, double>> parsedPool = new();
        
        private void DataReceived(IPacket packet)
        {
            if (IsHandleCreated == false) return;
            if (packet is TextPacket textPacket == false) return;

            var parsedValues = parsedPool.Rent();
            if (MyTextParser.Parse(textPacket.Text, parsedValues))
            {
                chart.AddData(parsedValues);
                parsedPool.Return(parsedValues);
            }
            else
                dbg.Log(textPacket.Text);
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

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            cts.Cancel();
        }
    }
}

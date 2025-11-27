

namespace TeensyMonitor
{
    using PsycSerial;

    public partial class Form1 : Form
    {
        readonly TeensySerial SP = Program.serialPort;

        public Form1()
        {
            InitializeComponent();

            SP.DataReceived += DataReceived;
            SP.ConnectionChanged += SerialPort_ConnectionChanged;
            SP.ErrorOccurred += SerialPort_ErrorOccurred;

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

        private void SerialPort_ErrorOccurred(Exception exception)
            => this.Invoker(delegate { tb.AppendText(exception.Message + Environment.NewLine); });

        bool stateSet = false;

        private void SerialPort_ConnectionChanged(ConnectionState state)
        {
            if (IsHandleCreated == false) return;

            this.Invoker(delegate
            {
                switch (state)
                {
                    case ConnectionState.Connected:
                    case ConnectionState.HandshakeInProgress: tb.AppendText("Connected " + SP.PortName + Environment.NewLine); break;
                    case ConnectionState.Disconnected:        tb.AppendText("Disconnected"             + Environment.NewLine); break;
                    case ConnectionState.HandshakeSuccessful: tb.AppendText("Handshake successful"     + Environment.NewLine); break;
                }

                stateSet = true;
            });
        }

        int count = 0;
        private void DataReceived(IPacket packet)
        {
            if (IsHandleCreated == false) return;
            if (packet is TextPacket textPacket == false) return;

            this.Invoker( () =>             {
                Text = $".{count++} {textPacket.Length}";
            });

        }

        private void cbPorts_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbPorts.SelectedItem == null) return;

            SP.Open(cbPorts.SelectedItem.ToString());
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (SP.IsOpen && stateSet == false) SerialPort_ConnectionChanged(SP.CurrentConnectionState);
        }
    }
}

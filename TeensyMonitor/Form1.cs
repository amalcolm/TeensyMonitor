

namespace TeensyMonitor
{
    using PsycSerial;

    public partial class Form1 : Form
    {
        readonly TeensySerial SP = Program.serialPort;

        public Form1()
        {
            InitializeComponent();

            SP.DataReceived      += DataReceived;
            SP.ConnectionChanged += SerialPort_ConnectionChanged;
            SP.ErrorOccurred     += SerialPort_ErrorOccurred;
        }

        private void SerialPort_ErrorOccurred(Exception exception)
            => myDebugPane1.Log(AString.FromString(exception.Message + Environment.NewLine));

        private void SerialPort_ConnectionChanged(ConnectionState state)
        {
            AString? str = state switch
            {
                ConnectionState.Connected           => AString.FromString("Connected " + SP.PortName),
                ConnectionState.HandshakeInProgress => AString.FromString("Handshake in progress"   ),
                ConnectionState.Disconnected        => AString.FromString("Disconnected"            ),
                ConnectionState.HandshakeSuccessful => AString.FromString("Handshake successful"    ),
                _ => null
            };

            if (str != null)
                myDebugPane1.Log(str);
        }

        int count = 0;
        private void DataReceived(IPacket packet)
        {
            if (IsHandleCreated == false) return;
            if (packet is TextPacket textPacket == false) return;

            this.Invoker(() =>
            {
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
    }
}

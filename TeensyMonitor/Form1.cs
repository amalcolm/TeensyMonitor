

namespace TeensyMonitor
{
    using PsycSerial;

    public partial class Form1 : Form
    {
        readonly SerialHelper SP = Program.serialPort;

        FrameTypes.DebugIO? frame;
        readonly Mutex mutex = new();

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

        private void SerialPort_ConnectionChanged(bool isOpen)
        {
            if (IsHandleCreated == false) return;

            this.Invoker(delegate
            {
                if (isOpen)
                    tb.AppendText("Connected " + SP.PortName + Environment.NewLine);
                else
                    tb.AppendText("Disconnected" + Environment.NewLine);

                stateSet = true;
            });
        }

        int count = 0;
        private void DataReceived(ManagedPacket packet)
        {
            // convert packet.data to string
            var data = System.Text.Encoding.UTF8.GetString(packet.Data);

            this.Invoker( () =>             {
                Text = $".{count++} {data.Length}";
            });
            if (data.StartsWith("Amb:"))
            {
                frame = new FrameTypes.DebugIO(packet);
            }

        }

        private void cbPorts_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbPorts.SelectedItem == null) return;

            SP.Open(cbPorts.SelectedItem.ToString());
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (SP.IsOpen && stateSet == false) SerialPort_ConnectionChanged(true);
        }
    }
}

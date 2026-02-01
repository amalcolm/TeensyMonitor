using Timer = System.Windows.Forms.Timer;

namespace TeensyMonitor
{
    using PsycSerial;
    using System.Diagnostics;
    using TeensyMonitor.Plotter.Helpers;
    using TeensyMonitor.Plotter.UserControls;


    public partial class MainForm : Form
    {
        readonly TeensySerial? SP = Program.serialPort;
        readonly CancellationTokenSource cts = new();

        readonly Dictionary<HeadState, MyChart> Charts = [];

        enum FormState
        {
            None,
            Initialising,
            Building,
            Running
        }

        public MainForm()
        {
            InitializeComponent();

            switch (Environment.MachineName)
            {
                case "BOX":
                    this.StartPosition = FormStartPosition.Manual;
                    this.Location = new Point(-1420, -100);
                    this.WindowState = FormWindowState.Maximized;
                    break;

                case "PSYC-ANDREW":
                    this.StartPosition = FormStartPosition.Manual;
                    this.Location = new Point(180, 100);
                    this.WindowState = FormWindowState.Maximized;
                    break;
            }

            monitorTimer.Tick += (s, e) =>
            {
                var ports = SerialHelper.GetUSBSerialPorts();
                if (ports.Length > 0)
                {
                    monitorTimer.Stop();
                    this.Invoker(() =>
                    {
                        cbPorts.Items.Clear();
                        cbPorts.Items.AddRange(ports);
                        cbPorts.SelectedIndex = cbPorts.Items.Count - 1;
                    });
                }
            };

            if (SP == null) return;

            SP.DataReceived += SP_DataReceived;
            SP.ConnectionChanged += SP_ConnectionChanged;
            SP.ErrorOccurred += SP_ErrorOccurred;

            Init_Clear();
        }

        private readonly Timer monitorTimer = new() { Interval = 1000, Enabled = false };


        readonly MyPool<Dictionary<string, double>> parsedPool = new();

        private void SP_DataReceived(IPacket packet)
        {
            if (IsHandleCreated == false) return;

            if (packet is     BlockPacket blockPacket) AddBlockPacket(blockPacket);
            if (packet is      TextPacket textPacket ) AddTextPacket( textPacket);
            if (packet is TelemetryPacket telePacket ) AddTelePacket( telePacket);

            packet.Cleanup();
        }

        private void AddBlockPacket(BlockPacket blockPacket)
        {
            if (blockPacket.Count == 0) return;

            if (State != FormState.Running) { Init_Packet(blockPacket); return; }

            if (Charts.TryGetValue(blockPacket.State, out MyChart? chart) && chart != null)
            {
                chart.SP_DataReceived(blockPacket);

                tallForm?.Process(blockPacket);
            }
            else
            {
                MyColours.Reset();
                MyChart newChart = new()
                {
                    BackColor = chart0.BackColor,
                    Dock = chart0.Dock,
                    Tag = blockPacket.State.Description(),
                };

                Charts[blockPacket.State] = newChart;
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
            => TelemetryPane.SP_DataReceived(telePacket);


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

            switch (state)
            {
                case ConnectionState.Connected:
                    dbg.Clear();
                    dbg.Log(str);
                    State = FormState.Initialising;
                    break;
                case ConnectionState.Disconnected:
                    State = FormState.None;

                    if (SocketWatcher.ReceivedDisconnect)
                    {
                        dbg.Log(AString.FromString("Disconnected by request, waiting for reconnect..."));
                        return;
                    }
                    dbg.Log(str);

                    this.Invoker(() => Form1_Load(this, EventArgs.Empty));
                    break;
            }

        }



        private void cbPorts_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbPorts.SelectedItem == null || cbPorts.SelectedItem.ToString() == "No ports found") return;

            SP?.Open(cbPorts.SelectedItem.ToString());
        }


        bool firstLoad = true;
        MyTallForm? tallForm;
        private void Form1_Load(object sender, EventArgs e)
        {
            var ports = SerialHelper.GetUSBSerialPorts();

            if (ports.Length == 0)
            {
                if (firstLoad)
                {
                    for (var res = DialogResult.Retry; res == DialogResult.Retry;)
                    {
                        ports = SerialHelper.GetUSBSerialPorts();
                        if (ports.Length > 0)
                            break;

                        res = MessageBox.Show("Could not find any device", "Device Not Found", MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Warning);

                        if (res == DialogResult.Abort)
                        {
                            Close();
                            return;
                        }
                    }
                }
                cbPorts.Items.Clear();
                cbPorts.Items.Add("No ports found");
                cbPorts.SelectedIndex = 0;

                monitorTimer.Start();
            }
            else
            {
                if (firstLoad)
                {
                    tallForm = new MyTallForm();
                    tallForm.FormClosed += (_, _) => this.Close();
                    tallForm.Show();
                }

                firstLoad = false;
                cbPorts.Items.Clear();
                cbPorts.Items.AddRange(ports);
                cbPorts.SelectedIndex = cbPorts.Items.Count - 1;
            }
        }


        private void Form1_Shown(object sender, EventArgs e)
            => this.Focus();

        int index = -1;
        private void butDBG_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < Charts.Count; i++)
            {
                var chart = Charts.ElementAt(i).Value;

                dbg.Log(chart.getDebugOutput(index));
            }
            butDBG.Text = $"DBG {index++}";
        }

        private readonly Stopwatch _swInit = new();

        private FormState State
        {
            get => _state;

            set
            {
                _state = value;
                _swInit.Restart();

                switch (_state)
                {
                    case FormState.None        : this.Invoker(Init_Clear);  break;
                    case FormState.Initialising:                            break;
                    case FormState.Building    : this.Invoker(Init_Set);    break;
                    case FormState.Running     :                            break;
                }
            }
        }

        private FormState _state = FormState.None;
        private readonly Dictionary<HeadState, int> initStates = [];

        private void Init_Clear()
        {

            for (int r = tlpCharts.RowCount - 1; r >= 0; r--)
                for (int c = tlpCharts.ColumnCount - 1; c >= 0; c--)
                {
                    var control = tlpCharts.GetControlFromPosition(c, r);
                    if (control is not null && control is MyChart chart && chart != chart0)
                    {
                        tlpCharts.Controls.Remove(control);
                        chart.Close();
                    }
                }

            tlpCharts.RowCount = 1;
            tlpCharts.ColumnCount = 1;
            tlpCharts.RowStyles.Clear();
            tlpCharts.ColumnStyles.Clear();

            Charts.Clear();


            initStates.Clear();
        }

        private void Init_Packet(BlockPacket blockPacket)
        {
            switch (State)
            {
                case FormState.None:
                case FormState.Initialising:
                    if (initStates.ContainsKey(blockPacket.State) == false)
                        initStates.Add(blockPacket.State, 0);
                    else
                        if (++initStates[blockPacket.State] > 2)
                        State = FormState.Building; // seen state enough times

                    if (_swInit.ElapsedMilliseconds > 1000)
                        State = FormState.Building;
                    break;
            }
        }

        MyChart[,] tabCharts = new MyChart[0, 0]; 

        private void Init_Set()
        {
            if (initStates.Count == 0) return;

            HeadState[] states = [.. initStates.Keys];
            Array.Sort(states);

            if (states.Length > 4)
            {
                WindowState = FormWindowState.Normal;
                Location = Point.Empty;
                WindowState = FormWindowState.Maximized;
            }
            Init_SetTable();

            int _tlpColumn = 0; int _tlpRow = 0;

            tabCharts = new MyChart[tlpCharts.ColumnCount, tlpCharts.RowCount];

            foreach (var state in states)
            {
                MyColours.Reset();

                MyChart newChart;
                if (_tlpColumn == 0 && _tlpRow == 0)
                    newChart = chart0;
                else
                    newChart = new MyChart()
                    {
                        BackColor = chart0.BackColor,
                        Dock = chart0.Dock,
                        Tag = state.Description(),
                    };
                
                Charts[state] = newChart;

                tabCharts[_tlpColumn, _tlpRow] = newChart;

                _tlpColumn++;
                if (_tlpColumn >= tlpCharts.ColumnCount)
                {
                    _tlpColumn = 0;
                    _tlpRow++;
                }

            }
            
            this.Invoker(() =>
            {
                SuspendLayout();
                for (int r = 0; r < tlpCharts.RowCount; r++)
                    for (int c = 0; c < tlpCharts.ColumnCount; c++)
                    {
                        if (r == 0 && c == 0) continue; // skip chart0

                        var chart = tabCharts[c, r]; if (chart == null) continue;

                        try { tlpCharts.Controls.Add(chart, c, r); }
                        catch (Exception)
                        {
//                            dbg.Log(AString.FromString($"Error adding chart for {chart.Tag}: {ex.Message}"));
                        }
                    }
                ResumeLayout(true);
                State = FormState.Running;
            });

        }

        private void Init_SetTable()
        {
            int n = initStates.Count;
            int cols = n < 3 ? 1 : (int)Math.Ceiling(Math.Sqrt(n));
            int rows = (int)Math.Ceiling((double)n / cols);

            tlpCharts.SuspendLayout();
            try
            {
                // tlpCharts already cleared in Init_Clear

                tlpCharts.ColumnCount = cols;
                tlpCharts.RowCount = rows;

                for (int c = 0; c < cols; c++) tlpCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f / cols));
                for (int r = 0; r < rows; r++) tlpCharts.   RowStyles.Add(new    RowStyle(SizeType.Percent, 100.0f / rows));

                tlpCharts.GrowStyle = TableLayoutPanelGrowStyle.FixedSize; // optional but avoids surprise growth
            }
            finally
            {
                tlpCharts.ResumeLayout(true);
            }

        }
    }
}

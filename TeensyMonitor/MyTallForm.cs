using PsycSerial;
using TeensyMonitor.DataTools;

namespace TeensyMonitor.Plotter.UserControls
{
    public partial class MyTallForm : Form
    {
        private readonly Dictionary<HeadState, SignalExtractor> _extractors = [];

        public MyTallForm()
        {
            InitializeComponent();

            chart.BackColor = chart.BackColor.Darken();


            switch (Environment.MachineName)
            {
                case "BOX":
                    this.StartPosition = FormStartPosition.Manual;
                    this.Location = new Point(3840, -200);
                    this.WindowState = FormWindowState.Maximized;
                    break;

                case "PSYC-ANDREW":
                    this.StartPosition = FormStartPosition.Manual;
                    this.Location = new Point(180, 100);
                    this.WindowState = FormWindowState.Maximized;
                    break;
            }


            FormClosing += (_, _) =>
            {
                foreach (var extractor in _extractors.Values)
                    extractor.Dispose();
                _extractors.Clear();
            };
        }

        public void Process(BlockPacket blockPacket)
        {
            if (blockPacket.Count == 0) return;

            DataPacket packet = blockPacket.BlockData[blockPacket.Count - 1];
            
            if (!_extractors.TryGetValue(packet.State, out var extractor))
                _extractors[packet.State] = extractor = new SignalExtractor(packet.State) { Chart = chart };

            extractor.Process(packet);
        }

        bool isMouseDown = false;
        int original_Y = 0;

        private void MyTallForm_MouseDown(object sender, MouseEventArgs e)
        {
            isMouseDown = true;
            original_Y = e.Y;
        }

        private void MyTallForm_MouseMove(object sender, MouseEventArgs e)
        {   if (!isMouseDown) return;

        }

        private void MyTallForm_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Y == original_Y) return;  
            isMouseDown = false;
        }


        readonly MouseEventArgs dummy = new(MouseButtons.Left, 1, 0, 0, 0);
        private void MyTallForm_MouseLeave(object sender, EventArgs e)
            => MyTallForm_MouseUp(sender, dummy);
    }
}

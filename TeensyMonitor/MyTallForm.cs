using PsycSerial;
using TeensyMonitor.DataTools;

namespace TeensyMonitor.Plotter.UserControls
{
    public partial class MyTallForm : Form
    {
        private readonly SignalExtractor extractor = new();

        public MyTallForm()
        {
            InitializeComponent();

            chart.BackColor = chart.BackColor.Darken();


            extractor.Chart = chart;

            FormClosing += (_, _) => extractor.Dispose();
        }

        public void Process(BlockPacket blockPacket)
        {
            if (blockPacket.Count == 0) return;

            DataPacket packet = blockPacket.BlockData[blockPacket.Count - 1];

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

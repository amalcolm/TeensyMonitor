using PsycSerial;

namespace TeensyMonitor.Plotter.UserControls
{
    public partial class MyTallForm : Form
    {
        const double scale_C0 = 1.0 / 4660.100;

        private double delta_Offset2 = 368.0;

        readonly Dictionary<string, double> data = [];

        public MyTallForm()
        {
            InitializeComponent();

            chart.AllowPause = false;
        }
        public void Process(BlockPacket blockPacket)
        {
            if (blockPacket.Count == 0) return;

            DataPacket packet = blockPacket.BlockData[blockPacket.Count - 1];

            if (chart.GetMetrics() is var metrics && metrics != null)
            {
                data["-Min"] = metrics.MinY;
                data["-Max"] = metrics.MaxY;
                data["-Range"] = metrics.RangeY;
                data["-DesiredRange"] = metrics.DesiredRangeY;

                if (Offset2 == int.MaxValue)
                {
                    Offset2 = 0;
                    lastOffset2 = packet.Offset2;
                }
            }

            double value = CalcValue(packet);

            data["Time"] = packet.TimeStamp;
            data["Value"] = value;

            
            chart.AddData(data);
        }

        int Offset2 = int.MaxValue;
        int lastOffset2 = int.MinValue;
        private double CalcValue(DataPacket packet)
        {
            if (Offset2 == int.MinValue) return 0;  // wait for metrics to be initialised, sets lastOffset2

            data["-Offset2"] = Offset2;
            
            Offset2 += packet.Offset2 - lastOffset2;
            lastOffset2 = packet.Offset2;

            double C0 = packet.Channel[0] * scale_C0;

            return C0 + Offset2 * delta_Offset2;
        }

        bool isMouseDown = false;
        int original_Y = 0;
        double original_Value = 0.0;

        private void MyTallForm_MouseDown(object sender, MouseEventArgs e)
        {
            isMouseDown = true;
            original_Y = e.Y;
            original_Value = delta_Offset2;
        }

        private void MyTallForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isMouseDown) return;

            delta_Offset2 = original_Value + (e.Y - original_Y)*1.0;

            data["-DeltaOffset2"] = delta_Offset2;
        }

        private void MyTallForm_MouseUp(object sender, MouseEventArgs e)
        {
            isMouseDown = false;
        }


        readonly MouseEventArgs dummy = new(MouseButtons.Left, 1, 0, 0, 0);
        private void MyTallForm_MouseLeave(object sender, EventArgs e)
            => MyTallForm_MouseUp(sender, dummy);
    }
}

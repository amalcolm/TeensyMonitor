using PsycSerial;
using TeensyMonitor.Plotter.Helpers;

namespace TeensyMonitor.Plotter.UserControls
{
    public partial class MyTallForm : Form
    {
        const double scale_C0 = 1.0 / 4660.100;

        const double delta_Offset2 = 200.0;

        readonly Dictionary<string, double> incomingData = [];

        public MyTallForm()
        {
            InitializeComponent();

            incomingData.Add("Time", 0);
            incomingData.Add("Value", 0);
        }

        public void Process(BlockPacket blockPacket)
        {
            if (blockPacket.Count == 0) return;

            incomingData["Time"] = blockPacket.BlockData[blockPacket.Count - 1].TimeStamp;
            incomingData["Value"] = CalcValue(blockPacket.BlockData[blockPacket.Count - 1]);

            chart.AddData(incomingData);
        }

        private static double CalcValue(DataPacket packet)
        {
            double c0 = packet.Channel[0] * scale_C0;
            return packet.Offset2 * delta_Offset2 + c0;
        }
    }
}

using PsycSerial;
using PsycSerial.Math;
using TeensyMonitor.Plotter.UserControls;

namespace TeensyMonitor.DataTools
{
    internal class SignalExtractor : IDisposable
    {
        private readonly record struct XY(double X, double Y);

        public  readonly Dictionary<string, double> telemetry = [];

        private readonly ZFixer fixer = new();
        private const double scale_C0 = 1.0 / 4660.100;
        private const double delta_Offset2 = 368.0;

        private const uint ra_Size = 99;

        private readonly RunningAverage ra = new(ra_Size);
        private readonly XY[] _buffer = new XY[ra_Size];
        private uint _ra_index = 0;

        public SignalExtractor() => fixer.Telemetry = telemetry;
        public void Dispose() => fixer.Dispose();


        public MyChart? Chart { get; set; } = null;
        public bool chartSet = false;


        int lastOffset2 = 0;

        public bool Process(DataPacket packet)
        {
            SetChart(packet); // keeps your existing init logic

            double C0 = packet.Channel[0] * scale_C0;

            bool isDiscontinuity = packet.Offset2 != lastOffset2;

            // Update last for next time (do this regardless)
            lastOffset2 = packet.Offset2;

            // Use the RAW packet Offset2 directly — this is the key fix!
            double y = C0 + packet.Offset2 * delta_Offset2;

            double x = packet.TimeStamp;

            if (isDiscontinuity)
            {
                // You could still run the fixer predict here if you want continuity in ZFixer state
                fixer.Predict(ref x, ref y);
                // But for now, just skip adding to chart
            }
            else
            {
                bool changed = fixer.Fix(ref x, ref y);

                telemetry["Time"] = x;
                telemetry["+Value"] = y;

                ra.Add(y);
                _buffer[_ra_index++] = new XY(x, y);
                if (_ra_index == ra_Size) _ra_index = 0;

                if (ra.Count == ra_Size)
                {
                    uint delay = (ra_Size - 1) / 2;
                    uint bufferIndex = (_ra_index + delay) % ra_Size;
                    telemetry["+Signal"] = _buffer[bufferIndex].Y - ra.Average;
                }

                Chart?.AddData(telemetry);

                return changed;
            }

            return false; // or whatever makes sense when skipped
        }
        private bool SetChart(DataPacket packet)
        {
            if (chartSet) return true;

            if (Chart?.GetMetrics() is var metrics && metrics != null)
            {
                lastOffset2 = packet.Offset2;
                chartSet = true;

                return true;
            }
            else
                return false;
            
        }
    }
}

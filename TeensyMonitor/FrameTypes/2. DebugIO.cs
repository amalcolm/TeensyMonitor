using System.Text;
using System.Text.RegularExpressions;
using TeensyMonitor.Helpers;

namespace TeensyMonitor.FrameTypes
{
    partial class DebugIO : BaseFrame, IAmbRedIR
    {
        public string Text { get; set; }

        public double Ambient => ambient;
        public double Red => red;
        public double IR => ir;


        public DebugIO(PsycSerial.ManagedPacket packet)
        {
            Text = Encoding.UTF8.GetString(packet.Data);

            var match = MyRegex.Match(Text);
            if (match.Success)
            {
                ambient = int.Parse(match.Groups[1].Value);
                red = int.Parse(match.Groups[2].Value);
                ir = int.Parse(match.Groups[3].Value);
            }
        }

        private static readonly Regex MyRegex = new(@"Amb:(-?\d+)\tRED:(-?\d+)\tIR:(-?\d+)", RegexOptions.Compiled);
    }
}

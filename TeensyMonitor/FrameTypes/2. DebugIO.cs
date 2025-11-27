using System.Text;
using System.Text.RegularExpressions;
using TeensyMonitor.Helpers;
using PsycSerial;

namespace TeensyMonitor.FrameTypes
{
    partial class DebugIO : BaseFrame, IAmbRedIR
    {
        public string Text { get; set; }

        public double Ambient => ambient;
        public double Red => red;
        public double IR => ir;


        public DebugIO(IPacket packet)
        {
            if (packet is TextPacket textPacket == false)
                throw new ArgumentException("Packet must be of type TextPacket", nameof(packet));


            Text = textPacket.Text.ToString()!;

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

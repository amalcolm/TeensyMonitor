using PsycSerial;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TeensyMonitor.Helpers;

namespace TeensyMonitor.FrameTypes
{
    public class TestFrame :  BaseFrame, IAmbRedIR
    {
        public string Text { get; set; }

        public double Ambient => ambient;
        public double Red => red;
        public double IR => ir;


        readonly Random random = new();
        public TestFrame(ManagedPacket packet) : base(true)
        {
            ambient = random.Next(0, 1000);
            red = random.Next(1000, 2000);
            ir = random.Next(2000, 3000);

            Text = $"Amb:{ambient}\tRED:{red}\tIR:{ir}";
        }

    }
}

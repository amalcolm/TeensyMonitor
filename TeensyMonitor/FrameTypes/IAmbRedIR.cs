using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeensyMonitor.FrameTypes
{
    public interface IAmbRedIR
    {
        double Ambient { get; }
        double Red { get; }
        double IR { get; }

    }
}

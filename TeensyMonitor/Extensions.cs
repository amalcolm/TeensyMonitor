using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeensyMonitor
{
    internal static class Extensions
    {
        public static void Invoker(this Control control, MethodInvoker action)
        {
            control.Invoke( action );
        }
    }
}

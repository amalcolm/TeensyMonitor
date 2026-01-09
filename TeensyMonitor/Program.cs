using PsycSerial;
using System.Diagnostics;
using TeensyMonitor.Plotter.Helpers;

namespace TeensyMonitor
{
    internal static class Program
    {
        public static readonly TeensySerial? serialPort = new();

        public static bool IsRunning = false;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            Application.ThreadException += (sender, e) =>
            {
                MessageBox.Show(e.Exception.Message);
                serialPort?.Close();
            };

            
            IsRunning = true;
            SocketWatcher.SP = serialPort;



            if (serialPort != null)
            {
                SocketWatcher.StartListening();

                Application.Run(new MainForm());

                SocketWatcher.StopListening();
            }

            serialPort?.Close();
        }

        static void DoTest()
        {
            ZFixer fixer = new(0.1, 5000000, 2.0, 1.0, 5);

                    fixer.Fix(0.0, 0.0);
                    fixer.Fix(0.0, 0.0);
            var a = fixer.Fix(0.0, 0.0);
            var b = fixer.Fix(1.0, 1.0);
            var c = fixer.Fix(2.0, 2.0);
            var d = fixer.Fix(3.0, 50.0); 
            var e = fixer.Fix(4.0, 51.0);
            var f = fixer.Fix(5.0, 52.0);


            Debug.WriteLine($"Fixed values: {a:0.00000}, {b:0.00000}, {c:0.00000}, {d:0.00000}, {e:0.00000}, {f:0.00000}");
        }
    }
}
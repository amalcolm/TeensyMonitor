using PsycSerial;
using PsycSerial.Math;
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

//            ZFixer.DoTest(); return;

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

    }
}
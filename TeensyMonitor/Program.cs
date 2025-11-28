using PsycSerial;

namespace TeensyMonitor
{
    internal static class Program
    {
        public const string Version = "1.0";

        public static readonly TeensySerial serialPort = new(Version);

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
                serialPort.Close();
            };

            IsRunning = true;
           
            Application.Run(new Form1());

            serialPort.Close();
        }
    }
}
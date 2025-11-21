using PsycSerial;

namespace TeensyMonitor
{
    internal static class Program
    {
        public static readonly SerialHelper serialPort = new(CallbackPolicy.Queued);
        public static bool IsRunning = true;
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
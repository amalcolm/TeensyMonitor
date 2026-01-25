using PsycSerial;
using System.Runtime;
using System.Threading.Tasks;
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

                using CancellationTokenSource cts = new();

                Task gcMonitor = Task.Run(() =>
                {
                    try   {Task.Delay(5000, cts.Token).Wait(cts.Token); }
                    catch (OperationCanceledException) { return; }

                    if (!IsRunning) return;

                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
                }, cts.Token);

                Application.Run(new MainForm());

                // cancel the delay/monitor when app exits
                cts.Cancel();

                try   { gcMonitor.Wait();}
                catch (AggregateException) { }

                SocketWatcher.StopListening();
            }

            serialPort?.Close();
        }
    }
}
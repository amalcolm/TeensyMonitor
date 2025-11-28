using PsycSerial;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TeensyMonitor.Plotter.Helpers
{
    internal class SocketWatcher
    {
        private static UdpClient listener = default!;
        private const int Port = 11000;


        private static readonly CancellationTokenSource cancellationTokenSource = new();

        public static TeensySerial? IO { get; set; }

        public static void StartListening()
        {
            if (IO == null) throw new InvalidOperationException("IO must be set before starting the listener.");

            // Run the listener on a background thread
            Task.Run(async () =>
            {
                try
                {
                    listener = new UdpClient(Port);
                    IPEndPoint groupEP = new(IPAddress.Any, Port);

                    while (true)
                    {
                        byte[] bytes = listener.Receive(ref groupEP);
                        string message = Encoding.ASCII.GetString(bytes, 0, bytes.Length);

                        await ProcessCommand(message);
                    }
                }
                catch (SocketException ex)
                {
                    // Handle exceptions, e.g., socket closed
                    Debug.WriteLine(ex.Message);
                }
            }, cancellationTokenSource.Token);
        }

        // This method runs on the UI thread
        private static async Task ProcessCommand(string command)
        {
            switch (command.ToUpper())
            {
                case "DISCONNECT":
                    // Your logic to close the serial port
                    Debug.WriteLine("Received DISCONNECT. Closing port.");
                    if (IO?.IsOpen == true)
                        await IO.CloseAsync();
                    break;
                case "RECONNECT":
                    // Your logic to try reopening the serial port
                    Debug.WriteLine("Received RECONNECT. Attempting to open port.");
                    if (IO?.IsOpen == false)
                        await IO.OpenAsync();
                    break;
            }
        }

        // Call this when your application closes
        public static void StopListening()
        {
            listener?.Close();
            cancellationTokenSource.Cancel();
        }
    }
}

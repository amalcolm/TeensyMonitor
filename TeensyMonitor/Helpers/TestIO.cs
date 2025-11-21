using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace TeensyMonitor.Helpers
{
    /// <summary>
    /// Manages serial port communication with Teensy device
    /// </summary>
    public class TestIO : IDisposable
    {
        const int DEFAULT_BAUDRATE = 57600 * 64;
        public readonly struct Packet(DateTime timestamp, byte[] data, int bytesRead)
        {
            public readonly DateTime Timestamp = timestamp;
            public readonly byte[]   Data      = data;
            public readonly int      BytesRead = bytesRead;
        }

        public  delegate void DataHandler(TestIO sender, Packet packet);

        private DataHandler?                   DataReceived;
        public  event EventHandler<bool>?      ConnectionStateChanged;
        public  event EventHandler<Exception>? ErrorOccurred;

        private SerialPort? _serialPort;
        private CancellationTokenSource? _readCancellation;
        public bool IsOpen { get; private set; }
        public bool FindingStart { get; set; } = true;
        public string? PortName => _serialPort?.PortName;
        public int BaudRate { get; private set; } = DEFAULT_BAUDRATE;
        private const int ReadBufferSize = 4096;

        public TestIO() { }
        public TestIO(string portName, DataHandler dataReceived, int baudRate = DEFAULT_BAUDRATE)
        {
            SetPort(portName, dataReceived, baudRate);
        }
        public bool SetPort(string portName, DataHandler dataReceived, int baudRate = DEFAULT_BAUDRATE)
        {
            // Close existing port if open
            if (IsOpen)
            {
                Close().GetAwaiter().GetResult();
            }

            try
            {
                BaudRate = baudRate;
                _serialPort = new SerialPort(portName)
                {
                    BaudRate = baudRate,
                    DataBits = 8,
                    StopBits = StopBits.One,
                    Parity = Parity.None,
                    WriteTimeout = 1500,
                    ReadTimeout = 1500,
                    ReadBufferSize = ReadBufferSize
                };

                _serialPort.Open();
                IsOpen = _serialPort.IsOpen;

                if (IsOpen)
                {
                    DataReceived = dataReceived;

                    _readCancellation = new CancellationTokenSource();
                    _ = StartReadLoop(_readCancellation.Token);
                    ConnectionStateChanged?.Invoke(this, IsOpen);
                }

                return IsOpen;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
                Debug.WriteLine($"Failed to set port: {ex.Message}");
                return false;
            }
        }

        private async Task StartReadLoop(CancellationToken cancellationToken)
        {
            if (_serialPort == null) return;

            // Discard any buffered data
            _ = _serialPort.ReadExisting();

            byte[] buffer = new byte[ReadBufferSize];

            while (_serialPort?.IsOpen == true && !cancellationToken.IsCancellationRequested && IsOpen)
            {
                try
                {
                    int bytesToRead = _serialPort.BytesToRead;

                    if (bytesToRead == 0)
                    {
                        // Wait a short time (system tick ~16ms) before checking again
                        await Task.Delay(1, cancellationToken);
                        continue;
                    }

                    // Read data into buffer
                    int bytesRead = _serialPort.Read(buffer, 0, Math.Min(bytesToRead, buffer.Length));

                    if (bytesRead > 0)
                    {
                        // Create a copy of the data to pass to the event
                        byte[] data = new byte[bytesRead];
                        Array.Copy(buffer, data, bytesRead);

                        // Create packet and raise event
                        var packet = new Packet(DateTime.Now, data, bytesRead);
                        DataReceived?.Invoke(this, packet);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normal cancellation, just exit the loop
                    break;
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, ex);
                    Debug.WriteLine($"Error in read loop: {ex.Message}");

                    // Delay before retrying
                    await Task.Delay(100, cancellationToken);
                    FindingStart = true;
                }
            }
        }


        public bool Write(string data)
        {
            try
            {
                if (IsOpen && _serialPort != null)
                {
                    _serialPort.Write(data);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
                Debug.WriteLine($"Write error: {ex.Message}");
                return false;
            }
        }


        public bool Write(byte[] data, int offset, int count)
        {
            try
            {
                if (IsOpen && _serialPort != null)
                {
                    _serialPort.Write(data, offset, count);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
                Debug.WriteLine($"Write error: {ex.Message}");
                return false;
            }
        }


        public async Task Close()
        {
            if (!IsOpen) return;

            try
            {
                DataReceived = null;

                // Cancel the read loop
                _readCancellation?.Cancel();

                // Send halt command
                try { Write("Halt"); } catch { /* Ignore errors during close */ }

                await Task.Delay(500);

                // Close the port with a timeout
                using var closeTimeoutCts = new CancellationTokenSource(1000);
                await Task.Run(() =>
                {
                    try { _serialPort?.Close(); }
                    catch (Exception ex) { Debug.WriteLine($"Error closing port: {ex.Message}"); }
                }, closeTimeoutCts.Token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Close error: {ex.Message}");
            }
            finally
            {
                IsOpen = false;
                ConnectionStateChanged?.Invoke(this, IsOpen);
            }
        }

        public void Dispose()
        {
            // Cancel the read loop
            _readCancellation?.Cancel();
            _readCancellation?.Dispose();

            // Close and dispose the serial port
            try
            {
                if (_serialPort != null)
                {
                    if (_serialPort.IsOpen)
                    {
                        _serialPort.Close();
                    }
                    _serialPort.Dispose();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Dispose error: {ex.Message}");
            }

            IsOpen = false;

            // Suppress finalization
            GC.SuppressFinalize(this);
        }
    }
}
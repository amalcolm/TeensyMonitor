using Microsoft.Web.WebView2.Core;
using System.Text.Json;
using System.Threading.Channels;

namespace TeensyMonitor.Caldera
{
    public sealed class Caldera : IDisposable
    {
        public CalderaControl Control { get; }
        public CoreWebView2 WebView { get; }

        public Caldera(CalderaControl control)
        {
            Control = control ?? throw new ArgumentNullException(nameof(control));
            WebView = control.CoreWebView2 ?? throw new InvalidOperationException("WebView2 is not initialized.");

            WebView.WebMessageReceived += WebView_WebMessageReceived;
            WebView.NavigationCompleted += WebView_NavigationCompleted;

            Program.Caldera = this;

            _dataMonitorTask = Task.Run(() => RunAsync(_cts.Token));
        }


        private readonly Channel<IWebMesage> _messageChannel = Channel.CreateBounded<IWebMesage>(new BoundedChannelOptions(4)
            {
                SingleReader = true,
                SingleWriter = false,

                FullMode = BoundedChannelFullMode.DropOldest
            });
        
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _dataMonitorTask;

        private bool _disposed;
        private bool _ready;

        public bool PostWipersChange(WipersChangedMessage wipers)
        {   if (_disposed || !_ready) return false;

            return _messageChannel.Writer.TryWrite(wipers);
        }

        public bool PostVoltagesChange(VoltagesChangedMessage voltages)
        {   if (_disposed || !_ready) return false;
         
            return _messageChannel.Writer.TryWrite(voltages);
        }

        private async Task RunAsync(CancellationToken ct)
        {
            try
            {
                await foreach (IWebMesage message in _messageChannel.Reader.ReadAllAsync(ct))
                {   if (_disposed || !_ready || Control.IsDisposed) return;

                    Control.BeginInvoke(() =>
                    {   if (_disposed || !_ready || Control.IsDisposed) return;

                        SendMessage(message);
                    });
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Normal shutdown.
            }
        }

        private readonly VoltagesChangedMessage lastVoltagesWritten = new();
        private readonly   WipersChangedMessage   lastWipersWritten = new();

        private void SendMessage(IWebMesage message)
        {   if (_disposed || !_ready || Control.IsDisposed || WebView == null) return;

            string? json = null;
            switch (message)
            {
                case WipersChangedMessage wipers:
                    if (wipers.Equals(lastWipersWritten)) return;
                    lastWipersWritten.CopyFrom(wipers);
                    json = JsonSerializer.Serialize(wipers);
                    break;

                case VoltagesChangedMessage voltages:
                    if (voltages.Equals(lastVoltagesWritten)) return;
                    lastVoltagesWritten.CopyFrom(voltages);
                    json = JsonSerializer.Serialize(voltages);
                    break;
            }

            if (json == null) return;

            try { WebView.PostWebMessageAsJson(json); }
            catch (Exception ex)
            {
                // Handle exceptions related to posting messages, such as if the WebView is not ready.
                System.Diagnostics.Debug.WriteLine($"Failed to post message to WebView: {ex.Message}");
            }

        }

        private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {   if (!e.IsSuccess) return;

            WebView.Settings.IsWebMessageEnabled = true;
            _ready = true;
        }

        private void WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = e.TryGetWebMessageAsString();

            if (message == "dataReady")
            {
                // If this is still needed, replace it with another Channel,
                // TaskCompletionSource, or callback depending on what waits for it.
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            WebView.WebMessageReceived -= WebView_WebMessageReceived;
            WebView.NavigationCompleted -= WebView_NavigationCompleted;

            _messageChannel.Writer.TryComplete();

            _cts.Cancel();

            try  { if (! _dataMonitorTask.Wait(1000)) { } }
            catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException)) { }

            _cts.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
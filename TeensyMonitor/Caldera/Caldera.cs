using Microsoft.Web.WebView2.Core;
using PsycSerial;
using PsycSerial.Packets;
using System.Text.Json;
using TeensyMonitor.Plotter.Helpers;

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
            _flushInvoker = FlushPendingMessages;
        }


        [Flags]
        private enum PendingMessage
        {
            None     = 0,
            Wipers   = 1,
            Voltages = 2
        }

        private readonly object _messageLock = new();
        private readonly MethodInvoker _flushInvoker;

        private bool _disposed;
        private bool _ready;
        private int _flushScheduled;
        private PendingMessage _pendingMessages;

        private readonly WipersChangedMessage   _pendingWipers   = new();
        private readonly VoltagesChangedMessage _pendingVoltages = new();
        private readonly WipersChangedMessage   _wipersToSend    = new();
        private readonly VoltagesChangedMessage _voltagesToSend  = new();

        private readonly VoltagesChangedMessage _lastVoltagesWritten = new();
        private readonly WipersChangedMessage   _lastWipersWritten   = new();
        private bool _forceNextWipersWrite;

        public bool PostWipersChange(WipersChangedMessage wipers, bool force = false)
        {
            if (!CanQueueMessages() || (!force && !wipers.IsValid))
                return false;

            lock (_messageLock)
            {
                var alreadyPending = (_pendingMessages & PendingMessage.Wipers) != 0;
                var current = alreadyPending ? _pendingWipers : _lastWipersWritten;

                if (!force && wipers.Equals(current))
                    return false;

                _pendingWipers.CopyFrom(wipers);
                _pendingMessages |= PendingMessage.Wipers;
                _forceNextWipersWrite |= force;
            }

            return ScheduleFlush();
        }

        public bool PostVoltagesChange(VoltagesChangedMessage voltages)
        {
            if (!CanQueueMessages() || !voltages.IsValid)
                return false;

            lock (_messageLock)
            {
                var alreadyPending = (_pendingMessages & PendingMessage.Voltages) != 0;
                var current = alreadyPending ? _pendingVoltages : _lastVoltagesWritten;

                if (voltages.Equals(current))
                    return false;

                _pendingVoltages.CopyFrom(voltages);
                _pendingMessages |= PendingMessage.Voltages;
            }

            return ScheduleFlush();
        }

        private bool CanQueueMessages()
            => !_disposed && _ready && !Control.IsDisposed && Control.IsHandleCreated;

        private bool ScheduleFlush()
        {
            if (!CanQueueMessages()) return false;

            if (Interlocked.Exchange(ref _flushScheduled, 1) != 0) return true;

//            try { Control.BeginInvoke(_flushInvoker); return true; }
           try {  FlushPendingMessages(); return true; }
            catch (InvalidOperationException)
            {
                Interlocked.Exchange(ref _flushScheduled, 0);
                return false;
            }
        }

        private void FlushPendingMessages()
        {
            Interlocked.Exchange(ref _flushScheduled, 0);

            if (!CanQueueMessages()) return;

            PendingMessage pendingMessages;
            bool forceWipersWrite;
            lock (_messageLock)
            {
                pendingMessages = _pendingMessages;
                _pendingMessages = PendingMessage.None;
                forceWipersWrite = (pendingMessages & PendingMessage.Wipers) != 0 && _forceNextWipersWrite;

                if ((pendingMessages & PendingMessage.Wipers  ) != 0)   _wipersToSend.CopyFrom(_pendingWipers  );
                if ((pendingMessages & PendingMessage.Voltages) != 0) _voltagesToSend.CopyFrom(_pendingVoltages);

                if ((pendingMessages & PendingMessage.Wipers) != 0)
                    _forceNextWipersWrite = false;
            }

            if ((pendingMessages & PendingMessage.Wipers)   != 0)   SendWipersMessage(_wipersToSend, forceWipersWrite);

            if ((pendingMessages & PendingMessage.Voltages) != 0) SendVoltagesMessage(_voltagesToSend);
        }

        private void SendWipersMessage(WipersChangedMessage wipers, bool force = false)
        {
            if (!force && wipers.Equals(_lastWipersWritten)) return;

            if (TryPostJson(CalderaJson.CreateWipersChanged(wipers.Wipers)))
                _lastWipersWritten.CopyFrom(wipers);
        }

        private void SendVoltagesMessage(VoltagesChangedMessage voltages)
        {
            if (voltages.Equals(_lastVoltagesWritten)) return;

            if (TryPostJson(CalderaJson.CreateVoltagesChanged(voltages.Voltages)))
                _lastVoltagesWritten.CopyFrom(voltages);
        }

        private bool TryPostJson(string json)
        {
            try
            {
                WebView.PostWebMessageAsJson(json);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to post message to WebView: {ex.Message}");
                return false;
            }
        }

        private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess) { _ready = false; return; }

            WebView.Settings.IsWebMessageEnabled = true;
            _ready = true;
        }

        private void WebView_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                HandleWebMessage(e.WebMessageAsJson);
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to parse WebView message: {ex.Message}");
            }
        }

        private void HandleWebMessage(string json)
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.String)
            {
                HandleWebMessageString(root.GetString());
                return;
            }

            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("type", out var typeElement)
                || typeElement.ValueKind != JsonValueKind.String)
            {
                return;
            }

            switch (typeElement.GetString())
            {
                case "getWipers":
                    Scheduler.RequestWipersRefresh();
                    break;
                case "setWipers":
                    HandleSetWipersMessage(root);
                    break;
                case "setState":
                    HandleSetStateMessage(root);
                    break;
            }
        }

        private static void HandleWebMessageString(string? message)
        {
            if (message == "dataReady")
            {
                // Reserved for a future frontend readiness handshake.
            }
        }

        private static void HandleSetWipersMessage(JsonElement root)
        {
            var message = root.Deserialize<SetWipersMessage>();
            if (message?.Wipers == null) return;

            var wipers = message.Wipers;
            XCMD_SetWipers xCMD = new()
            {
                top    = ClampWiper(wipers.Top),
                bot    = ClampWiper(wipers.Bot),
                mid    = ClampWiper(wipers.Mid),
                offset = ClampWiper(wipers.Offset),
                gain   = ClampWiper(wipers.Gain),
                flags  = XCMD_SetWipers.FLAG_HOLD
            };

            Program.serialPort?.Write(xCMD);
        }

        private static void HandleSetStateMessage(JsonElement root)
        {
            var message = root.Deserialize<SetStateMessage>();
            if (message == null) return;

            XCMD_SetState xCMD = new()
            {
                state = (uint)message.State,
                flags = message.Flags
            };

            Program.serialPort?.Write(xCMD);
        }

        private static byte ClampWiper(int value)
            => (byte)Math.Clamp(value, byte.MinValue, byte.MaxValue);

        public void Dispose()
        {   if (_disposed) return;

            _disposed = true;
            _ready = false;

            WebView.WebMessageReceived  -= WebView_WebMessageReceived;
            WebView.NavigationCompleted -= WebView_NavigationCompleted;

            lock (_messageLock)
                _pendingMessages = PendingMessage.None;

            if (ReferenceEquals(Program.Caldera, this))
                Program.Caldera = null;

            GC.SuppressFinalize(this);
        }

    }
}

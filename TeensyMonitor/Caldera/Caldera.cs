using Microsoft.Web.WebView2.Core;
using PsycSerial;
using PsycSerial.Packets;
using System.Text.Json;
using TeensyMonitor.Plotter.Helpers;

namespace TeensyMonitor.Caldera
{
    public class Caldera : IDisposable
    {
        protected static TeensySerial SP => Program.serialPort ?? throw new InvalidOperationException("Serial port is not initialized.");
        public CalderaControl Control { get; }
        public CoreWebView2 WebView { get; }
        public bool IsRunning => !_disposed && _ready;
        public Caldera(CalderaControl control)
        {
            Control = control ?? throw new ArgumentNullException(nameof(control));
            WebView = control.CoreWebView2 ?? throw new InvalidOperationException("WebView2 is not initialized.");

            WebView.WebMessageReceived += WebView_WebMessageReceived;
            WebView.NavigationCompleted += WebView_NavigationCompleted;

            Program.Caldera = this;
            _wipersPoster = CreateWipersPoster();
            _voltagesPoster = CreateVoltagesPoster();
            _statePoster = CreateStatePoster();

            SP.DataReceived += SP_DataReceived;
        }

        private bool _needsRefresh = true;
        private int _lastState = -1;
        private void SP_DataReceived(IPacket packet)
        {
            if (IsRunning == false) return;

            switch (packet)
            {
                case BlockPacket blockPacket:
                    if (_needsRefresh)
                    {
                        _needsRefresh = false;
                        if (_lastState < 0) _lastState = (int)blockPacket.State;
                        PostStateChange(_lastState);
                    }
                    break;
                case DebugPacket debugPacket:
                    PostStateChange((int)debugPacket.State, force: true);
                    break;
            }
        }

        private bool _disposed;
        private bool _ready;
        private readonly BufferedPoster<WipersChangedMessage> _wipersPoster;
        private readonly BufferedPoster<VoltagesChangedMessage> _voltagesPoster;
        private readonly BufferedPoster<StateChangedMessage> _statePoster;

        public bool PostWipersChange(WipersChangedMessage wipers, bool force = false)
            => _wipersPoster.Post(wipers, force);

        public bool PostVoltagesChange(VoltagesChangedMessage voltages)
            => _voltagesPoster.Post(voltages);

        public bool PostStateChange(int state, bool force = false)
            => _statePoster.Post(new StateChangedMessage(state), force);

        private bool CanPostMessages()
            => !_disposed && _ready && !Control.IsDisposed && Control.IsHandleCreated;

        private BufferedPoster<WipersChangedMessage> CreateWipersPoster()
            => new(
                Control,
                CanPostMessages,
                () => new WipersChangedMessage(),
                static (target, source) => target.CopyFrom(source),
                static message => message.IsValid,
                static message => CalderaJson.CreateWipersChanged(message.Wipers),
                TryPostJson);

        private BufferedPoster<VoltagesChangedMessage> CreateVoltagesPoster()
            => new(
                Control,
                CanPostMessages,
                () => new VoltagesChangedMessage(),
                static (target, source) => target.CopyFrom(source),
                static message => message.IsValid,
                static message => CalderaJson.CreateVoltagesChanged(message.Voltages),
                TryPostJson);

        private BufferedPoster<StateChangedMessage> CreateStatePoster()
            => new(
                Control,
                CanPostMessages,
                () => new StateChangedMessage(),
                static (target, source) => target.CopyFrom(source),
                static _ => true,
                static message => CalderaJson.CreateStateChanged(message),
                TryPostJson);

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
                case "ready":
                    _needsRefresh = true;
                    if (_lastState >= 0)
                    {
                        PostStateChange(_lastState, force: true);
                        _needsRefresh = false;
                    }
                    break;
                case "getWipers":
                    Scheduler.RequestWipersRefresh();
                    break;
                case "setWipers":
                    HandleSetWipersMessage(root);
                    break;
                case "getState":
                    HandleGetSateMessage();
                    break;
                case "setState":
                    HandleSetStateMessage(root);
                    break;
                case "setDebugFlags":
                    HandleSetDebugFlagsMessage(root);
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
                flags  = message.Flags
            };

            Program.serialPort?.Write(xCMD);
        }

        private void HandleGetSateMessage()
        {
            PostStateChange(_lastState < 0 ? unchecked((int)HeadState.UNSET) : _lastState);
        }

        private void HandleSetStateMessage(JsonElement root)
        {
            var message = root.Deserialize<SetStateMessage>();
            if (message == null) return;

            XCMD_SetState xCMD = new()
            {
                state = (uint)message.State,
                flags = message.Flags
            };

            Program.serialPort?.Write(xCMD);
            _lastState = (int)message.State;
        }

        private static void HandleSetDebugFlagsMessage(JsonElement root)
        {
            var message = root.Deserialize<SetDebugFlagsMessage>();
            if (message == null) return;

            XCMD_SetDebugFlags xCMD = new()
            {
                debugFlags = message.Flags
            };

            Program.serialPort?.Write(xCMD);
        }

        private static byte ClampWiper(int value)
            => (byte)Math.Clamp(value, byte.MinValue, byte.MaxValue);

        public void Dispose()
        {   if (_disposed) return;

            _disposed = true;
            _ready = false;

            if (Program.serialPort != null)
                Program.serialPort.DataReceived -= SP_DataReceived;
            WebView.WebMessageReceived  -= WebView_WebMessageReceived;
            WebView.NavigationCompleted -= WebView_NavigationCompleted;

            _wipersPoster.Clear();
            _voltagesPoster.Clear();
            _statePoster.Clear();

            if (ReferenceEquals(Program.Caldera, this))
                Program.Caldera = null;

            GC.SuppressFinalize(this);
        }

    }
}

using PsycSerial;
using System.Windows.Forms;
using TeensyMonitor.MyGLTools.Helpers;
using TeensyMonitor.MyGLTools.UserControls;

namespace TeensyMonitor.DataTools.Controls
{
    public partial class NoiseViewer : MyPlotterBaseWithAxes
    {
        private const int MAX_VERTICES = 4096;

        private static TeensySerial SP => Program.serialPort ?? throw new InvalidOperationException("Serial port is not initialized.");

        private bool IsRunning => !_disposed && _ready;

        private readonly object _lock = new();

        public NoiseViewer()
        {
            BackColor = Color.MistyRose;
            Setup(initAction: Init, shutdownAction: Shutdown);
            SP.DataReceived += SP_DataReceived;

            AxesOptions.DrawAxes = false;
            AxesOptions.DrawGrid = false;
        }


        static readonly float ticksToSeconds = 1.0f / 600_000_000f;
        private void SP_DataReceived(IPacket packet)
        {
            if (IsRunning == false || packet is not DebugPacket dbg) return; if (dbg.Count <= 0) return;

            int max = Math.Min(dbg.Count, vertices.Length);

            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < max; i++)
            {
                float x = dbg.Data[i].StartTick * ticksToSeconds;
                float y = dbg.Data[i].Sample;

                if (float.IsFinite(y))
                {
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }

                vertices[i].Position.X = x;
                vertices[i].Position.Y = y;
            }
            vertexCount = max;

            float lastX = vertices[max - 1].Position.X;

            if (!float.IsFinite(minY) || !float.IsFinite(maxY) || maxY <= minY || lastX <= 0.0f)
                return;

            lock (_lock)
            {
                _vertexBuffer.Set(ref vertices, vertexCount);
                ViewPort = new RectangleF(0, minY, lastX, maxY - minY);
            }
        }


        private readonly MyGLVertexBuffer _vertexBuffer = new(MAX_VERTICES);
        private Vertex[] vertices = new Vertex[MAX_VERTICES];
        private int vertexCount = 0;

        private bool _ready = false;
        private bool _disposed = false;
        protected override void Init()
        {
            base.Init();
            _vertexBuffer.Init();;

            _vertexBuffer.Set(ref vertices, vertexCount);

            ViewPort = new RectangleF(-0.5f, -0.5f, MAX_VERTICES-0.5f, 1000-0.5f);
            MyColour myColour = MyColour.Black;

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].Position.X = i;
                vertices[i].Position.Y = Random.Shared.Next(0, 1000);
                vertices[i].Colour = myColour;
            }
            vertexCount = vertices.Length;
            _vertexBuffer.Set(ref vertices, vertexCount);  // no lock as ready is not yet set.
            _ready = true;
        }

        protected override void Shutdown()
        {
            _ready = false;
            Program.serialPort!.DataReceived -= SP_DataReceived;
            _vertexBuffer.Dispose();
            base.Shutdown();
            _disposed = true;
        }

        protected override void DrawPlots()
        {
            lock (_lock)
                _vertexBuffer.DrawLineStrip();
        }

        protected override void DrawPlotOverlays()
        {
            lock (_lock)
                base.DrawPlotOverlays();
        }
    }
}

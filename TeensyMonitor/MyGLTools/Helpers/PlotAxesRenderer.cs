using TeensyMonitor.MyGLTools.Fonts;

namespace TeensyMonitor.MyGLTools.Helpers
{
    public sealed class PlotAxesRenderer
    {
        private const int MaxXTicks = 16;
        private const int MaxYTicks = 10;
        private const int MaxLabels = MaxXTicks + MaxYTicks;
        private const int VertexCapacity = 256;
        
        public class _Options
        {
            public bool DrawAxes { get; set; } = true;
            public bool DrawGrid { get; set; } = true;
            public bool DrawTicks { get; set; } = true;
        };
        public _Options Options { get; private set; } = new();

        public MyColour AxisColour { get; init; } = Color.FromArgb(180, 32, 32, 32);
        public MyColour GridColour { get; init; } = Color.FromArgb(  8, 32, 32, 32);
        public MyColour TickColour { get; init; } = Color.FromArgb(140, 32, 32, 32);

        private static readonly string XFormat = "G5";
        private static readonly string YFormat = "G5";

        private readonly MyGLVertexBuffer _lineBuffer = new(VertexCapacity);
        private Vertex[] _vertices = new Vertex[VertexCapacity];

        private readonly TextBlock[] _xLabels = new TextBlock[MaxXTicks];
        private readonly TextBlock[] _yLabels = new TextBlock[MaxYTicks];
        private readonly TextBlock[] _labels = new TextBlock[MaxLabels];

        private RectangleF _lastViewPort = RectangleF.Empty;
        private Size _lastClientSize = Size.Empty;
        private int _labelCount;
        private bool _ready;

        public bool Visible { get; set; } = true;
        public bool ShowGrid { get; set; } = true;

        public void Init(FontFile? font)
        {
            _lineBuffer.Init();  

            for (int i = 0; i < _xLabels.Length; i++)
                _xLabels[i] = new TextBlock("0", 0, 0, font, TextAlign.Right, XFormat);

            for (int i = 0; i < _yLabels.Length; i++)
                _yLabels[i] = new TextBlock("0", 0, 0, font, TextAlign.Right, YFormat);

            _ready = true;
        }

        public void Shutdown()
        {
            _ready = false;
            _lineBuffer.Dispose();

            for (int i = 0; i < _xLabels.Length; i++)
                _xLabels[i]?.Dispose();

            for (int i = 0; i < _yLabels.Length; i++)
                _yLabels[i]?.Dispose();
        }

        public void RenderLines(RectangleF viewPort, Size clientSize)
        {
            if (!_ready || !Visible) return;

            if (!IsUsable(viewPort, clientSize))
            {
                _labelCount = 0;
                return;
            }

            if (NeedsRebuild(viewPort, clientSize))
                Rebuild(viewPort, clientSize);

            _lineBuffer.DrawLines();
        }

        public void RenderText(FontRenderer fontRenderer)
        {
            if (!_ready || !Visible || _labelCount == 0) return;

            fontRenderer.RenderText(_labels, _labelCount);
        }

        private bool NeedsRebuild(RectangleF viewPort, Size clientSize)
        {
            const float epsilon = 0.000001f;

            if (!_lastClientSize.Equals(clientSize)) return true;

            return Math.Abs(_lastViewPort.X      - viewPort.X     ) > epsilon
                || Math.Abs(_lastViewPort.Y      - viewPort.Y     ) > epsilon
                || Math.Abs(_lastViewPort.Width  - viewPort.Width ) > epsilon
                || Math.Abs(_lastViewPort.Height - viewPort.Height) > epsilon;
        }

        private void Rebuild(RectangleF viewPort, Size clientSize)
        {
            _lastViewPort = viewPort;
            _lastClientSize = clientSize;
            _labelCount = 0;

            float xMin = viewPort.Left;
            float xMax = viewPort.Right;
            float yMin = viewPort.Top;
            float yMax = viewPort.Bottom;

            float xRange = xMax - xMin;
            float yRange = yMax - yMin;
            float xTickWorld = xRange * 6.0f / Math.Max(1, clientSize.Width);
            float yTickWorld = yRange * 6.0f / Math.Max(1, clientSize.Height);

            int vertexCount = 0;

            AddLine(ref vertexCount, xMin, yMin, xMax, yMin, AxisColour);
            AddLine(ref vertexCount, xMin, yMin, xMin, yMax, AxisColour);

            BuildXTicks(ref vertexCount, xMin, xMax, yMin, yMax, yTickWorld, clientSize);
            BuildYTicks(ref vertexCount, xMin, xMax, yMin, yMax, xTickWorld, clientSize);

            _lineBuffer.Set(ref _vertices, vertexCount);
        }

        private void BuildXTicks(ref int vertexCount, float xMin, float xMax, float yMin, float yMax, float tickWorld, Size clientSize)
        {
            if (Options.DrawTicks == false) return;

            float step = NiceStep(xMax - xMin, 8);
            if (!float.IsFinite(step) || step <= 0.0f) return;

            float first = MathF.Ceiling(xMin / step) * step;
            int tickCount = 0;

            for (float x = first; x <= xMax && tickCount < MaxXTicks; x += step)
            {
                if (ShowGrid) AddLine(ref vertexCount, x, yMin, x, yMax, GridColour);
                AddLine(ref vertexCount, x, yMin, x, yMin + tickWorld, TickColour);

                TextBlock label = _xLabels[tickCount++];
                label.X = WorldToScreenX(x, xMin, xMax, clientSize.Width) + 14.0f;
                label.Y = 4.0f;
                label.SetValue(x, XFormat);
                _labels[_labelCount++] = label;
            }
        }

        private void BuildYTicks(ref int vertexCount, float xMin, float xMax, float yMin, float yMax, float tickWorld, Size clientSize)
        {
            if (Options.DrawTicks == false) return;

            float step = NiceStep(yMax - yMin, 6);
            if (!float.IsFinite(step) || step <= 0.0f) return;

            float first = MathF.Ceiling(yMin / step) * step;
            int tickCount = 0;

            for (float y = first; y <= yMax && tickCount < MaxYTicks; y += step)
            {
                if (ShowGrid) AddLine(ref vertexCount, xMin, y, xMax, y, GridColour);
                AddLine(ref vertexCount, xMin, y, xMin + tickWorld, y, TickColour);

                TextBlock label = _yLabels[tickCount++];
                label.X = 58.0f;
                label.Y = WorldToScreenY(y, yMin, yMax, clientSize.Height) - 8.0f;
                label.SetValue(y, YFormat);
                _labels[_labelCount++] = label;
            }
        }

        private void AddLine(ref int count, float x1, float y1, float x2, float y2, MyColour colour)
        {
            if (count + 2 > _vertices.Length) return;

            _vertices[count++] = new Vertex(x1, y1, 0.0f, colour);
            _vertices[count++] = new Vertex(x2, y2, 0.0f, colour);
        }

        private static float NiceStep(float range, int desiredTicks)
        {
            if (!float.IsFinite(range) || range <= 0.0f) return 0.0f;

            float rawStep = range / Math.Max(1, desiredTicks - 1);
            float exponent = MathF.Floor(MathF.Log10(rawStep));
            float magnitude = MathF.Pow(10.0f, exponent);
            float fraction = rawStep / magnitude;

            float niceFraction =
                fraction <= 1.0f ? 1.0f :
                fraction <= 2.0f ? 2.0f :
                fraction <= 5.0f ? 5.0f : 10.0f;

            return niceFraction * magnitude;
        }

        private static bool IsUsable(RectangleF viewPort, Size clientSize)
            => clientSize.Width > 0
            && clientSize.Height > 0
            && float.IsFinite(viewPort.Left)
            && float.IsFinite(viewPort.Right)
            && float.IsFinite(viewPort.Top)
            && float.IsFinite(viewPort.Bottom)
            && viewPort.Width > 0.0f
            && viewPort.Height > 0.0f;

        private static float WorldToScreenX(float x, float xMin, float xMax, int width)
            => (x - xMin) * width / (xMax - xMin);

        private static float WorldToScreenY(float y, float yMin, float yMax, int height)
            => (y - yMin) * height / (yMax - yMin);
    }
}

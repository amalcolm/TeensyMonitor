using OpenTK.Graphics.OpenGL4;
using TeensyMonitor.Plotter.UserControls;

namespace TeensyMonitor.Plotter.Helpers
{
     class MySubplot : MyGLViewport
    {
        private readonly MyGLVertexBuffer _gridBuffer = new(128);
        private bool _gridDirty = true;
        
        private int _colorLoc = -1;

        public int GridDivisions { get; set; } = 20;
        public bool UniformGrid { get; set; } = false;

        public MySubplot(MyPlotterBase myPlotter) : base(myPlotter)
        {
            Margin = 20;
            InRect = new RectangleF(0f, 0f, 0.5f, 0.35f);
            OutRect = new RectangleF(0f, 1000000f, 20f, 4000000f);
        }

        public override void Init()
        {
            base.Init();

            _colorLoc = GL.GetUniformLocation(_myPlotter.GetPlotShader(), "uColor");

            _gridBuffer.Init();
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _gridBuffer.Dispose();
        }

        /// <summary>
        /// Overrides base OutRect to auto-invalidate grid when data range changes.
        /// Feel free to set this directly – it's your RectangleF for X/Y min/max.
        /// Left = XMin, Bottom = YMin, Right = XMax, Top = YMax.
        /// </summary>
        public new RectangleF OutRect
        {
            get => base.OutRect;
            set
            {
                if (!value.Equals(base.OutRect))
                {
                    base.OutRect = value;
                    _gridDirty = true;
                }
            }
        }

        public void Render(MyGLVertexBuffer waveBuffer)
        {
            if (waveBuffer.VertexCount <= 0) return;

            Set();  // full base magic: viewport, scissor, ortho projection from OutRect

            if (_gridDirty)
                if (UniformGrid)
                    BuildGrid();
                else
                    BuildGrid(waveBuffer);
            
            // Dim grey grid
            GL.Uniform4(_colorLoc, 0.35f, 0.35f, 0.35f, 0.28f);
            _gridBuffer.DrawLines();

            // Teal/cyan waveform
            GL.Uniform4(_colorLoc, 0.0f, 0.3f, 0.3f, 1.0f);
            waveBuffer.DrawLineStrip();

            Reset();  // clean restore to parent viewport
        }

        private void BuildGrid()
        {
            var data = OutRect;
            float xMin = data.Left;
            float xMax = data.Right;
            float yMin = data.Bottom;
            float yMax = data.Top;

            float xStep = (xMax - xMin) / GridDivisions;

            int verticalPairs = GridDivisions + 1;
            int vertexCount = verticalPairs * 2 + 4;  // verticals + top/bottom horiz

            float[] grid = new float[vertexCount * 3];
            int idx = 0;

            // Vertical lines (evenly spaced across current X range)
            for (int i = 0; i <= GridDivisions; i++)
            {
                float x = xMin + i * xStep;
                grid[idx++] = x; grid[idx++] = yMin; grid[idx++] = 0f;
                grid[idx++] = x; grid[idx++] = yMax; grid[idx++] = 0f;
            }

            // Top horizontal
            grid[idx++] = xMin; grid[idx++] = yMax; grid[idx++] = 0f;
            grid[idx++] = xMax; grid[idx++] = yMax; grid[idx++] = 0f;

            // Bottom horizontal
            grid[idx++] = xMin; grid[idx++] = yMin; grid[idx++] = 0f;
            grid[idx++] = xMax; grid[idx++] = yMin; grid[idx++] = 0f;

            _gridBuffer.Set(ref grid, vertexCount);

            _gridDirty = false;
        }

        private void BuildGrid(MyGLVertexBuffer waveBuffer)
        {
            var data = OutRect;
            float xMin = data.Left;
            float xMax = data.Right;
            float yMin = data.Bottom;
            float yMax = data.Top;

            GridDivisions = waveBuffer.LatestXCount + 1;
            var spanX = waveBuffer.LatestX;
            var numX = waveBuffer.LatestXCount;

            // how many verticals we *want* (same as before), but never more than we have samples
            int verticalLines = GridDivisions + 1;
            if (verticalLines <= 0) return;

            int vertexCount = verticalLines * 2 + 4;   // verticals + top/bottom horiz
            float[] grid = new float[vertexCount * 3];
            int idx = 0;

            // Left vertical
            grid[idx++] = xMin; grid[idx++] = yMin; grid[idx++] = 0f;
            grid[idx++] = xMin; grid[idx++] = yMax; grid[idx++] = 0f;

            for (int i = 0; i < numX; i++)
            {
                float x = spanX[i];

                grid[idx++] = x; grid[idx++] = yMin; grid[idx++] = 0f;
                grid[idx++] = x; grid[idx++] = yMax; grid[idx++] = 0f;
            }

            // Right vertical
            grid[idx++] = xMax; grid[idx++] = yMin; grid[idx++] = 0f;
            grid[idx++] = xMax; grid[idx++] = yMax; grid[idx++] = 0f;

            // Top horizontal
            grid[idx++] = xMin; grid[idx++] = yMax; grid[idx++] = 0f;
            grid[idx++] = xMax; grid[idx++] = yMax; grid[idx++] = 0f;

            // Bottom horizontal
            grid[idx++] = xMin; grid[idx++] = yMin; grid[idx++] = 0f;
            grid[idx++] = xMax; grid[idx++] = yMin; grid[idx++] = 0f;

            _gridBuffer.Set(ref grid, vertexCount);
        }

    }
}
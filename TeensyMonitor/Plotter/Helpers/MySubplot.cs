using OpenTK.Graphics.OpenGL4;
using PsycSerial;
using TeensyMonitor.Plotter.UserControls;

namespace TeensyMonitor.Plotter.Helpers
{
     class MySubplot : MyGLViewport
    {
        private readonly MyGLVertexBuffer _waveBuffer_C0 = new(1024);
        private readonly MyGLVertexBuffer _waveBuffer_PG = new(1024);
        private readonly MyGLVertexBuffer _waveBuffer_EV = new(1024);

        private readonly double _C0_Scale = Config.C0to1024;
        private readonly double _PG_Scale = 1.0;

        private readonly MyGLVertexBuffer _gridBuffer = new(4096);
        private bool _gridDirty = true;
        
        private int _colorLoc = -1;

        public int GridDivisions { get; set; } = (int)Math.Round(Config.STATE_DURATION_uS/1000.0f);
        public bool UniformGrid { get; set; } = false;

        public MySubplot(MyPlotterBase myPlotter) : base(myPlotter)
        {
            base.Margin = 20;
            base.InRect = new RectangleF(0f, 0f, 0.5f, 0.35f);
            this.OutRect = new RectangleF(0f, -50f, Config.STATE_DURATION_uS/1000.0f, 1050f);
        }

        public override void Init()
        {
            base.Init();

            _colorLoc = GL.GetUniformLocation(_myPlotter.GetPlotShader(), "uColor");

            _waveBuffer_C0.Init();
            _waveBuffer_PG.Init();
            _waveBuffer_EV.Init();
            _gridBuffer.Init();
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _gridBuffer.Dispose();
            _waveBuffer_C0.Dispose();
            _waveBuffer_PG.Dispose();
            _waveBuffer_EV.Dispose();
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

        public void SetBlock(BlockPacket block)
        {
            _waveBuffer_C0.SetBlock(block, FieldEnum.C0            , _C0_Scale);
            _waveBuffer_PG.SetBlock(block, FieldEnum.postGainSensor, _PG_Scale);
            _waveBuffer_EV.SetBlock(block, FieldEnum.Events        , 1.0);
        }

        public void Render()
        {
            if (_waveBuffer_C0.VertexCount <= 0) return;

            SetupViewport();  // viewport, scissor, ortho projection from OutRect

            if (_gridDirty)
                if (UniformGrid)
                    BuildGrid();
                else
                    BuildGrid(_waveBuffer_C0);
            
            // Dim grey grid
            GL.Uniform4(_colorLoc, 0.35f, 0.35f, 0.35f, 0.28f);
            _gridBuffer.DrawLines();

            // Teal/cyan waveform
            GL.Uniform4(_colorLoc, 0.0f, 0.3f, 0.3f, 1.0f);
            _waveBuffer_C0.DrawLineStrip();

            GL.Uniform4(_colorLoc, 1.0f, 0.0f, 0.0f, 1.0f);
            _waveBuffer_PG.DrawLineStrip();

            ResetViewport(_myPlotter.getPlotTransform());  // clean restore to parent viewport
        }





        #region Build Grid Methods
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

            Vertex[] grid = new Vertex[vertexCount];
            int idx = 0;

            // Vertical lines (evenly spaced across current X range)
            for (int i = 0; i <= GridDivisions; i++)
            {
                float x = xMin + i * xStep;
                grid[idx++] = new Vertex(x, yMin, 0f);
                grid[idx++] = new Vertex(x, yMax, 0f);
            }

            // Top horizontal
            grid[idx++] = new Vertex(xMin, yMax, 0f);
            grid[idx++] = new Vertex(xMax, yMax, 0f);

            // Bottom horizontal
            grid[idx++] = new Vertex(xMin, yMin, 0f);
            grid[idx++] = new Vertex(xMax, yMin, 0f);

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

            waveBuffer.getLatestX(out var spanX, out var numX);

            GridDivisions = numX + 1;

            // how many verticals we *want* (same as before), but never more than we have samples
            int verticalLines = GridDivisions + 1;
            if (verticalLines <= 0) return;

            int vertexCount = verticalLines * 2 + 4;   // verticals + top/bottom horiz
            Vertex[] grid = new Vertex[vertexCount * 3];
            int idx = 0;

            // Left vertical
            grid[idx++] = new Vertex(xMin, yMin, 0f);
            grid[idx++] = new Vertex(xMin, yMax, 0f);

            for (int i = 0; i < numX; i++)
            {
                float x = spanX[i];

                grid[idx++] = new Vertex(x, yMin, 0f);
                grid[idx++] = new Vertex(x, yMax, 0f);
            }

            // Right vertical
            grid[idx++] = new Vertex(xMax, yMin, 0f);
            grid[idx++] = new Vertex(xMax, yMax, 0f);

            // Top horizontal
            grid[idx++] = new Vertex(xMin, yMax, 0f);
            grid[idx++] = new Vertex(xMax, yMax, 0f);
            // Bottom horizontal
            grid[idx++] = new Vertex(xMin, yMin, 0f);
            grid[idx++] = new Vertex(xMax, yMin, 0f);

            _gridBuffer.Set(ref grid, vertexCount);

            _gridDirty = false;
        }
        #endregion
    }

}
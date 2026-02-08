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
        
        
        public int GridDivisions { get; set; } = (int)Math.Round(Config.STATE_DURATION_uS/1000000.0f);
        public bool UniformGrid { get; set; } = false;

        public MySubplot(MyPlotterBase myPlotter) : base(myPlotter)
        {
            base.Margin = 20;
            base.InRect = new RectangleF(0f, 0f, 0.5f, 0.35f);
            this.OutRect = new RectangleF(0f, -10f, Config.STATE_DURATION_uS/1000000.0f, 1050f);
        }

        public override void Init()
        {
            base.Init();

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
            
            _gridBuffer.DrawLines();

            _waveBuffer_C0.DrawLineStrip();
            _waveBuffer_PG.DrawLineStrip();
            _waveBuffer_EV.DrawLines();
            ResetViewport(_myPlotter.getPlotTransform());  // clean restore to parent viewport
        }





        #region Build Grid Methods
        Color _gridColor = Color.FromArgb(50, 64, 64, 64);
        float[] xs = new float[16];
        private void BuildGrid(MyGLVertexBuffer? waveBuffer = null)
        {
            var r = OutRect;
            float xMin = r.Left, xMax = r.Right, yMin = r.Top, yMax = r.Bottom; // note: Y inverted in GL coords

            // 1) Get the X positions for vertical lines
            if (waveBuffer is null)
            {
                int div = Math.Max(1, GridDivisions);
                if (xs.Length < div + 1)
                    xs = new float[div + 1];

                float step = (xMax - xMin) / div;
                for (int i = 0; i <= div; i++)
                    xs[i] = xMin + i * step;

                _gridDirty = false;  // only when uniform grid
            }
            else
            {
                var span = waveBuffer.GetLatestX();
                if (xs.Length < span.Length + 2)
                    xs = new float[span.Length + 2];

                xs[0] = xMin;
                for (int i = 0; i < span.Length; i++)
                    xs[i + 1] = span[i];
                xs[^1] = xMax;

                GridDivisions = xs.Length - 1;
            }

            // 2) Allocate and emit geometry
            int vertexCount = xs.Length * 2 + 4; // verticals + top/bottom
            Vertex[] grid = new Vertex[vertexCount];
            int idx = 0;

            foreach (float x in xs)
            {
                grid[idx++] = new Vertex(x, yMin + 80.0f, 0f, _gridColor);
                grid[idx++] = new Vertex(x, yMax        , 0f, _gridColor);
            }

            // top
            grid[idx++] = new Vertex(xMin, yMax, 0f, _gridColor);
            grid[idx++] = new Vertex(xMax, yMax, 0f, _gridColor);
            // bottom
            grid[idx++] = new Vertex(xMin, yMin, 0f, _gridColor);
            grid[idx++] = new Vertex(xMax, yMin, 0f, _gridColor);

            _gridBuffer.Set(ref grid, idx);
        }
        #endregion
    }

}
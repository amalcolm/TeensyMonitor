using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using PsycSerial;
using System.Diagnostics;
using TeensyMonitor.Plotter.UserControls;

namespace TeensyMonitor.Plotter.Helpers
{
    public class MyPlot
    {
        public float  LastX    { get; private set; } = 0;
        public float  LastY    { get; private set; } = 0;
        public double Yscale   { get; set;         } = 0.0; // overridden by MyPlotter if 0.0.  If not overridden, use 1.0.
        public Color  Colour   { get; set;         } = MyColours.GetNextColour();
        public double XCounter { get; set;         } = -Math.Pow(2, 20) + 2; // X value counter, for signals without timestamps
                                                                             // Starts at a large negative value to avoid issues with float precision with ++;

        public DataSelector? Selector { get; set; }
        
        
        private MyChart? _parentChart;
        private readonly object _lock = new();

        // OpenGL handles
        private MyGLVertexBuffer _bufMainPlot = default!;
        private MyGLVertexBuffer _bufSubPlot = new(1024) { ChannelScale = 1.0f };
        private MyGLVertexBuffer _bufSubPlotGrid = new(128);

        // Configuration
        private readonly int _bufferCapacity; // The total size of our vertex buffer
    

        public MyPlot(int historyLength, MyGLControl myGL)
        {
            if (myGL is MyChart chart)
                _parentChart = chart;

            // Make the buffer larger than the history to avoid copying every single frame
            _bufferCapacity = historyLength * 8 + Random.Shared.Next(0, historyLength);  // stagger refreshes

            _bufMainPlot = new MyGLVertexBuffer(_bufferCapacity) { WindowSize = historyLength };

            myGL.Setup(initAction:Init, shutdownAction:Shutdown);
        }


        private void Init()
        {
            _bufMainPlot.Init();
            _bufSubPlot.Init();
            _bufSubPlotGrid.Init();
        }

        public void Add(double y) => Add(XCounter++, y);

        private bool first = true;
        /// <summary>
        /// Adds a new Y data point to the plot. The X value is automatically incremented.
        /// </summary>
        public void Add(double x, double y)
        {
            if (first) { first = false; return; }

            double scale = Yscale == 0.0 ? 1.0 : Yscale;

            lock (_lock)
            {
                float fX = (float)x;
                float fY = (float)(y * scale);

                _bufMainPlot.AddVertex(fX, fY, 0.0f);

                LastX = fX;
                LastY = fY;
            }
        }

        float[] latestBlock = [];

        public void Add(BlockPacket packet, DataSelector? selector = null)
        {
            if (first) { first = false; return; }

            if (latestBlock.Length < packet.Count * 3)
                latestBlock = new float[packet.Count * 3];

            for (int i = 0; i < packet.Count; i++)
            {
                int baseIndex = i * 3;  

                latestBlock[baseIndex    ] = (float)packet.BlockData[i].StateTime * 1000.0f;  // milliseconds for subplot visibility
                latestBlock[baseIndex + 1] = (float)packet.BlockData[i].Channel[0];
                latestBlock[baseIndex + 2] = 0.0f;
            }

            double scale = Yscale == 0.0 ? 1.0 : Yscale;                     // debugQueue.Enqueue(new DebugPoint { SW = sw.Elapsed.TotalMilliseconds, Timestamp = packet.BlockData[0].TimeStamp });

            if (selector == null)  // check data
                for (int i = 1; i < packet.Count; i++)
                {
                    ref var item = ref packet.BlockData[i];

                    uint c1 = item.Channel[0];
                    uint c0 = packet.BlockData[i - 1].Channel[0];
                    uint diff = (c0 > c1) ? (c0 - c1) : (c1 - c0);

                    if (c0 < 0x1000 && c0 > 0xFEFFFFF)
                    {   // 0's and 0xFF.... are invalid data
                        // Debug.WriteLine($"Skipping c0: 0x{c0:X8}");
                    }
                    else
                    if (c1 < 0x1000 && c1 > 0xFEFFFFF)
                    {
                        // skip diff check as this will be caught next iteration
                    }
                    else
                    if (diff > 0x00400000)
                        Debug.WriteLine($"c0: 0x{c0:X8}, c1: 0x{c1:X8}");
                }
           
            // display all data, regardless of checks
            _bufMainPlot.AddBlock(ref packet, selector);

            var lastData = packet.BlockData[packet.Count - 1];
            LastX = (float) lastData.TimeStamp;
            LastY = (float)(lastData.Channel[0] * _bufMainPlot.ChannelScale);
        }

        /// <summary>
        /// Renders the plot. Assumes the correct shader program is already active.
        /// </summary>
        public void Render() // No need for view parameters here now
        {
            _bufMainPlot.DrawLineStrip();

            RenderSubPlot(latestBlock.Length);
        }


        /// <summary>
        /// Releases the GPU resources (VBO and VAO).
        /// </summary>
        public void Shutdown()
        {
            _bufMainPlot.Dispose();
            _bufSubPlot.Dispose();
            _bufSubPlotGrid.Dispose();
        }

        private const int margin = 20;
        private const float ymin = 1000000.0f;
        private const float ymax = 5000000.0f;

        private readonly int[] _viewport = new int[4];
        private bool _buildGrid = true;

        private float[] block = new float[3072];
        private void RenderSubPlot(int count)
        {
            if (_parentChart == null) return;

            lock (_lock)
                Array.Copy(latestBlock, 0, block, 0, Math.Min(count, block.Length));

            _bufSubPlot.Set(ref block, count);

            GL.GetInteger(GetPName.Viewport, _viewport);
            int vpWidth  = _viewport[2];
            int vpHeight = _viewport[3];

            float sliceSize = vpWidth * 0.02f;

            int subW = (int)(sliceSize * 20.00f);
            int subH = (int)(vpHeight  *  0.25f);

            GL.Scissor (margin, margin, subW, subH);
            GL.Enable  (EnableCap.ScissorTest);
            GL.Viewport(margin, margin, subW, subH);

            // the x-ordinates are in milliseconds for the subplot, max 20ms
            var transform = Matrix4.CreateOrthographicOffCenter(0f, 20.0f, ymin, ymax, -1f, 1f);

            int plotShader   = _parentChart.GetPlotShader();
            int colorLoc     = GL.GetUniformLocation(plotShader, "uColor");
            int transformLoc = GL.GetUniformLocation(plotShader, "uTransform");
            GL.UniformMatrix4(transformLoc, false, ref transform);

            // --- Draw subplot grid ---
            if (_buildGrid)
                BuildSubplotGrid();

            GL.Uniform4(colorLoc, 0.35f, 0.35f, 0.35f, 0.28f);
            _bufSubPlotGrid.DrawLines();

            // --- Draw subplot waveform ---
            GL.Uniform4(colorLoc, 0.0f, 0.3f, 0.3f, 1.0f);
            _bufSubPlot.DrawLineStrip();

            // --- Cleanup ---
            GL.Disable(EnableCap.ScissorTest);
            GL.Viewport(0, 0, vpWidth, vpHeight);
        }


        private void BuildSubplotGrid()
        {
            if (_parentChart == null) return;

            int maxDivisor = 20;

            int gridVertexCount = ((maxDivisor + 1) * 2) + 4;
            float[] grid = new float[gridVertexCount * 3];
            int idx = 0;

            // Vertical lines
            for (int i = 0; i <= maxDivisor; i++)
            {
                grid[idx++] = i; grid[idx++] = ymin; grid[idx++] = 0.0f;
                grid[idx++] = i; grid[idx++] = ymax; grid[idx++] = 0.0f;
            }

            // Horizontal lines
            grid[idx++] = 0.0f;       grid[idx++] = ymax; grid[idx++] = 0.0f;
            grid[idx++] = maxDivisor; grid[idx++] = ymax; grid[idx++] = 0.0f;
            grid[idx++] = 0.0f;       grid[idx++] = ymin; grid[idx++] = 0.0f;
            grid[idx++] = maxDivisor; grid[idx++] = ymin; grid[idx++] = 0.0f;

            _bufSubPlotGrid.Set(ref grid, grid.Length);
            
            _buildGrid = false;
        }


    }
}
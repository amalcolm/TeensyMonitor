using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using PsycSerial;
using System.Diagnostics;
using TeensyMonitor.Plotter.UserControls;
using static TeensyMonitor.Plotter.Helpers.ShaderManager;

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
        
        
        private MyPlotterBase _parentControl;
        private readonly object _lock = new();

        // OpenGL handles
        private MyGLVertexBuffer _bufMainPlot = default!;
        private MyGLVertexBuffer _bufSubPlot = new(1024) { ChannelScale = 1.0f };
        private MyGLVertexBuffer _bufSubPlotGrid = new(128);

        // Configuration
        private int _colorLoc = -1;  // location of uColor uniform in shader


        public MyPlot(int historyLength, MyPlotterBase myPlotter)
        {
            _parentControl = myPlotter;

            // Make the buffer larger than the history to avoid copying every single frame
            int _bufferCapacity = historyLength * 8 + Random.Shared.Next(0, historyLength);  // stagger refreshes

            _bufMainPlot = new MyGLVertexBuffer(_bufferCapacity) { WindowSize = historyLength };

            _subPlotViewport = new GLViewport(myPlotter)
            {
                Margin  = _subPlot_Margin,
                InRect  = new RectangleF(0, 0, 0.5f, 0.35f),
                OutRect = new RectangleF(0, _subPlot_Ymin, 20.0f, _subPlot_Ymax - _subPlot_Ymin)
            };

            myPlotter.Setup(initAction:Init, shutdownAction:Shutdown);
        }


        private void Init()
        {
            _colorLoc = GL.GetUniformLocation(_parentControl.GetPlotShader(), "uColor");

            _subPlotViewport.Init();

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
        int packetCount = 0;

        public void Add(BlockPacket packet, DataSelector? selector = null)
        {
            if (first) { first = false; return; }

            if (latestBlock.Length < packet.Count * 3)
                latestBlock = new float[packet.Count * 3];

            packetCount = packet.Count;

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

                    if (c0 < 0x1000 || c0 > 0xFEFFFFF)
                    {   // 0's and 0xFF.... are invalid data
                        // Debug.WriteLine($"Skipping c0: 0x{c0:X8}");
                    }
                    else
                    if (c1 < 0x1000 || c1 > 0xFEFFFFF)
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

            RenderSubPlot();
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



        private bool _buildGrid = true;
        const int _subPlot_Margin = 20;
        const float _subPlot_Ymin = 1000000.0f;
        const float _subPlot_Ymax = 5000000.0f;

        private float[] block = new float[3072];

        private GLViewport _subPlotViewport;

        private void RenderSubPlot()
        {
            if (packetCount <= 0) return;

            lock (_lock)
                Array.Copy(latestBlock, 0, block, 0, Math.Min(packetCount * 3, block.Length));

            _bufSubPlot.Set(ref block, packetCount);

            _subPlotViewport.Set();

            // --- Draw subplot grid ---
            if (_buildGrid)
                BuildSubplotGrid();


            GL.Uniform4(_colorLoc, 0.35f, 0.35f, 0.35f, 0.28f);
            _bufSubPlotGrid.DrawLines();

            // --- Draw subplot waveform ---
            GL.Uniform4(_colorLoc, 0.0f, 0.3f, 0.3f, 1.0f);
            _bufSubPlot.DrawLineStrip();

            _subPlotViewport.Reset();
        }



        private void BuildSubplotGrid()
        {
            int maxDivisor = 20;

            int gridVertexCount = ((maxDivisor + 1) * 2) + 4;
            float[] grid = new float[gridVertexCount * 3];
            int idx = 0;

            RectangleF subPlotRect = _subPlotViewport.OutRect;
            float yMin = subPlotRect.Bottom;
            float yMax = subPlotRect.Top;

            // Vertical lines
            for (int i = 0; i <= maxDivisor; i++)
            {
                grid[idx++] = i; grid[idx++] = yMin; grid[idx++] = 0.0f;
                grid[idx++] = i; grid[idx++] = yMax; grid[idx++] = 0.0f;
            }

            // Horizontal lines
            grid[idx++] = 0.0f;       grid[idx++] = yMax; grid[idx++] = 0.0f;
            grid[idx++] = maxDivisor; grid[idx++] = yMax; grid[idx++] = 0.0f;
            grid[idx++] = 0.0f;       grid[idx++] = yMin; grid[idx++] = 0.0f;
            grid[idx++] = maxDivisor; grid[idx++] = yMin; grid[idx++] = 0.0f;

            _bufSubPlotGrid.Set(ref grid, gridVertexCount);

            _buildGrid = false;
        }


    }
}
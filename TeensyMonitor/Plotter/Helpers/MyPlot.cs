using OpenTK.Graphics.OpenGL4;
using PsycSerial;
using System.Diagnostics;
using TeensyMonitor.Plotter.UserControls;

namespace TeensyMonitor.Plotter.Helpers
{
    public class MyPlot
    {
        public float  LastX    { get; private set; } = 0;
        public double Yscale   { get; set;         } = 0.0; // overridden by MyPlotter if 0.0.  If not overridden, use 1.0.
        public Color  Colour   { get; set;         } = MyColours.GetNextColour();
        public double XCounter { get; set;         } = -Math.Pow(2, 20) + 2; // X value counter, for signals without timestamps
                                                                             // Starts at a large negative value to avoid issues with float precision with ++;
        public bool   Visible  { get; set;         } = true;
        public DataSelector? Selector { get; set; }

        
        
        private readonly object _lock = new();

        public string DBG { get; set; } = string.Empty;
        
        private MyGLVertexBuffer _bufMainPlot = default!;
        private MyGLVertexBuffer _bufSubPlot = new(4096) { ChannelScale = 1.0f };
        private MyGLVertexBuffer _bufSubPlotGrid = new(4096);

        private MySubplot _subPlot = default!;

        public int NumPoints => _bufMainPlot.Count;


        public MyPlot(int historyLength, MyPlotterBase myPlotter)
        {
            // Make the buffer larger than the history to avoid copying every single frame
            int _bufferCapacity = historyLength * 8 + Random.Shared.Next(0, historyLength);  // stagger refreshes

            _bufMainPlot = new MyGLVertexBuffer(_bufferCapacity) { WindowSize = historyLength };

            _subPlot = new MySubplot(myPlotter)
            {
                Margin  = 10,
                InRect  = new RectangleF(0, 0, 0.5f, 0.35f),
                OutRect = new RectangleF(0, 1000000f, Setup.LoopMS, 4000000f)
            };

            myPlotter.Setup(initAction:Init, shutdownAction:Shutdown);
        }


        private void Init()
        {
            _subPlot.Init();

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
            }
        }

        

        public void Add(BlockPacket block)
        {
            if (first) { first = false; return; }

            double scale = Yscale == 0.0 ? 1.0 : Yscale;                     // debugQueue.Enqueue(new DebugPoint { SW = sw.Elapsed.TotalMilliseconds, Timestamp = block.BlockData[0].TimeStamp });

            if (Selector == null)
                SetSubplot(block);

            _bufMainPlot.AddBlock(ref block, Selector, onlyLast:true); // only plot last point in block

            LastX = (float)block.BlockData[block.Count - 1].TimeStamp;
        }

        /// <summary>
        /// Renders the plot. Assumes the correct shader program is already active.
        /// </summary>
        public void Render()
        {
            if (Visible)
            {
                _bufMainPlot.DrawLineStrip();
                _subPlot.Render();
                DBG = "Rendered";
            }
            else
            {
                DBG = "Not Visible";
            }
        }

        /// <summary>
        /// Releases the GPU resources (VBO and VAO).
        /// </summary>
        public void Shutdown()
        {
            _bufMainPlot.Dispose();
            _bufSubPlot.Dispose();
            _bufSubPlotGrid.Dispose();

            _subPlot.Shutdown();
        }

        private void SetSubplot(BlockPacket block)
        {
            if (block == null) return;

            _subPlot.SetBlock(block);

            for (int i = 1; i < block.Count; i++)
            {
                ref var item = ref block.BlockData[i];

                uint c0 = block.BlockData[i - 1].Channel[0];
                uint c1 = item.Channel[0];
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

        }
    }
}

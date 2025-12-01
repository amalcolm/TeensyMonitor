using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using PsycSerial;
using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using TeensyMonitor.Plotter.Backgrounds;
using TeensyMonitor.Plotter.Fonts;
using TeensyMonitor.Plotter.Helpers;

namespace TeensyMonitor.Plotter.UserControls
{

    [ToolboxItem(true)]
    public partial class MyChart : MyPlotter
    {
        public TeensySerial? SP = Program.serialPort;

        public float ChannelScale { get; set; } = 0.05f;

        private const int WindowSize = 1200;

        private readonly Dictionary<uint, double> _latestValues = [];
        private readonly Dictionary<uint, Tuple<TextBlock, TextBlock>> _blocks = [];
        private readonly List<TextBlock> _textBlocksToRender = [];

        private LabelAreaRenderer? _labelAreaRenderer;

        private object _lock = new();

        public MyChart()
        {
            InitializeComponent();

            if (SP == null) return;

            SP.DataReceived += IO_DataReceived;
        }



        private void IO_DataReceived(IPacket packet)
        {
            if (packet is BlockPacket blockPacket == false) return;
            if (blockPacket.Count == 0) return;
            if (Plots.TryGetValue(blockPacket.State, out var plot) == false)
            {
                plot = new MyPlot(WindowSize, this);
                Plots[blockPacket.State] = plot;
                CreateTextBlocksForLabel(blockPacket.State);
            }

            plot.Add(blockPacket);
            if (blockPacket.Count > 0)
            {
                float scaledValue = blockPacket.BlockData[blockPacket.Count - 1].Channel[0] * ChannelScale;

                lock (_lock)
                    _latestValues[blockPacket.State] = scaledValue;
            }
        }

        protected override void Init()
        {
            base.Init();
            _labelAreaRenderer = new (this, "Resources/Backgrounds/LabelArea.png");
        }

        protected override void Shutdown()
        {
            base.Shutdown();
            _labelAreaRenderer?.Shutdown();
        }


        private void CreateTextBlocksForLabel(uint state)
        {
            if (font == null) return;

            string labelText = $": {state.Description()}";

            lock (_lock)
            {
                float yPos = MyGL.Height - 70 - _blocks.Count * 50;

                var labelBlock = new TextBlock(labelText, 106, yPos, font);
                var valueBlock = new TextBlock("0.00", 100, yPos, font, TextAlign.Right);

                _blocks[state] = Tuple.Create(labelBlock, valueBlock);
            }
        }

        protected override void DrawText()
        {
            if (font == null) return;

            _textBlocksToRender.Clear();
            // 1. Populate the list of blocks to render and flag if their content has changed.
            lock (_lock)
                foreach (var key in _latestValues.Keys)
                {
                    if (_blocks.TryGetValue(key, out var tuple))
                    {
                        ref var item = ref CollectionsMarshal.GetValueRefOrNullRef(_latestValues, key);

                        if (System.Runtime.CompilerServices.Unsafe.IsNullRef(ref item)) continue;

                        // Check
                        tuple.Item2.SetValue(item, "F2");

                        _textBlocksToRender.Add(tuple.Item1);
                        _textBlocksToRender.Add(tuple.Item2);
                    }
                }

            if (!_textBlocksToRender.Any()) return;
            
            // 2. Calculate the total bounding box for all visible labels.
            RectangleF totalBounds = CalculateTotalBounds();

            // 3. Render the background with padding.
            if (!totalBounds.IsEmpty)
            {
                float padding = 10f;
                var paddedBounds = new RectangleF(
                    totalBounds.X - padding,
                    totalBounds.Y - padding,
                    totalBounds.Width + (padding * 2),
                    totalBounds.Height + (padding * 2)
                );
                var projection = Matrix4.CreateOrthographicOffCenter(0, MyGL.ClientSize.Width, 0, MyGL.ClientSize.Height, -1.0f, 1.0f);
                
                _labelAreaRenderer?.Render(paddedBounds, projection);

                GL.UseProgram(_textShaderProgram);
            }

            fontRenderer.RenderText(_textBlocksToRender);
        }

        // Return this helper method inside the MyChart class
        private RectangleF CalculateTotalBounds()
        {
            RectangleF totalBounds = RectangleF.Empty;

            foreach (ref var block in CollectionsMarshal.AsSpan(_textBlocksToRender))
            {
                if (block.Bounds.IsEmpty) continue;

                if (totalBounds.IsEmpty)
                    totalBounds = block.Bounds;
                else
                    totalBounds = RectangleF.Union(totalBounds, block.Bounds);
            }
        
            return totalBounds;
        }

        // Add cleanup logic when plots are removed to prevent memory leaks from the pool.
        private void RemovePlot(uint key)
        {
            if (Plots.Remove(key, out var plot))
            {
                plot.Shutdown(); // Release OpenGL resources
            }

            if (_blocks.Remove(key, out var textBlocks))
            {
                textBlocks.Item1.Dispose(); // Return state buffer to pool
                textBlocks.Item2.Dispose(); // Return value buffer to pool
            }

            _latestValues.Remove(key);
        }

    }
}

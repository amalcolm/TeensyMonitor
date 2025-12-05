using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.InteropServices;

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

using PsycSerial;
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

        private readonly ConcurrentDictionary<uint, double> _latestValues = [];
        private readonly ConcurrentDictionary<uint, Tuple<TextBlock, TextBlock>> _blocks = [];
        private readonly List<TextBlock> _textBlocksToRender = [];

        private volatile int labelCount = 0;
        private readonly ConcurrentDictionary<uint, bool> _pendingStates = [];

        private LabelAreaRenderer? _labelAreaRenderer;

        private object _lock = new();

        public MyChart()
        {
            InitializeComponent();

            if (SP == null) return;

            SP.DataReceived += IO_DataReceived;
        }

        public void AddData(Dictionary<string, double> data)
        {
            foreach (var key in data.Keys)
                if (string.IsNullOrWhiteSpace(key) == false)
                {
                    uint stateHash = (uint)key.GetHashCode();
                    if (Plots.TryGetValue(stateHash, out var plot) == false)
                    {
                        if (TestAndSetPending(stateHash)) continue;

                        plot = new(WindowSize, this) { Yscale = 1.0 };
                        lock (PlotsLock)
                            Plots[stateHash] = plot;
                        CreateTextBlocksForLabel(stateHash, key);
                    }
                    else
                        plot.Add(data[key]);

                    lock (_lock)
                            _latestValues[stateHash] = data[key];
                }
        }

        private void IO_DataReceived(IPacket packet)
        {
            if (packet is BlockPacket blockPacket == false) return;
            if (blockPacket.Count == 0) return;
            lock (PlotsLock)
            {
                if (Plots.TryGetValue(blockPacket.State, out var plot) == false)
                    if (TestAndSetPending(blockPacket.State) == false)
                    {
                        plot = new MyPlot(WindowSize, this);
                        Plots[blockPacket.State] = plot;
                        CreateTextBlocksForLabel(blockPacket.State);
                    }
                    else
                        return;

                plot.Add(blockPacket);
            }
            if (blockPacket.Count > 0)
            {
                float val = blockPacket.BlockData[blockPacket.Count - 1].Channel[0] * ChannelScale;

                lock (_lock)
                    _latestValues[blockPacket.State] = val;
            }
        }

        private bool TestAndSetPending(uint state)
        {
            lock (_lock)
            {
                if (_pendingStates.ContainsKey(state)) return true;
                _pendingStates[state] = true;
                return false;
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
            => CreateTextBlocksForLabel(state, state.Description());

        private void CreateTextBlocksForLabel(uint state, string label)
        {
            if (font == null) return;

            string labelText = $": {label}";

            lock (_lock)
            {
                Interlocked.Increment(ref labelCount);
                float yPos = MyGL.Height - 20 - labelCount * 50;

                var labelBlock = new TextBlock(labelText, 106, yPos, font);
                var valueBlock = new TextBlock("0.00", 100, yPos, font, TextAlign.Right);

                _blocks[state] = Tuple.Create(labelBlock, valueBlock);
            }
            _pendingStates.TryRemove(state, out _);
        }

        protected override void DrawText()
        {
            if (font == null) return;

            _textBlocksToRender.Clear();
            // 1. Populate the list of blocks to render and flag if their content has changed.
            lock (_lock)
            {
                foreach (var key in _latestValues.Keys)
                {
                    if (_blocks.TryGetValue(key, out var tuple))
                    {
                        var item = _latestValues[key];

                        // Check
                        tuple.Item2.SetValue(item, "F2");

                        _textBlocksToRender.Add(tuple.Item1);
                        _textBlocksToRender.Add(tuple.Item2);
                    }
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

            _latestValues.TryRemove(key, out _);
        }

    }
}

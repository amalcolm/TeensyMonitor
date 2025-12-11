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
        private const int WindowSize = 1200;

        public bool EnablePlots  { get; set; } = true;
        public bool EnableLabels { get; set; } = true;

        private readonly ConcurrentDictionary<uint, double> _latestValues = [];
        private readonly ConcurrentDictionary<uint, Tuple<TextBlock, TextBlock>> _blocks = [];
        private readonly List<TextBlock> _textBlocksToRender = [];

        private volatile int labelCount = 0;
        private readonly ConcurrentDictionary<uint, bool> _pendingStates = [];

        private LabelAreaRenderer? _labelAreaRenderer;

        private readonly object _lock = new();

        public MyChart()
        {
            InitializeComponent();

            if (SP == null) return;

            SP.ConnectionChanged += SP_ConnectionChanged;
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


        const uint maskOffset1 = 0b00010000000000000000000000000000;
        const uint maskOffset2 = 0b00100000000000000000000000000000;
        const uint maskGain    = 0b00110000000000000000000000000000;
        const uint maskUser1   = 0b01000000000000000000000000000000;

        //                          3         2         1         0
        //                         10987654321098765432109876543210

        const uint maskPreGain  = 0b0000000000000000100000000000000;
        const uint maskPostGain = 0b0000000000000001000000000000000;

        private RunningAverage ra = new (20);

        public void SP_DataReceived(IPacket packet)
        {
            if (packet is BlockPacket blockPacket == false) return;
            if (blockPacket.Count == 0) return;

            uint state = (uint)blockPacket.State;

            uint  offset1State = state | maskOffset1;
            uint  offset2State = state | maskOffset2;
            uint     gainState = state | maskGain;
            uint    user1State = state | maskUser1;
            uint  preGainState = state | maskPreGain;
            uint postGainState = state | maskPostGain;

            if (EnableLabels)
                lock (PlotsLock)
                {
                    if (Plots.ContainsKey(state) == false)
                        if (TestAndSetPending(state) == false)
                        {
                            Plots[        state] = new MyPlot(WindowSize, this);

                            Plots[ offset1State] = new MyPlot(WindowSize, this);
                            Plots[ offset2State] = new MyPlot(WindowSize, this);
                            Plots[    gainState] = new MyPlot(WindowSize, this);
                            Plots[ preGainState] = new MyPlot(WindowSize, this);
                            Plots[postGainState] = new MyPlot(WindowSize, this);

                            Plots[   user1State] = new MyPlot(WindowSize, this);
                        }
                        else
                            return;

                            Plots[state].Add(blockPacket, MyPlot.DataToShow.Channel0, false, ref ra);
                    Plots[ offset1State].Add(blockPacket, MyPlot.DataToShow.Offset1 , false, ref ra);
                    Plots[ offset2State].Add(blockPacket, MyPlot.DataToShow.Offset2 , false, ref ra);
                    Plots[    gainState].Add(blockPacket, MyPlot.DataToShow.Gain    , false, ref ra);
                    Plots[ preGainState].Add(blockPacket, MyPlot.DataToShow.Gain    , false, ref ra);
                    Plots[postGainState].Add(blockPacket, MyPlot.DataToShow.Gain    , false, ref ra);

    //              Plots[   user1State].Add(blockPacket, MyPlot.DataToShow.Channel0, true , ref ra); // plot to show difference from running average
                }


            if (EnableLabels == false || font == null) return;  // packet received before GL is initialized

            if (_blocks.ContainsKey(state) == false)
            {
                string description = blockPacket.State.Description();

                CreateTextBlocksForLabel(        state, description + " A2D %"   , "0.0000%");
                CreateTextBlocksForLabel( offset1State, description + " Offset1" );
                CreateTextBlocksForLabel( offset2State, description + " Offset2" );
                CreateTextBlocksForLabel(    gainState, description + " Gain"    );
                CreateTextBlocksForLabel( preGainState, description + " preGain" );
                CreateTextBlocksForLabel(postGainState, description + " postGain");

                // no label for user1State
            }



            if (blockPacket.Count > 0)
            {
                ref DataPacket data = ref blockPacket.BlockData[blockPacket.Count-1];

                float c0   = data.Channel[0] / 4660100.0f;
                float off1 = data.Offset1;
                float off2 = data.Offset2;
                float gain = data.Gain;

                float preGain = data.preGainSensor;
                float postGain = data.postGainSensor;

                lock (_lock)
                {
                    _latestValues[        state] = c0;
                    _latestValues[ offset1State] = off1;
                    _latestValues[ offset2State] = off2;
                    _latestValues[    gainState] = gain;
                    _latestValues[ preGainState] = preGain;
                    _latestValues[postGainState] = postGain;
                  // do not show user1State value as label
                }
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

        private void CreateTextBlocksForLabel(uint state, string label, string valueFormat = "F0")
        {
            if (font == null) return;

            string labelText = $": {label}";

            lock (_lock)
            {
                Interlocked.Increment(ref labelCount);
                float yPos = MyGL.Height - 20 - labelCount * 50;

                var labelBlock = new TextBlock(labelText, 126, yPos, font);
                var valueBlock = new TextBlock("0.00", 120, yPos, font, TextAlign.Right, valueFormat);

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
                        tuple.Item2.SetValue(item);

                        _textBlocksToRender.Add(tuple.Item1);
                        _textBlocksToRender.Add(tuple.Item2);
                    }
                }
            }

            if (!_textBlocksToRender.Any()) return;
            
            // 2. Calculate the total bounding box for all visible labels.
            RectangleF totalBounds = _textBlocksToRender.CalculateTotalBounds(ref maxBounds);

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

        RectangleF maxBounds = RectangleF.Empty;

        // Return this helper method inside the MyChart class

        protected override void SP_ConnectionChanged(ConnectionState state)
        {
            base.SP_ConnectionChanged(state);

            if (state == ConnectionState.Disconnected)
                lock (_lock)
                {
                    _blocks.Clear();
                    _latestValues.Clear();
                    _pendingStates.Clear();
                    Interlocked.Exchange(ref labelCount, 0);
                    MyColours.Reset();
                }
        }


    }
}

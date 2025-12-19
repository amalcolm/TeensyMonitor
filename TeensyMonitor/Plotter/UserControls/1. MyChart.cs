using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using PsycSerial;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using TeensyMonitor.Plotter.Backgrounds;
using TeensyMonitor.Plotter.Fonts;
using TeensyMonitor.Plotter.Helpers;

namespace TeensyMonitor.Plotter.UserControls
{

    [ToolboxItem(true)]
    public partial class MyChart : MyPlotter
    {
        static volatile uint _instanceCounter = 0;
        private readonly uint _instanceId = _instanceCounter++;
        private const int WindowSize = 5120;

        public bool EnablePlots  { get; set; } = true;
        public bool EnableLabels { get; set; } = true;

        private readonly ConcurrentDictionary<uint, double> _latestValues = [];
        private readonly ConcurrentDictionary<uint, Tuple<TextBlock, TextBlock>> _blocks = [];
        private readonly List<TextBlock> _textBlocksToRender = [];

        private readonly ConcurrentDictionary<uint, bool> _pendingStates = [];

        private LabelAreaRenderer? _labelAreaRenderer;

        struct DataSelectorInfo
        {
            public string Name;
            public DataSelector Selector;
            public uint AdditionalMask;
        }

        static readonly List<DataSelectorInfo> dataSelectorsToOutput = [];
        static readonly List<DataSelectorInfo> dataSelectorsToPlot = [];
        static readonly List<DataSelectorInfo> dataSelectorsForLabels = [];

        private readonly object _lock = new();

        static readonly string[] dataFieldsToPlot = {
//            "Offset1", "Offset2", "Gain",
//            "preGainSensor",
//            "postGainSensor",
            };

        static readonly string[] dataFieldsForLabels = {
            "Offset1", "Offset2", "Gain",
            "preGainSensor",
            "postGainSensor",
            };

        public MyChart()
        {
            InitializeComponent();

            if (SP == null) return;

            SP.ConnectionChanged += SP_ConnectionChanged;

            var properties = typeof(DataPacket).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            

            if (dataSelectorsToOutput.Count > 0) return;

            var allDataFields = dataFieldsToPlot
                .Concat(dataFieldsForLabels)
                .Distinct()
                .ToArray();

            for (uint count = 1; count <= allDataFields.Length; count++)
            {
                var property = properties.First(p => p.Name == allDataFields[count - 1]);  // must declare a local to capture correctly in lambda

                DataSelector selector;

                if (property.PropertyType.IsArray)
                    selector = data =>
                    {
                        if (property.GetValue(data) is Array arr && arr.Length > 0)
                            return Convert.ToDouble(arr.GetValue(0));
                        else
                            return 0.0;
                    };
                else
                    selector = data => Convert.ToDouble(property.GetValue(data));

                var dsInfo = new DataSelectorInfo
                {
                    Name = property.Name,
                    Selector = selector,
                    AdditionalMask = count << 12 // 12 > number of red LEDs, so as not to overlap state bits
                };

                dataSelectorsToOutput.Add(dsInfo); // for latest values tracking, which handles both plots and labels

                if (dataFieldsToPlot   .Contains(property.Name)) dataSelectorsToPlot   .Add(dsInfo);
                if (dataFieldsForLabels.Contains(property.Name)) dataSelectorsForLabels.Add(dsInfo);
            }
        }
  
        private RunningAverage ra = new (20);

        public void SP_DataReceived(IPacket packet)
        {
            if (packet is BlockPacket blockPacket == false) return;
            if (blockPacket.Count == 0) return;

            uint state = (uint)blockPacket.State;

            if (EnableLabels)
                lock (PlotsLock)
                {
                    if (Plots.ContainsKey(state) == false)
                        if (TestAndSetPending(state) == false)
                        {
                            Plots[state] = new MyPlot(WindowSize, this);

                            foreach (var info in dataSelectorsToPlot)
                                    Plots[state | info.AdditionalMask] = new MyPlot(WindowSize, this)
                                    {
                                        Yscale = 1.0,
                                        Colour = MyColours.GetNextColour(),
                                        Selector = info.Selector
                                    };
                            

                        }
                        else
                            return;

                    Plots[state].Add(blockPacket);
                    foreach (var info in dataSelectorsToPlot)
                        Plots[state | info.AdditionalMask].Add(blockPacket, info.Selector);


                }


            if (EnableLabels == false || font == null) return;  // packet received before GL is initialized

            if (_blocks.ContainsKey(state) == false)
            {
                string description = blockPacket.State.Description();

                CreateTextBlocksForLabel(        state, description + " A2D %"   , "0.0000%");

                foreach (var info in dataSelectorsForLabels)
                    CreateTextBlocksForLabel(state | info.AdditionalMask, description + " " + info.Name, "F4");
                
            }



            if (blockPacket.Count > 0)
            {
                ref DataPacket data = ref blockPacket.BlockData[blockPacket.Count-1];

                float c0   = data.Channel[0] / 4660100.0f;

                lock (_lock)
                {
                    _latestValues[state] = c0;
                    foreach (var info in dataSelectorsToOutput)
                    {
                        var x = info.Selector(data);
                        _latestValues[state | info.AdditionalMask] = x;
                    }
                }
            }

            
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
                var labelBlock = new TextBlock(labelText, 126, 0, font);                                 // yPos set on render
                var valueBlock = new TextBlock("0.00", 120, 0, font, TextAlign.Right, valueFormat);

                _blocks[state] = Tuple.Create(labelBlock, valueBlock);
            }
            _pendingStates.TryRemove(state, out _);
        }
        protected override void DrawText()
        {
            if (font == null) return;

            _textBlocksToRender.Clear();

            lock (_lock)
            {
                // Sort by state for consistent order (important for stable layout)
                
                int index = 1;
                float lineSpacing = 50f;
                float topMargin = 20f;

                foreach (var stateKey in _latestValues.Keys)
                {
                    if (_blocks.TryGetValue(stateKey, out var tuple))
                    {
                        tuple.Item2.SetValue(_latestValues[stateKey]);

                        float yPos = MyGL.Height - topMargin - (index * lineSpacing);

                        tuple.Item1.Y = yPos;
                        tuple.Item2.Y = yPos;

                        _textBlocksToRender.Add(tuple.Item1);
                        _textBlocksToRender.Add(tuple.Item2);

                        index++;
                    }
                }
            }

            if (_textBlocksToRender.Count == 0) return;
            
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
                    MyColours.Reset();
                }
        }

//      public string getDebugOutput()
//      {
//          lock (_lock)
//          {
//              StringBuilder sb = new();
//              sb.AppendLine($"Chart {_instanceId}:");
//              foreach (var plot in Plots.Values)
//              {
//                  sb.Append(plot.getDebugOutput());
//              }
//              return sb.ToString();
//          }
//      }

    }
}

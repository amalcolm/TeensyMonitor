using OpenTK.Graphics.OpenGL4;
using PsycSerial;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using TeensyMonitor.Plotter.Helpers;
namespace TeensyMonitor.Plotter.UserControls
{
    [ToolboxItem(false)]
    public partial class MyPlotter : MyPlotterBase
    {
        protected TeensySerial? SP = Program.serialPort;

        protected ConcurrentDictionary<uint, MyPlot> Plots = [];
        protected readonly object PlotsLock = new();

        public float TimeWindowSeconds  { get; set; } = 10.0f;
        public float Yscale             { get; set; } =  1.0f;

        protected string Debug = string.Empty;
        protected override void Init()
        {
            base.Init();
            MyGL.MouseWheel += MyGL_MouseWheel;

            if (SP == null) return;

            SP.ConnectionChanged += SP_ConnectionChanged;
        }

        private float _currentViewRight = 0.0f;
        private float _maxTime = 0.0f;

        private readonly Stopwatch SW = new();
        private double _watchOffset = 0.0;

        private DateTime lastTime = DateTime.Now;
        private readonly TimeSpan timeBetweenDebug = TimeSpan.MaxValue;
        protected override void DrawPlots()
        {
            if (DateTime.Now - lastTime > timeBetweenDebug)
            {
                System.Diagnostics.Debug.WriteLine($"[MyPlotter] Plots: {Plots.Count}, TimeWindow: {TimeWindowSeconds:F1}s, MaxTime: {_maxTime:F3}s");

                lastTime = DateTime.Now;
            }

            if (Plots.IsEmpty) return;

            // 1. Get the latest time from all plots
            lock (PlotsLock)
                _maxTime = Plots.Values.Max(p => p?.LastX ?? float.MinValue);
            if (_maxTime == float.MinValue) return; // work around synchronization issue

            if (SW.IsRunning == false)
            {
                _watchOffset = _maxTime + (TimeWindowSeconds * 0.05);
                SW.Start();
            }
            

            // 2. Define the target for the right edge of our viewport.
            //    This includes a small buffer for the gap.
            _currentViewRight = (float)(SW.ElapsedMilliseconds / 1000.0 + _watchOffset);

            if (_maxTime > _currentViewRight)
            {
                // If new data has arrived that is beyond our current view, jump the view forward.
                _watchOffset = _maxTime - TimeWindowSeconds * 0.05;
                _currentViewRight = (float)(_maxTime + TimeWindowSeconds * 0.05);
                SW.Restart();
            }

            // 4. Define the viewport based on the smoothed position.
            float viewLeft = _currentViewRight - TimeWindowSeconds;
            ViewPort = new RectangleF(viewLeft, -6, TimeWindowSeconds, 1030);


            int colorLocation = GL.GetUniformLocation(_plotShaderProgram, "uColor");
            lock (PlotsLock)
                foreach (var key in Plots.Keys)
                {
                    var plot = Plots[key]; if (plot == null) continue;

                    if (plot.Yscale == 0.0f)
                        plot.Yscale = Yscale;

                    GL.Uniform4(colorLocation, plot.Colour);
                    plot.Render();
                }

 //           Debug = $"Plots: {Plots.Count}, Time: {_maxTime:F2}, Window: {TimeWindowSeconds}s";
        }

        protected virtual void SP_ConnectionChanged(ConnectionState state)
        {
            switch (state)
            {
                case ConnectionState.Connected:
                    // Handle connection established
                    break;
                case ConnectionState.Disconnected:
                    lock (PlotsLock)
                    {
                        Plots.Clear();
                    }
                    break;
            }
            SW.Reset();
        }

        protected override void DrawText()
            => fontRenderer?.RenderText(Debug, 10, 10);

        private void MyGL_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (GLThread == null || GLThread._shutdownRequested) return;

            const float zoomFactor = 1.1f;
            float newTimeWindow;

            if (e.Delta > 0)
                newTimeWindow = TimeWindowSeconds / zoomFactor;
            else
                newTimeWindow = TimeWindowSeconds * zoomFactor;

            newTimeWindow = Math.Clamp(newTimeWindow, 0.1f, 10.0f);

            GLThread.Enqueue(() => { TimeWindowSeconds = newTimeWindow; });
        }


    }
}
using OpenTK.Graphics.OpenGL4;
using System.ComponentModel;
using System.Runtime.InteropServices;
using TeensyMonitor.Plotter.Helpers;
namespace TeensyMonitor.Plotter.UserControls
{
    [ToolboxItem(false)]
    public partial class MyPlotter : MyPlotterBase
    {
        protected Dictionary<uint, MyPlot> Plots = [];
        public float TimeWindowSeconds  { get; set; } = 10.0f;
        public float Yscale             { get; set; } =  1.0f;

        protected string Debug = string.Empty;
        protected override void Init()
        {
            base.Init();
            MyGL.MouseWheel += MyGL_MouseWheel;
        }

        private float _currentViewRight = 0.0f;
        private float _maxTime = 0.0f;

        private DateTime lastTime = DateTime.Now;
        private readonly TimeSpan timeBetweenDebug = TimeSpan.MaxValue;
        protected override void DrawPlots()
        {
            if (DateTime.Now - lastTime > timeBetweenDebug)
            {
                System.Diagnostics.Debug.WriteLine($"[MyPlotter] Plots: {Plots.Count}, TimeWindow: {TimeWindowSeconds:F1}s, MaxTime: {_maxTime:F3}s");

                lastTime = DateTime.Now;
            }

            if (Plots.Count == 0) return;

            // 1. Get the latest time from all plots
            _maxTime = Plots.Values.Max(p => p?.LastX ?? float.MinValue);
            if (_maxTime == float.MinValue) return; // work around synchronization issue

            // 2. Define the target for the right edge of our viewport.
            //    This includes a small buffer for the gap.
            float targetViewRight = _maxTime + (TimeWindowSeconds * 0.05f); // 5% buffer

            // 3. Smoothly interpolate the current view position towards the target.
            //    The `0.1f` is the "smoothing factor". A smaller value gives
            //    a smoother, but more "laggy" scroll. A larger value is more
            //    responsive but can be more jittery. You can tune this.
            _currentViewRight = _currentViewRight * 0.9f + targetViewRight * 0.1f;

            // 4. Define the viewport based on the smoothed position.
            float viewLeft = _currentViewRight - TimeWindowSeconds;
            ViewPort = new RectangleF(viewLeft, -6, TimeWindowSeconds, 1030);


            int colorLocation = GL.GetUniformLocation(_plotShaderProgram, "uColor");
            foreach (var key in Plots.Keys)
            {
                ref var plot = ref CollectionsMarshal.GetValueRefOrNullRef(Plots, key);
                if (plot.Yscale == 0.0f)
                    plot.Yscale = Yscale;

                if (System.Runtime.CompilerServices.Unsafe.IsNullRef(ref plot)) continue;
                
                // Chec
                GL.Uniform4(colorLocation, plot.Colour);
                plot.Render();
            }

 //           Debug = $"Plots: {Plots.Count}, Time: {_maxTime:F2}, Window: {TimeWindowSeconds}s";
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
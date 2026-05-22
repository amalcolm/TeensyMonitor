using System.Drawing;
using TeensyMonitor.MyGLTools.Helpers;

namespace TeensyMonitor.MyGLTools.UserControls
{
    public partial class MyPlotterWithAxes : MyPlotter
    {
        private readonly PlotAxesRenderer _axes = new();

        public PlotAxesRenderer._Options AxesOptions { get => _axes.Options; }

        protected override void Init()
        {
            base.Init();
            _axes.Init(font);
        }

        protected override void Shutdown()
        {
            _axes.Shutdown();
            base.Shutdown();
        }

        protected override void DrawPlotOverlays()
        {
            base.DrawPlotOverlays();

            if (AxesOptions.DrawAxes == false) return;

            RectangleF axesViewPort = GetAxesViewPort();
            ApplyPlotTransform(axesViewPort);
            _axes.RenderLines(axesViewPort, GLClientSize);
        }

        protected override void DrawText()
        {
            base.DrawText();

            if (AxesOptions.DrawAxes)
                _axes.RenderText(fontRenderer);
        }

        protected virtual RectangleF GetAxesViewPort() => GetMetricsViewPort();
    }
}

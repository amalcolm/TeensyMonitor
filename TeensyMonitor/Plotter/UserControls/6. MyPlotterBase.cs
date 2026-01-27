using TeensyMonitor;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.ComponentModel;

using TeensyMonitor.Plotter.Helpers;
namespace TeensyMonitor.Plotter.UserControls
{
    public abstract class MyPlotterBase : MyGLControl
    {

        // Shader programs
        protected int _plotShaderProgram ;
        private Matrix4 _plotTransform;
        public Matrix4 getPlotTransform() => _plotTransform;

        public int GetPlotShader() => _plotShaderProgram;

        protected MyPlotterBase()
        {
            if (!Program.IsRunning) return;
        }

        protected override void Init()
        {
            _plotShaderProgram = ShaderManager.Get("plot");
        }

        protected override void Render()
        {
            GL.UseProgram(_plotShaderProgram);

            _plotTransform = Matrix4.CreateOrthographicOffCenter(ViewPort.Left, ViewPort.Right, ViewPort.Top, ViewPort.Bottom, -1.0f, 1.0f);
            int transformLocation = GL.GetUniformLocation(_plotShaderProgram, "uTransform");
            GL.UniformMatrix4(transformLocation, false, ref _plotTransform);

            DrawPlots();
        }

        protected abstract void DrawPlots();

        public void SetMetrics(float min, float max, float range, float desiredRange)
        {
            _metrics ??= new PlotMetrics();

            _metrics.MinY = min;
            _metrics.MaxY = max;
            _metrics.RangeY = range;
            _metrics.DesiredRangeY = desiredRange;
        }


        public class PlotMetrics
        {
            public float MinY = 0.0f;
            public float MaxY = 0.0f;
            public float RangeY = 0.0f;
            public float DesiredRangeY = 0.0f;
        }

        private PlotMetrics? _metrics = null;
        public PlotMetrics? GetMetrics() => _metrics;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ScottPlot;
using ScottPlot.WinForms;

using Color = System.Drawing.Color;
using System.Diagnostics;

using TeensyMonitor.FrameTypes;
using PsycSerial;

namespace TeensyMonitor.Helpers
{
    public partial class CTG_Chart : UserControl
    {
        private readonly FormsPlot formsPlot;
        private readonly Plot      plot;

        private readonly Dictionary<PlotKind, CTG_Plot> dPlots = [];

        private readonly ScottPlot.Color backColor = ScottPlot.Color.FromColor(Color.Cornsilk);

        public CTG_Chart()
        {
            InitializeComponent();

            formsPlot = new FormsPlot
            {
                Dock = DockStyle.Fill,
                Parent = this,
                BackColor = Color.Cornsilk
            };
            plot = formsPlot.Plot;
            plot.DataBackground.Color = backColor;

            var plots = new List<CTG_Plot>
            {
                new(plot, PlotKind.Ambient, Color.FromArgb(24, 24, 24)),
                new(plot, PlotKind.Red , Color.Red  ),
                new(plot, PlotKind.IR , Color.Blue),
            };
            
            dPlots = plots.ToDictionary(p => p.Kind, p => p);

            this.Controls.Add(formsPlot);

        }


        public void AddData<T>(T data) where T : BaseFrame, IAmbRedIR
        {
            if (okayToRender && frameTimer.IsRunning == false)
            {
                _cts = new();
                frameTimer.Start();
                _ = StartAnimation();
            }

            dPlots[PlotKind.Ambient].Add(data.Ambient);
            dPlots[PlotKind.Red].Add(data.Red);
            dPlots[PlotKind.IR].Add(data.IR);


            var raAmbient = dPlots[PlotKind.Ambient].RunningAverage;
            var raRed = dPlots[PlotKind.Red].RunningAverage;
            var raIR = dPlots[PlotKind.IR].RunningAverage;

            double min = Math.Min(raAmbient.Min, Math.Min(raRed.Min, raIR.Min));
            double max = Math.Max(raAmbient.Max, Math.Max(raRed.Max, raIR.Max));
            double range = max - min;
            double scale = 10.0;
            plot.Axes.SetLimitsY(min - range/scale, max + range/scale);
        }

        private readonly System.Windows.Forms.Timer ti = new() { Interval = 100 };
        private void CTG_Chart_Load(object sender, EventArgs e)
        {   if (DesignMode || Program.IsRunning == false) return;

            ManagedPacket packet = new(DateTime.Now, [], 0);

            ti.Tick += (s, e) =>  AddData( new TestFrame(packet) );
            plot.Axes.SetLimitsX(0, 1);

            okayToRender = true;

            if (Program.serialPort.IsOpen == false)
                ti.Start();
        }

        bool okayToRender = false;
        protected override void OnHandleDestroyed(EventArgs e)
        {
            _cts.Cancel();
            okayToRender = false;
            base.OnHandleDestroyed(e);
        }

        private CancellationTokenSource _cts = new();

        private async Task StartAnimation()
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    double time = frameTimer.Elapsed.TotalSeconds;

                    plot.Axes.SetLimitsX(time - 9, time + 1);
                    this.Invoke(formsPlot.Refresh);

                    await Task.Delay(33, _cts.Token); // ~30fps
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _cts.Dispose();
                _cts = new();
                frameTimer.Stop();
            }
        }

        public void StopAnimation() => _cts.Cancel();
        
        public void Clear()
        {
            frameTimer.Restart();

            foreach (var plot in dPlots.Values)
                plot.Restart();
        }

        private readonly Stopwatch frameTimer = new();
    }
}

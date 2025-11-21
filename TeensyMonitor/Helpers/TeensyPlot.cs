using ScottPlot;
using ScottPlot.Plottables;

using Color = ScottPlot.Color;

namespace TeensyMonitor.Helpers
{
    public enum PlotKind { Ambient, Red, IR }

    public class CTG_Plot
    {
        public Color Colour;
        public PlotKind Kind;
        public Signal Plot;

        public MySignalSource Data = new();
        public RunningAverage RunningAverage = new(40);
        public CTG_Plot(Plot ParentPlot, PlotKind kind, System.Drawing.Color colour)
        {
            Colour = Color.FromColor(colour);
            Kind = kind;
            Plot = ParentPlot.Add.Signal(Data, Colour);
        }

        public void Add(double y)
        {
            double val = (y == 0 ? double.NaN : y);

            Data.Add(val);
            RunningAverage.Add(val);
        }

        public void Restart()
        {
            Data.Clear();
            RunningAverage.Reset();
        }
    }
}

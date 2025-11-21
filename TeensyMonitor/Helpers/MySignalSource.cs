using ScottPlot;

namespace TeensyMonitor.Helpers
{
    public class MySignalSource : ISignalSource
    {
        private readonly List<double> values = [];
        private double minValue = double.MaxValue;
        private double maxValue = double.MinValue;

        public double Period  { get; set; } = 1.0/10.0;  // Time between samples in seconds
        public double XOffset { get; set; } = 0;
        public double YOffset { get; set; } = 0;
        public double YScale  { get; set; } = 1.0;

        private int _count = 0;  // Track number of values added
        public int MaximumIndex { get => _count - 1; set {} }
        public int MinimumIndex { get => 0;          set {} }

        public void Add(double value)
        {
            values.Add(value);
            _count++;

//            if (_count > MaximumValues)
//                values.RemoveAt(0);

            minValue = Math.Min(minValue, value);
            maxValue = Math.Max(maxValue, value);
        }
        public int GetIndex(double x, bool clamp)
        {
            int index = (int)Math.Round((x - XOffset) / Period);

            return clamp ? Math.Max(MinimumIndex, Math.Min(MaximumIndex, index))
                         : index;
        }

        public AxisLimits GetLimits()
        {
            var LimitsX = GetLimitsX();
            var LimitsY = GetLimitsY();

            return new AxisLimits(LimitsX.Value1, LimitsX.Value2, LimitsY.Value1, LimitsY.Value2);
        }

        public CoordinateRange GetLimitsX()
        {
            return values.Count == 0
                ? new CoordinateRange(0, Period)
                : new CoordinateRange(
                          GetX(MinimumIndex),
                          GetX(MaximumIndex)
                      );
                }

        public CoordinateRange GetLimitsY()
        {
            return values.Count == 0
                ? new CoordinateRange(0, 1)
                : new CoordinateRange(
                          minValue * YScale + YOffset,
                          maxValue * YScale + YOffset
                      );
        }

        public PixelColumn GetPixelColumn(IAxes axes, int xPixelIndex)
        {
            var xCoord = axes.GetCoordinateX(xPixelIndex);
            int index = GetIndex(xCoord, true);

            if (index < 0 || index >= values.Count)
                return new PixelColumn(xPixelIndex, float.NaN, float.NaN, float.NaN, float.NaN);

            // Get current value and adjacent values for enter/exit points
            float currentY = (float)GetY(index);
            float prevY = (float)GetY(index - 1);
            float nextY = (float)GetY(index + 1);

            // Calculate enter and exit points
            float enter = float.IsNaN(prevY) ? currentY : (prevY + currentY) / 2;
            float exit = float.IsNaN(nextY) ? currentY : (currentY + nextY) / 2;

            // Calculate bottom and top for the column
            float bottom = Math.Min(enter, exit);
            float top = Math.Max(enter, exit);

            return new PixelColumn(xPixelIndex, enter, exit, bottom, top);
        }

        public double GetX(int index) => index * Period + XOffset;
        
        public double GetY(int index) =>
            index >= 0 && index < values.Count ? values[index] * YScale + YOffset
                                               : double.NaN;


        public IReadOnlyList<double> GetYs() => values;
        

        public IEnumerable<double> GetYs(int index1, int index2)
        {
            int start = Math.Max(0               , Math.Min(index1, index2));
            int end   = Math.Min(values.Count - 1, Math.Max(index1, index2));

            for (int i = start; i <= end; i++)
                yield return values[i];
        }

        public void Clear()
        {
            values.Clear();
            minValue = double.MaxValue;
            maxValue = double.MinValue;
            _count = 0;
        }
    }
}
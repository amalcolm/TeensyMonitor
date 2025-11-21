namespace TeensyMonitor.Helpers
{
    public class RunningAverage(int windowSize)
    {
        public static bool IsValidValue(double value) =>
            !(double.IsNaN(value) || double.IsInfinity(value));

        private readonly Queue<double> values = [];
        private double runningSum = 0.0;
        private int validSampleCount = 0;
        private int nCount = 0;
        public void Add(double value)
        {
            if (nCount >= windowSize)
            {
                double oldVal = values.Dequeue();
                if (IsValidValue(oldVal))
                {
                    runningSum -= oldVal;
                    validSampleCount--;
                }
            }

            values.Enqueue(value);

            if (IsValidValue(value))
            {
                runningSum += value;
                validSampleCount++;
            }

            if (validSampleCount > 0)
            {
                double average = runningSum / validSampleCount;

                Min = average - 10;
                Max = average + 10;
            }
            else
            {
                Min = 0;
                Max = 200;
            }

            nCount++;
        }

        public void Reset()
        {
            values.Clear();
            runningSum = 0.0;
            validSampleCount = 0;
            nCount = 0;
            Min = 0;
            Max = 200;
        }

        public double Min { get; private set; } = 0;
        public double Max { get; private set; } = 200;

        public int Count => nCount;
    }
}

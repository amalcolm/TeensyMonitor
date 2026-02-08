
namespace TeensyMonitor
{
    using PsycSerial;
    using System.Diagnostics;
    using TeensyMonitor.Plotter.UserControls;

    public partial class MainForm : Form
    {
        readonly Dictionary<HeadState, MyChart> Charts = [];

        enum FormState
        {
            None,
            Initialising,
            Building,
            Running
        }


        private FormState State
        {
            get => _state;

            set
            {
                _state = value;
                _swInit.Restart();

                switch (_state)
                {
                    case FormState.None:         this.Invoker(Init_Clear); break;
                    case FormState.Initialising:                           break;
                    case FormState.Building:     this.Invoker(Init_Set);   break;
                    case FormState.Running:                                break;
                }
            }
        }

        private readonly Stopwatch _swInit = new();

        private FormState _state = FormState.None;
        private readonly Dictionary<HeadState, int> initStates = [];

        private void Init_Clear()
        {

            for (int r = tlpCharts.RowCount - 1; r >= 0; r--)
                for (int c = tlpCharts.ColumnCount - 1; c >= 0; c--)
                {
                    var control = tlpCharts.GetControlFromPosition(c, r);
                    if (control is not null && control is MyChart chart && chart != chart0)
                    {
                        tlpCharts.Controls.Remove(control);
                        chart.Close();
                    }
                }

            tlpCharts.RowCount = 1;
            tlpCharts.ColumnCount = 1;
            tlpCharts.RowStyles.Clear();
            tlpCharts.ColumnStyles.Clear();

            Charts.Clear();


            initStates.Clear();
        }

        private void AddChart(BlockPacket blockPacket)
        {
            MyChart newChart = new()
            {
                BackColor = chart0.BackColor,
                Dock = chart0.Dock,
                Tag = blockPacket.State.Description(),
            };

            Charts[blockPacket.State] = newChart;
            newChart.SP_DataReceived(blockPacket);

            this.Invoker(() =>
            {
                SuspendLayout();

                tlpCharts.RowCount += 1;
                tlpCharts.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                tlpCharts.Controls.Add(newChart, 0, tlpCharts.RowCount - 1);

                ResumeLayout(true);
            });
        }


        private void Init_Packet(BlockPacket blockPacket)
        {
            switch (State)
            {
                case FormState.None:
                case FormState.Initialising:
                    if (initStates.ContainsKey(blockPacket.State) == false)
                        initStates.Add(blockPacket.State, 0);
                    else
                        if (++initStates[blockPacket.State] > 2)
                        State = FormState.Building; // seen state enough times

                    if (_swInit.ElapsedMilliseconds > 1000)
                        State = FormState.Building;
                    break;
            }
        }

        MyChart[,] tabCharts = new MyChart[0, 0];

        private void Init_Set()
        {
            if (initStates.Count == 0) return;

            HeadState[] states = [.. initStates.Keys];
            Array.Sort(states);

            if (states.Length > 4)
            {
                WindowState = FormWindowState.Normal;
                Location = Point.Empty;
                WindowState = FormWindowState.Maximized;
            }
            Init_SetTable();

            int _tlpColumn = 0; int _tlpRow = 0;

            tabCharts = new MyChart[tlpCharts.ColumnCount, tlpCharts.RowCount];

            foreach (var state in states)
            {
                MyChart newChart;
                if (_tlpColumn == 0 && _tlpRow == 0)
                    newChart = chart0;
                else
                    newChart = new MyChart()
                    {
                        BackColor = chart0.BackColor,
                        Dock = chart0.Dock,
                        Tag = state.Description(),
                    };

                Charts[state] = newChart;

                tabCharts[_tlpColumn, _tlpRow] = newChart;

                _tlpColumn++;
                if (_tlpColumn >= tlpCharts.ColumnCount)
                {
                    _tlpColumn = 0;
                    _tlpRow++;
                }

            }

            this.Invoker(() =>
            {
                SuspendLayout();
                for (int r = 0; r < tlpCharts.RowCount; r++)
                    for (int c = 0; c < tlpCharts.ColumnCount; c++)
                    {
                        if (r == 0 && c == 0) continue; // skip chart0

                        var chart = tabCharts[c, r]; if (chart == null) continue;

                        try { tlpCharts.Controls.Add(chart, c, r); }
                        catch (Exception)
                        {
                            //                            dbg.Log(AString.FromString($"Error adding chart for {chart.Tag}: {ex.Message}"));
                        }
                    }
                ResumeLayout(true);
                State = FormState.Running;
            });

        }

        private void Init_SetTable()
        {
            int n = initStates.Count;
            int cols = n < 3 ? 1 : (int)Math.Ceiling(Math.Sqrt(n));
            int rows = (int)Math.Ceiling((double)n / cols);

            tlpCharts.SuspendLayout();
            try
            {
                // tlpCharts already cleared in Init_Clear

                tlpCharts.ColumnCount = cols;
                tlpCharts.RowCount = rows;

                for (int c = 0; c < cols; c++) tlpCharts.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100.0f / cols));
                for (int r = 0; r < rows; r++) tlpCharts.RowStyles.Add(new RowStyle(SizeType.Percent, 100.0f / rows));

                tlpCharts.GrowStyle = TableLayoutPanelGrowStyle.FixedSize; // optional but avoids surprise growth
            }
            finally
            {
                tlpCharts.ResumeLayout(true);
            }

        }

    }
}
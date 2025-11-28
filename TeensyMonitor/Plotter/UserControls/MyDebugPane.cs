using OpenTK.Mathematics;
using PsycSerial;
using TeensyMonitor.Plotter.Fonts;

using System.Buffers;
using System.Collections.Concurrent;
using System.ComponentModel;


namespace TeensyMonitor.Plotter.UserControls
{
    [ToolboxItem(true)]
    public partial class MyDebugPane : MyGLControl
    {
        private readonly Log log = default!;
        public MyDebugPane()
        {
            if (!Program.IsRunning) { ShowDesignView("MyDebugPane (Design View)"); return; }

            InitializeComponent();

            log = new Log(this);
        }

        protected override void Init()
        {   base.Init();
         
            fontRenderer.Scaling = 0.4f;
            log.Init();
        }


        public void Log(AString str) => log.Add(str);

        protected override void DrawText() => log.Render();

    }

    class Log
    {
        public Log(MyGLControl control)
        {
            _control = control;
            control.LoadedChanged += (s, isLoaded) =>
            {
                if (isLoaded)
                    while (qStringsToAdd.TryDequeue(out AString? str))
                        if (str != null)
                            Add(str);
            };
        }
        readonly ArrayPool<FontVertex> pool = ArrayPool<FontVertex>.Shared;

        public void Init()
        {
            var fr = _control.fontRenderer;
            MaxNumberOfLines = _control.Height / (int)(fr.Font.LineHeight * fr.Scaling * 1.2f) - 1;
            LineBuffers = new LineVertices[MaxNumberOfLines];
            nextHeight = _control.Height - 10 - fr.Font.LineHeight * fr.Scaling * 1.2f;
        }

        private int MaxNumberOfLines;
        private float nextHeight = 0;
        private float baseHeight = 0;
        private int UsedLines = 0;

        private LineVertices[] LineBuffers = default!;

        struct LineVertices
        {
            public FontVertex[] Vertices;
            public int Length;
        }

        private readonly ConcurrentQueue<LineVertices> qLinesToAdd = [];

        private readonly ConcurrentQueue<AString> qStringsToAdd = [];

        public void Add(AString str)
        {
            if (str.Length == 0) return;

            if (_control.IsLoaded == false)
            {
                qStringsToAdd.Enqueue(str);
                return;
            }

            var buf = pool.Rent(str.Length * 6);

            var fr = _control.fontRenderer;


            var numVerts = FontVertex.BuildString(buf, 0, str.Buffer.AsSpan(), FontFile.Default, 0, nextHeight, fr.Scaling, TextAlign.Left);
            qLinesToAdd.Enqueue(new LineVertices { Vertices = buf, Length = numVerts });
            nextHeight -= fr.Font.LineHeight * fr.Scaling * 1.2f;
        }

        public void Render()
        {
            var fr = _control.fontRenderer;
            float lineHeight = fr.Font.LineHeight * fr.Scaling * 1.2f;
            while (qLinesToAdd.TryDequeue(out LineVertices newLine))
            {
                int thisLine = UsedLines % MaxNumberOfLines;
                if (UsedLines >= MaxNumberOfLines)
                {
                    pool.Return(LineBuffers[thisLine].Vertices);
                    baseHeight -= lineHeight;
                }

                LineBuffers[thisLine] = newLine;
                UsedLines++;

            }

            fr.ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(
                -10, _control.Width - 10,
                10 + baseHeight, 10 + _control.Height + baseHeight, 
                -1, 1);

            foreach (var line in LineBuffers)
            {
                var vertices = line.Vertices;
                fr.RenderText(line.Vertices, line.Length);
            }
        }

        private MyGLControl _control;

    }
}

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
        public const int Margin = 8;
        public const float LineSpacing = 1.2f;

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
        float lineHeight;
        public void Init()
        {
            var fr = _control.fontRenderer;
            lineHeight = fr.Font.LineHeight * fr.Scaling * LineSpacing;

            MaxNumberOfLines = (int)Math.Round((_control.Height - 2 * Margin) / lineHeight);
            LineBuffers = new LineVertices[MaxNumberOfLines];

            baseHeight = -PrecisionBoundary;
            nextHeight = baseHeight + _control.Height - Margin - lineHeight;
            
            _control.AutoClear = false;
        }

        private int MaxNumberOfLines;  // number of lines that fit in the control
        private float nextHeight = 0;  // height of the next line to add
        private float baseHeight = 0;  // base height offset for scrolling
        private int UsedLines = 0;     // total number of lines added so far

        private const float PrecisionBoundary = 0x200000;
        private LineVertices[] LineBuffers = [];

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
            nextHeight -= lineHeight;
        }

        public void Render()
        {
            bool needUpdate = false;
            while (qLinesToAdd.TryDequeue(out LineVertices newLine))
            {
                int thisLine = UsedLines % MaxNumberOfLines;

                if (UsedLines >= MaxNumberOfLines)
                    pool.Return(LineBuffers[thisLine].Vertices);

                LineBuffers[thisLine] = newLine;

                UsedLines++;

                needUpdate = true;
                
                ManageScrolling();
            }

            if (!needUpdate) return;

            _control.ClearViewport();

            var fr = _control.fontRenderer;
            float fMargin = Margin;

            fr.ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(
                -fMargin, _control.Width - fMargin,
                fMargin + baseHeight, fMargin + _control.Height + baseHeight,
                -1, 1);

            foreach (ref var line in LineBuffers.AsSpan())
                fr.RenderText(line.Vertices, line.Length);
        }

        private MyGLControl _control;


        private void ManageScrolling()
        {
            // Scroll is needed one before we exceed max lines
            // No idea why, but it works.  Beats GPT-5.1 Thinking et.al.
            if (UsedLines >= MaxNumberOfLines)
            {
                baseHeight -= lineHeight;
                if (baseHeight > PrecisionBoundary)
                {
                    // reset base height to avoid float precision issues
                    baseHeight = -PrecisionBoundary;
                    nextHeight = baseHeight + _control.Height - Margin - lineHeight;
                    nextHeight = (MaxNumberOfLines - 1) * lineHeight;

                    float offset = 2 * PrecisionBoundary;
                    foreach (ref var line in LineBuffers.AsSpan())
                        for (int i = 0; i < line.Length; i++)
                            line.Vertices[i].Position.Y -= offset;
                }
            }
        }
    }
}

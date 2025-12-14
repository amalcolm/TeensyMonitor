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

        public void Clear() => log.Clear();

        protected override void DrawText() => log.Render();

    }

    class Log(MyGLControl control)
    {
        public const int Margin = 8;
        public const double LineSpacing = 1.2;
        readonly ArrayPool<FontVertex> pool = ArrayPool<FontVertex>.Shared;
        int lineHeight;
        public void Init()
        {
            var fr = control.fontRenderer;
            lineHeight = (int)(fr.Font.LineHeight * fr.Scaling * LineSpacing);

            MaxNumberOfLines = (control.Height - 2 * Margin) / lineHeight;
            Clear();
            control.AutoClear = false;
        }

        public void Clear()
        {
            while (qLinesToAdd.TryDequeue(out _)) ;
            while (qStringsToAdd.TryDequeue(out _)) ;
            for (int i = 0; i < LineBuffers.Length; i++)
                if (LineBuffers[i].Vertices != null)
                    pool.Return(LineBuffers[i].Vertices);
            
            LineBuffers = new LineVertices[MaxNumberOfLines];
            
            UsedLines = 0;
            baseHeight = -PrecisionBoundary;
            nextHeight = baseHeight + control.Height - Margin - lineHeight;
        }

        private int MaxNumberOfLines;  // number of lines that fit in the control
        private volatile int nextHeight = 0;  // height of the next line to add
        private volatile int baseHeight = 0;  // base height offset for scrolling
        private int UsedLines = 0;     // total number of lines added so far

        private const int PrecisionBoundary = 0x200000;
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
            if (str.Length > 0)
                qStringsToAdd.Enqueue(str);
        }

        public void Render()
        {
            while (qStringsToAdd.TryDequeue(out AString? str))
            {
                var buf = pool.Rent(str.Length * 6);

                var numVerts = FontVertex.BuildString(buf, 0, str.Buffer.AsSpan(), FontFile.Default, 0, nextHeight, control.fontRenderer.Scaling, TextAlign.Left);

                qLinesToAdd.Enqueue(new LineVertices { Vertices = buf, Length = numVerts });

                Interlocked.Add(ref nextHeight, -lineHeight);

            }

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

            control.ClearViewport();

            var fr = control.fontRenderer;
            float fMargin = Margin;

            fr.ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(
                -fMargin, control.Width - fMargin,
                fMargin + baseHeight, fMargin + control.Height + baseHeight,
                -1, 1);

            foreach (ref var line in LineBuffers.AsSpan())
                fr.RenderText(line.Vertices, line.Length);
        }


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
                    nextHeight = baseHeight + control.Height - Margin - lineHeight;
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

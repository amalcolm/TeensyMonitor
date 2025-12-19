using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using PsycSerial;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography.Xml;
using System.Text;
using TeensyMonitor.Plotter.UserControls;

namespace TeensyMonitor.Plotter.Helpers
{
    public class MyPlot
    {

        static volatile int _instanceCounter = 0;
        private readonly int _instanceId = _instanceCounter++;

        public delegate double DataSelector(DataPacket data);

        public float ChannelScale { get; set; } = 0.0002f;

        public float  LastX    { get; private set; } = 0;
        public float  LastY    { get; private set; } = 0;
        public double Yscale   { get; set;         } = 0.0; // overridden by MyPlotter if 0.0.  If not overridden, use 1.0.
        public Color  Colour   { get; set;         } = MyColours.GetNextColour();
        public double XCounter { get; set;         } = -Math.Pow(2, 20) + 2; // X value counter, for signals without timestamps
                                                                             // Starts at a large negative value to avoid issues with float precision with ++;

        public DataSelector? Selector { get; set; }
        
        
        private MyChart? _parentChart;
        private readonly object _lock = new();

        // OpenGL handles
        private int _vboHandle;
        private int _vaoHandle;

        private int _subVAOHandle;
        private int _subVBOHandle;

        private int _subGridVBOHandle;
        private int _subGridVAOHandle;

        // Configuration
        private readonly int _bufferCapacity; // The total size of our vertex buffer
        private readonly int _historyLength;  // The number of recent points we want to keep contiguous for drawing

        // Data and state
        private readonly float[] _vertexData;
        private int _writeIndex = 0; // Where to write the next data point

        struct Vertex
        {
            public float X;
            public float Y;
            public float Z;
        }
        private Vertex[] latestBlock = [];  // or List<float>, but array is fine
        private readonly object blockLock = new();

        public MyPlot(int historyLength, MyGLControl myGL)
        {
            if (myGL is MyChart chart)
                _parentChart = chart;

            _historyLength = historyLength;

            // Make the buffer larger than the history to avoid copying every single frame
            _bufferCapacity = historyLength * 8 + Random.Shared.Next(0, historyLength);  // stagger refreshes
            
            _vertexData = new float[_bufferCapacity * 3];

            myGL.Setup(initAction:Init, shutdownAction:Shutdown);
        }


        private void Init()
        { 
            // --- One-time OpenGL Setup ---
            // 1. Create and bind a VAO
            _vaoHandle = GL.GenVertexArray();
            GL.BindVertexArray(_vaoHandle);

            // 2. Create a VBO and allocate memory on the GPU
            _vboHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboHandle);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                _vertexData.Length * sizeof(float),
                IntPtr.Zero, // Allocate memory, but don't upload data yet
                BufferUsageHint.DynamicDraw // Hint that we will be updating this buffer frequently
            );

            // 3. Configure vertex attributes
            // Tell OpenGL that our vertex data is arranged as 3 floats per vertex.
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // Unbind the VAO to prevent accidental changes
            GL.BindVertexArray(0);

            // repeat for subplot
            // 1. Configure vertex attributes for subplot
            _subVAOHandle = GL.GenVertexArray();
            GL.BindVertexArray(_subVAOHandle);

            // 2. Create its own VBO and allocate (you already had this)
            _subVBOHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _subVBOHandle);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                1024 * 3 * sizeof(float),  // Better: 1024 _vertices max, 3 floats each (bump if blocks are bigger)
                IntPtr.Zero,
                BufferUsageHint.DynamicDraw
            );

            // 3. Configure the SAME vertex layout for the sub VAO (while its VBO is bound)
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.BindVertexArray(0);

            _subGridVAOHandle = GL.GenVertexArray();
            GL.BindVertexArray(_subGridVAOHandle);

            // 2. Create its own VBO and allocate (you already had this)
            _subGridVBOHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _subGridVBOHandle);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                128 * 3 * sizeof(float),  // Better: 128 _vertices max, 3 floats each
                IntPtr.Zero,
                BufferUsageHint.DynamicDraw
            );

            // 3. Configure the SAME vertex layout for the sub VAO (while its VBO is bound)
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);


            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        public void Add(double y) => Add(XCounter++, y);

        private bool first = true;
        /// <summary>
        /// Adds a new Y data point to the plot. The X value is automatically incremented.
        /// </summary>
        public void Add(double x, double y)
        {
            if (first) { first = false; return; }

            double scale = Yscale == 0.0 ? 1.0 : Yscale;

            lock (_lock)
            {
                // When the buffer is full, copy the last block of data to the start.
                if (_writeIndex >= _bufferCapacity)
                {
                    int sourceIndex = (_bufferCapacity - _historyLength) * 3;
                    int length = _historyLength * 3;
                    Array.Copy(_vertexData, sourceIndex, _vertexData, 0, length);
                    _writeIndex = _historyLength;
                }

                float fX = (float)x;
                float fY = (float)(y * scale);

                _vertexData[_writeIndex * 3 + 0] = fX;
                _vertexData[_writeIndex * 3 + 1] = fY;
                _vertexData[_writeIndex * 3 + 2] = 0.0f;

                LastX = fX;
                LastY = fY;

                _writeIndex++;
            }
        }


        public void Add(BlockPacket packet, bool useRA, ref RunningAverage ra, DataSelector? selector = null)
        {
            if (first) { first = false; return; }

            lock (blockLock)  // or just outside the main lock
            {
                if (latestBlock.Length != packet.Count)
                    latestBlock = new Vertex[packet.Count];

                for (int i = 0; i < packet.Count; i++)
                {
                    latestBlock[i] = new Vertex
                    {
                        X = (float)packet.BlockData[i].StateTime * 1000.0f,  // milliseconds for subplot visibility
                        Y = (float)packet.BlockData[i].Channel[0],
                        Z = 0.0f
                    };
                }
            }

            lock (_lock)
            {
                double scale = Yscale == 0.0 ? 1.0 : Yscale;                     // debugQueue.Enqueue(new DebugPoint { SW = sw.Elapsed.TotalMilliseconds, Timestamp = packet.BlockData[0].TimeStamp });

                var today = DateTime.Today;
                for (int i = packet.Count-1; i < packet.Count; i++)
                {
                    ref var item = ref packet.BlockData[i];
                    double x = packet.BlockData[i].TimeStamp;
                    double y;
                    if (selector == null)
                    {
                        y = item.Channel[0] * ChannelScale;

                        if (i > 0)
                        {
                            uint c1 = item.Channel[0];
                            uint c0 = packet.BlockData[i - 1].Channel[0];
                            uint diff = (c0 > c1) ? (c0 - c1) : (c1 - c0);


                            if (c0 < 0x1000 && c0 > 0xFEFFFFF) { // 0's and 0xFF.... are invalid data
//                                Debug.WriteLine($"Skipping c0: 0x{c0:X8}");
                            }
                            else
                            if (c1 < 0x1000 && c1 > 0xFEFFFFF)
                            {
                                // skip diff check as this will be caught next iteration
                            }
                            else
                            if (diff > 0x00400000)
                                Debug.WriteLine($"c0: 0x{c0:X8}, c1: 0x{c1:X8}");

                        }

                    }
                    else
                        y = selector(item);

                    // When the buffer is full, copy the last block of data to the start.
                    if (_writeIndex >= _bufferCapacity)
                    {
                        int sourceIndex = (_bufferCapacity - _historyLength) * 3;
                        int length = _historyLength * 3;
                        Array.Copy(_vertexData, sourceIndex, _vertexData, 0, length);
                        _writeIndex = _historyLength;
                    }

                    float fX = (float)x;
                    float fY = (float)(y * scale);
                   

                    if (useRA)
                    {
                        ra.Add(fY);
                        fY = (fY - (float)ra.Average) * 10 + 512f; // Centered and scaled for visibility
                    }

                    _vertexData[_writeIndex * 3 + 0] = fX;
                    _vertexData[_writeIndex * 3 + 1] = fY;
                    _vertexData[_writeIndex * 3 + 2] = 0.0f;

                    LastX = fX;
                    LastY = fY;


                    _writeIndex++;
                }
            }
        }

        /// <summary>
        /// Renders the plot. Assumes the correct shader program is already active.
        /// </summary>
        public void Render() // No need for view parameters here now
        {
            if (_vaoHandle == 0 || _vboHandle == 0 || _writeIndex < 2) return;

            lock (_lock)
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, _vboHandle);
                // We only need to upload the part of the buffer that contains valid data
                GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, 3 * _writeIndex * sizeof(float), _vertexData);

                GL.BindVertexArray(_vaoHandle);

                // The magic: always one simple draw call.
                GL.DrawArrays(PrimitiveType.LineStrip, 0, _writeIndex);

                RenderLatestBlock();
            }
        }


        /// <summary>
        /// Releases the GPU resources (VBO and VAO).
        /// </summary>
        public void Shutdown()
        {
            lock (_lock)
            {
                if (_vboHandle != 0) GL.DeleteBuffer(_vboHandle);
                if (_vaoHandle != 0) GL.DeleteVertexArray(_vaoHandle);
                if (_subVBOHandle != 0) GL.DeleteBuffer(_subVBOHandle);
                if (_subVAOHandle != 0) GL.DeleteVertexArray(_subVAOHandle);
                if (_subGridVBOHandle != 0) GL.DeleteBuffer(_subGridVBOHandle);
                if (_subGridVAOHandle != 0) GL.DeleteVertexArray(_subGridVAOHandle);
            }
        }

        private const float ymin = 1000000.0f;
        private const float ymax = 5000000.0f;

        private float[] _vertices = new float[1024 * 3];
        private float[] _gridVertices = new float[128 * 3];
        private readonly int[] _viewport = new int[4];
        private int _maxGridCount = 0;
        private int _gridVertexCount = 0;

        private readonly Vertex[] block = new Vertex[1024];
        private void RenderLatestBlock()
        {
            if (_parentChart == null) return;
            int count = 0;
            lock (blockLock)
            {
                count = latestBlock.Length;
                if (count < 2) return;

                Array.Copy(latestBlock, block, count);
            }

            GL.GetInteger(GetPName.Viewport, _viewport);
            int vpWidth = _viewport[2];
            int vpHeight = _viewport[3];
            
            const int margin = 20;
            float sliceSize = vpWidth * 0.02f;
            
            int subW = (int)(sliceSize * _maxGridCount);
            int subH = (int)(vpHeight * 0.25f);

            GL.Scissor(margin, margin, subW, subH);
            GL.Enable(EnableCap.ScissorTest);

            GL.Viewport(margin, margin, subW, subH);

            // Key change: x now maps pixel width → 0 to count-1 logically
            // So short blocks get stretched, long ones compressed – consistent size on screen
            float xScale = (count - 1) / (subW - 1f);  // pixels per sample (approx)

            var transform = Matrix4.CreateOrthographicOffCenter(0f, 20.0F, ymin, ymax, -1f, 1f);

            int plotShader = _parentChart.GetPlotShader();
            int colorLoc = GL.GetUniformLocation(plotShader, "uColor");
            int transformLoc = GL.GetUniformLocation(plotShader, "uTransform");
            GL.UniformMatrix4(transformLoc, false, ref transform);


            // Draw grid first (now in separate method – clean!)
            if (_maxGridCount == 0)
            {
                BuildSubplotGrid();
            }
            else
            {   // Still set dim color and draw existing (potentially larger)
                GL.Uniform4(colorLoc, 0.35f, 0.35f, 0.35f, 0.28f);
                GL.BindVertexArray(_subGridVAOHandle);
                GL.DrawArrays(PrimitiveType.Lines, 0, _gridVertexCount);
                GL.BindVertexArray(0);
            }

            int idx = 0;
            for (int i = 0; i < count; i++)
            {
                _vertices[idx++] = block[i].X;
                _vertices[idx++] = block[i].Y;
                _vertices[idx++] = block[i].Z;
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, _subVBOHandle);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, count * 3 * sizeof(float), _vertices);

            GL.Uniform4(colorLoc, 0.0f, 0.3f, 0.3f, 1.0f);

            GL.BindVertexArray(_subVAOHandle);
            GL.DrawArrays(PrimitiveType.LineStrip, 0, count);
            GL.BindVertexArray(0);

            GL.Disable(EnableCap.ScissorTest);
            GL.Viewport(0, 0, vpWidth, vpHeight);
        }

        private void BuildSubplotGrid()
        {
            if (_parentChart == null) return;

            int count = 20;
            // Faint vertical lines at every sample + top/bottom horizontals
            _gridVertexCount = ((count + 1) * 2) + 4;  // 2 per vertical + 4 for top/bottom lines
            if (_gridVertices.Length < _gridVertexCount * 3)
                Array.Resize(ref _gridVertices, _gridVertexCount * 3 * 2);  // plenty of room

            int idx = 0;

            // Vertical lines
            for (int i = 0; i <= count; i++)
            {
                float x = i;
                _gridVertices[idx++] = x; _gridVertices[idx++] = ymin; _gridVertices[idx++] = 0.0f;
                _gridVertices[idx++] = x; _gridVertices[idx++] = ymax; _gridVertices[idx++] = 0.0f;
            }

            // Horizontal lines
            _gridVertices[idx++] = 0.0f;  _gridVertices[idx++] = ymax; _gridVertices[idx++] = 0.0f;
            _gridVertices[idx++] = count; _gridVertices[idx++] = ymax; _gridVertices[idx++] = 0.0f;
            _gridVertices[idx++] = 0.0f;  _gridVertices[idx++] = ymin; _gridVertices[idx++] = 0.0f;
            _gridVertices[idx++] = count; _gridVertices[idx++] = ymin; _gridVertices[idx++] = 0.0f;

            // Upload grid
            GL.BindBuffer(BufferTarget.ArrayBuffer, _subGridVBOHandle);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _gridVertexCount * 3 * sizeof(float), _gridVertices);

            int plotShader = _parentChart.GetPlotShader();
            int colorLoc = GL.GetUniformLocation(plotShader, "uColor");

            // Dim subtle gray
            GL.Uniform4(colorLoc, 0.35f, 0.35f, 0.35f, 0.28f);

            GL.BindVertexArray(_subGridVAOHandle);
            GL.DrawArrays(PrimitiveType.Lines, 0, idx/3);
            GL.BindVertexArray(0);

            _maxGridCount = count;
        }

        // static readonly Stopwatch sw = Stopwatch.StartNew();
        // struct DebugPoint
        // {
        //     public double SW;
        //     public double Timestamp;
        // }
        // readonly ConcurrentQueue<DebugPoint> debugQueue = [];


        // public string getDebugOutput()
        // {
        //     StringBuilder sb = new();
        //     sb.AppendLine($"MyPlot Instance {_instanceId} Debug Output:");
        //     foreach (var dp in debugQueue)
        //         sb.AppendLine($"SW: {dp.SW:F2} ms, Timestamp: {dp.Timestamp * 1000.0:F2} ms");
        //
        //     return sb.ToString();
        // }

    }
}
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
        public MyChart? ParentChart { get; set; }

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

        private readonly object _lock = new();

        // OpenGL handles
        private int _vboHandle;
        private int _vaoHandle;

        private int _subVBOHandle;

        // Configuration
        private readonly int _bufferCapacity; // The total size of our vertex buffer
        private readonly int _historyLength;  // The number of recent points we want to keep contiguous for drawing

        // Data and state
        private readonly float[] _vertexData;
        private int _writeIndex = 0; // Where to write the next data point

        private List<float> latestBlock = [];  // or List<float>, but array is fine
        private readonly object blockLock = new();

        public MyPlot(int historyLength, MyGLControl myGL)
        {
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

            _subVBOHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _subVBOHandle);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                1024 * sizeof(float),  // way too big, but whatever :) 
                IntPtr.Zero, // Allocate memory, but don't upload data yet
                BufferUsageHint.DynamicDraw // Hint that we will be updating this buffer frequently
            );


            // 3. Configure vertex attributes
            // Tell OpenGL that our vertex data is arranged as 3 floats per vertex.
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            // Unbind the VAO to prevent accidental changes
            GL.BindVertexArray(0);

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

            lock (_lock)
            {
                double scale = Yscale == 0.0 ? 1.0 : Yscale;                     // debugQueue.Enqueue(new DebugPoint { SW = sw.Elapsed.TotalMilliseconds, Timestamp = packet.BlockData[0].TimeStamp });

                if (latestBlock.Count != packet.Count)
                    latestBlock = new List<float>(packet.Count);

                for (int i = 0; i < packet.Count; i++)
                    latestBlock[i] = (float)packet.BlockData[i].Channel[0];

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
            }
        }
        readonly float ymin = 1000000.0f;
        readonly float ymax = 3500000.0f;

        float[] vertices = new float[1024 * 3];

        private void RenderLatestBlock()
        {
            if (ParentChart == null) return;

            float[] block;
         
            if (latestBlock.Count < 2) return;
            block = latestBlock.ToArray();  // shallow copy is fine since we don't modify

            int count = block.Length;

            // Grab current viewport for sizing
            int[] viewport = ParentChart.GetViewport();
            GL.GetInteger(GetPName.Viewport, viewport);
            int vpWidth = viewport[2];
            int vpHeight = viewport[3];

            // Define subplot area: bottom-left, say 30% width, 25% height, with small margin
            float margin = 20f;
            float subWidth = vpWidth * 0.30f - margin * 2;
            float subHeight = vpHeight * 0.25f - margin * 2;

            // Set scissor to clip drawing to this rect only (nice clean box)
            GL.Scissor((int)margin, (int)margin, (int)subWidth, (int)subHeight);
            GL.Enable(EnableCap.ScissorTest);

            // Simple dark semi-transparent background
            GL.Viewport((int)margin, (int)margin, (int)subWidth, (int)subHeight);
            GL.ClearColor(0.05f, 0.05f, 0.1f, 0.7f);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            // Setup ortho projection for this subplot: x from 0 to count-1, y auto-scaled
            var transform = Matrix4.CreateOrthographicOffCenter(0f, count - 1, ymin, ymax, -1f, 1f);
            // You'll need to calculate ymin/ymax – see below

            int _plotShaderProgram = ParentChart.GetPlotShader();

            int transformLocation = GL.GetUniformLocation(_plotShaderProgram, "uTransform");
            GL.UniformMatrix4(transformLocation, false, ref transform);

            // Build vertices exactly like your main MyPlot does
            for (int i = 0, j = 0; i < count; i++)
            {
                float x = i;
                float y = block[i]; 
                vertices[j++] = x;
                vertices[j++] = y;
                vertices[j++] = 0.0f;

            }

            // Upload to VBO/VAO the same way MyPlot does (reuse your buffer upload code)

            GL.BindBuffer(BufferTarget.ArrayBuffer, _subVBOHandle);
            // We only need to upload the part of the buffer that contains valid data
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, count * 3 * sizeof(float), vertices);

            GL.DrawArrays(PrimitiveType.LineStrip, 0, count);

            // Optional: thin grid or axis lines here if you want

            // Clean up
            GL.Disable(EnableCap.ScissorTest);
            // Restore main viewport
            GL.Viewport(0, 0, vpWidth, vpHeight);
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
using OpenTK.Graphics.OpenGL4;
using PsycSerial;

namespace TeensyMonitor.Plotter.Helpers
{
    /// <summary>
    /// Represents a single vertex in 3D space.
    /// </summary>
    public struct Vertex(float x, float y, float z) { public float X = x, Y = y, Z = z; }


    /// <summary>
    /// Creates a fixed-size vertex buffer for a given number of vertices.
    /// </summary>
    /// <param name="vertexCapacity">Maximum vertex count to allocate space for.</param>
    /// <param name="stride">Number of floats per vertex (e.g. 3 for XYZ).</param>
    public class MyGLVertexBuffer(int vertexCapacity, int stride = 3) : IDisposable
    {
        public float ChannelScale { get; set; } = 0.0002f;
        public int VertexCount { get => _vertexCount; }


        private float[] _vertexData = new float[vertexCapacity * stride];
        private int _vao;
        private int _vbo;
        private bool _disposed;

        private int _vertexCount = 0;

        public int WindowSize { get; set; } = -1;

        private object _lock = new();

        public void Init()
        { 
            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();

            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);

            // Allocate GPU memory
            GL.BufferData(BufferTarget.ArrayBuffer,
                vertexCapacity * stride * sizeof(float),
                IntPtr.Zero,
                BufferUsageHint.DynamicDraw);

            // Default vertex attribute layout (location 0 = vec3 position)
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(
                index: 0,
                size: stride,
                type: VertexAttribPointerType.Float,
                normalized: false,
                stride: stride * sizeof(float),
                offset: 0);

            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        public void Clear()
        {
            lock (_lock)
            {
                _vertexCount = 0;
            }
        }

        private void AddUnderLock(float x, float y, float z)
        {
            int baseIndex = _vertexCount * stride;
            _vertexData[baseIndex    ] = x;
            _vertexData[baseIndex + 1] = y;
            _vertexData[baseIndex + 2] = z;
            _vertexCount++;
        }

        public void AddVertex(float x, float y, float z)
        {
            lock (_lock)
            {
                CheckSize();

                AddUnderLock(x, y, z);
            }
        }

        public void AddBlock(ref BlockPacket packet, FieldEnum? selector, bool onlyLast)
        {
            lock (_lock)
            {
                int start = onlyLast ? packet.Count - 1 : 0;
                for (int i = start; i < packet.Count; i++)
                {
                    CheckSize();

                    float x = (float)packet.BlockData[i].TimeStamp;
                    float y = (selector == null) ? (float)packet.BlockData[i].Channel[0] * ChannelScale + 40.0f 
                                                 : (float)packet.BlockData[i].get(selector.Value);

                    AddUnderLock(x, y, 0.0f);
                
                }

            }
        }

        private float[] _latestX = new float[16];
        private int _latestXCount = 0;

        public int LatestXCount => _latestXCount;
        public ReadOnlySpan<float> LatestX => _latestX.AsSpan(0, _latestXCount);

        public int Count => _vertexCount;


        void CheckSize()
        {
            if (_vertexCount < vertexCapacity) return;
            int windowMax = WindowSize - 1;

            // Shift data left to make room for new vertex
            Array.Copy(_vertexData, (vertexCapacity - windowMax) * stride, _vertexData, 0, windowMax * stride);
            _vertexCount = windowMax;
        }

        float[] subPlotData = new float[1024 * 3];
        public void SetBlock(BlockPacket block)
        {
            if (subPlotData.Length < block.Count * 3)
                subPlotData = new float[block.Count * 3 * 2];

            for (int i = 0; i < block.Count; i++)
            {
                int baseIndex = i * 3;

                subPlotData[baseIndex] = (float)block.BlockData[i].StateTime * 1000.0f;  // milliseconds for subplot visibility
                subPlotData[baseIndex + 1] = (float)block.BlockData[i].Channel[0];
                subPlotData[baseIndex + 2] = 0.0f;
            }

            Set(ref subPlotData, block.Count);

        }

        /// <summary>
        /// Replaces the buffer content with new data.
        /// </summary>
        /// <param name="data">Flat array of floats (must be multiple of stride)</param>
        /// <param name="vertexCount">Number of vertices (not floats!)</param>
        public void Set(ref float[] data, int vertexCount)
        {
            if (vertexCount > vertexCapacity)       throw new ArgumentOutOfRangeException(nameof(vertexCount));

            if (data.Length < vertexCount * stride) throw new ArgumentException("Data array too small for vertex count");

            _vertexCount = vertexCount;
            // Copy only the used portion
            Array.Copy(data, _vertexData, vertexCount * stride);

            _latestXCount = vertexCount;
            if (_latestX.Length < _latestXCount)
                _latestX = new float[_latestXCount * 2];

            for (int i = 0; i < _latestXCount; i++)
                _latestX[i] = _vertexData[i * stride];
        }

        /// <summary>
        /// Uploads the current CPU vertex data to the GPU.
        /// Call this after you've finished adding vertices for the frame.
        /// </summary>
        public void Upload()
        {
            if (_vertexCount < 2) return;

            GL.BindBuffer   (BufferTarget.ArrayBuffer, _vbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _vertexCount * stride * sizeof(float), _vertexData);
            GL.BindBuffer   (BufferTarget.ArrayBuffer, 0);
        }


        /// <summary>
        /// Draws the buffer using GL_LINES.
        /// </summary>
        public void DrawLines()
        {
            if (_vertexCount < 2) return;

            Upload();

            GL.BindVertexArray(_vao);
            GL.DrawArrays(PrimitiveType.Lines, 0, _vertexCount);
            GL.BindVertexArray(0);
        }

        /// <summary>
        /// Draws the buffer using GL_LINE_STRIP.
        /// </summary>
        public void DrawLineStrip()
        {
            if (_vertexCount < 2) return;

            Upload();

            GL.BindVertexArray(_vao);
            GL.DrawArrays(PrimitiveType.LineStrip, 0, _vertexCount);
            GL.BindVertexArray(0);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            GL.DeleteBuffer(_vbo);
            GL.DeleteVertexArray(_vao);
            GC.SuppressFinalize(this);
        }
    }
}

using OpenTK.Graphics.OpenGL4;
using PsycSerial;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TeensyMonitor.Plotter.Helpers
{
    /// <summary>
    /// Represents a single vertex in 3D space.
    /// </summary>
    struct Vertex(float x, float y, float z)
    {
        public float X = x;
        public float Y = y;
        public float Z = z;
    }

    /// <summary>
    /// Selects a specific data point from a data packet.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public delegate double DataSelector(DataPacket data);



    /// <summary>
    /// Creates a fixed-size vertex buffer for a given number of vertices.
    /// </summary>
    /// <param name="vertexCapacity">Maximum vertex count to allocate space for.</param>
    /// <param name="stride">Number of floats per vertex (e.g. 3 for XYZ).</param>
    public class MyGLVertexBuffer(int vertexCapacity, int stride = 3) : IDisposable
    {
        private readonly int _vertexCapacity = vertexCapacity;
        private readonly int _stride = stride;

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
                _vertexCapacity * _stride * sizeof(float),
                IntPtr.Zero,
                BufferUsageHint.DynamicDraw);

            // Default vertex attribute layout (location 0 = vec3 position)
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(
                index: 0,
                size: _stride,
                type: VertexAttribPointerType.Float,
                normalized: false,
                stride: _stride * sizeof(float),
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

        public void AddVertex(float x, float y, float z)
        {
            lock (_lock)
            {
                CheckSize();

                int baseIndex = _vertexCount * _stride;
                _vertexData[baseIndex    ] = x;
                _vertexData[baseIndex + 1] = y;
                _vertexData[baseIndex + 2] = z;

                _vertexCount++;
            }
        }

        public void AddBlock(ref BlockPacket packet, DataSelector? selector)
        {
            lock (_lock)
            {
                for (int i = 0; i < packet.Count; i++)
                {
                    CheckSize();

                    float x = (float)packet.BlockData[i].TimeStamp;
                    float y = (selector == null) ? (float)packet.BlockData[i].Channel[0] 
                                                 : (float)selector(packet.BlockData[i]);

                    int baseIndex = _vertexCount * _stride;
                    _vertexData[baseIndex    ] = x;
                    _vertexData[baseIndex + 1] = y;
                    _vertexData[baseIndex + 2] = 0.0f;

                    _vertexCount++;
                }
            }
        }

        public int Count => _vertexCount;


        void CheckSize()
        {
            if (_vertexCount >= _vertexCapacity)
            {
                if (WindowSize < 0) throw new InvalidOperationException("Vertex buffer is full.");

                // Shift data left to make room for new vertex
                Array.Copy(_vertexData, _vertexCapacity - WindowSize * _stride, _vertexData, 0, WindowSize * _stride);
                _vertexCount = WindowSize;
            }
        }
        /// <summary>
        /// Uploads vertex data to the GPU. 
        /// The number of vertices becomes the current draw count.
        /// </summary>
        public void Set(ref float[] data)
        {
            if (data.Length % _stride != 0)
                throw new ArgumentException("Data length must be a multiple of stride.", nameof(data));

            _vertexCount = data.Length / _stride;

            if (_vertexCount > _vertexCapacity)
                throw new ArgumentOutOfRangeException(nameof(data),
                    "Data exceeds allocated vertex capacity.");

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, data.Length * sizeof(float), data);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        /// <summary>
        /// Uploads the current CPU vertex data to the GPU.
        /// Call this after you've finished adding vertices for the frame.
        /// </summary>
        public void Upload()
        {
            if (_vertexCount == 0) return;

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _vertexCount * _stride * sizeof(float), _vertexData);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }


        /// <summary>
        /// Draws the buffer using GL_LINES.
        /// </summary>
        public void DrawLines()
        {
            if (_vertexCount == 0) return;

            GL.BindVertexArray(_vao);
            GL.DrawArrays(PrimitiveType.Lines, 0, _vertexCount);
            GL.BindVertexArray(0);
        }

        /// <summary>
        /// Draws the buffer using GL_LINE_STRIP.
        /// </summary>
        public void DrawLineStrip()
        {
            if (_vertexCount == 0) return;

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

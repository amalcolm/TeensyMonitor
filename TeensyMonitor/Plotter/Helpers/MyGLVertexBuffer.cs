using OpenTK.Graphics.OpenGL4;
using PsycSerial;
using System.Runtime.InteropServices;
using OpenTK.Mathematics;

namespace TeensyMonitor.Plotter.Helpers
{
    /// <summary>
    /// Represents a single vertex in 3D space.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct Vertex
    {
        public Vector4 Position; // location 0
        public Vector4 Normal;   // location 1
        public Vector4 Colour;   // location 2
        public Vector2 uv0;      // location 3
        public Vector2 uv1;      // location 4

        public Vertex(Vector4 position)
        {
            Position = position;
            Normal   = Vector4.UnitZ;
            Colour   = Vector4.One;
            uv0      = Vector2.Zero;
            uv1      = Vector2.Zero;
        }
    
        public Vertex(float x, float y, float z)
        {
            Position = new Vector4(x, y, z, 1.0f);
            Normal   = Vector4.UnitZ;
            Colour   = Vector4.One;
            uv0      = Vector2.Zero;
            uv1      = Vector2.Zero;
        }

        public Vertex(float x, float y, float z, float r, Color colour)
        {
            Position = new Vector4(x, y, z, 1.0f);
            Normal   = Vector4.UnitZ;
            Colour   = new Vector4(colour.R / 255.0f, colour.G / 255.0f, colour.B / 255.0f, 1.0f);
            uv0      = Vector2.Zero;
            uv1      = Vector2.Zero;
        }

        public static readonly int Size = Marshal.SizeOf<Vertex>();
    }

    /// <summary>
    /// Creates a fixed-size vertex buffer for a given number of vertices.
    /// </summary>
    /// <param name="vertexCapacity">Maximum vertex count to allocate space for.</param>
    /// <param name="stride">Number of floats per vertex (e.g. 3 for XYZ).</param>
    public class MyGLVertexBuffer(int vertexCapacity) : IDisposable
    {
        public int VertexCount { get => _vertexCount; }
  
        private Vertex[] _vertexData = new Vertex[vertexCapacity];
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


            
            // Allocate GPU memory (bytes)
            GL.BufferData(BufferTarget.ArrayBuffer,
                vertexCapacity * Vertex.Size,
                IntPtr.Zero,
                BufferUsageHint.DynamicDraw);

            /* Attribute 0: vec4 position */  GL.EnableVertexAttribArray(0); GL.VertexAttribPointer( index: 0, size: 4, type: VertexAttribPointerType.Float, normalized: false, stride: Vertex.Size, (IntPtr)Marshal.OffsetOf<Vertex>(nameof(Vertex.Position)));
            /* Attribute 1: vec4 normal   */  GL.EnableVertexAttribArray(1); GL.VertexAttribPointer( index: 1, size: 4, type: VertexAttribPointerType.Float, normalized: false, stride: Vertex.Size, (IntPtr)Marshal.OffsetOf<Vertex>(nameof(Vertex.Normal  )));
            /* Attribute 2: vec4 colour   */  GL.EnableVertexAttribArray(2); GL.VertexAttribPointer( index: 2, size: 4, type: VertexAttribPointerType.Float, normalized: false, stride: Vertex.Size, (IntPtr)Marshal.OffsetOf<Vertex>(nameof(Vertex.Colour  )));
            /* Attribute 3: vec2 uv0      */  GL.EnableVertexAttribArray(3); GL.VertexAttribPointer( index: 3, size: 2, type: VertexAttribPointerType.Float, normalized: false, stride: Vertex.Size, (IntPtr)Marshal.OffsetOf<Vertex>(nameof(Vertex.uv0     )));
            /* Attribute 4: vec2 uv1      */  GL.EnableVertexAttribArray(4); GL.VertexAttribPointer( index: 4, size: 2, type: VertexAttribPointerType.Float, normalized: false, stride: Vertex.Size, (IntPtr)Marshal.OffsetOf<Vertex>(nameof(Vertex.uv1     )));
            
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
        {;
            _vertexData[_vertexCount] = new Vertex { Position = new Vector4(x, y, z, 1.0f) };
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
                    float y = (selector == null) ? (float)(packet.BlockData[i].Channel[0] * Config.C0to1024) 
                                                 : (float)(packet.BlockData[i].get(selector.Value)         );

                    AddUnderLock(x, y, 0.0f);
                
                }

            }
        }

        private float[] _latestX = new float[16];
        private int _latestXCount = 0;

        public int LatestXCount => _latestXCount;
        public ReadOnlySpan<float> LatestX => _latestX.AsSpan(0, _latestXCount);

        public void getLatestX(out Span<float> dest, out int count)
        {
            dest = _latestX.AsSpan(0, _latestXCount);
            count = _latestXCount;
        }

        public int Count => _vertexCount;


        void CheckSize()
        {
            if (_vertexCount < vertexCapacity || WindowSize < 0) return;
            int windowMax = WindowSize - 1;

            // Shift data left to make room for new vertex
            Array.Copy(_vertexData, (vertexCapacity - windowMax), _vertexData, 0, windowMax);
            _vertexCount = windowMax;
        }

        Vertex[] subPlotData = new Vertex[1024];
        public void SetBlock(BlockPacket block, FieldEnum field, double scale)
        {
            if (subPlotData.Length < block.Count)
                subPlotData = new Vertex[block.Count * 2];

            if (field == FieldEnum.Events)
            {
                SetEvents(block, scale);
                return;
            }

            for (int i = 0; i < block.Count; i++)
            {
                double value = block.BlockData[i].get(field) * scale;

                float x = (float)block.BlockData[i].StateTime * 1000.0f;  // milliseconds for subplot visibility
                float y = (float)value;
                float z = 0.0f;
                subPlotData[i] = new Vertex(x, y, z);
            }

            Set(ref subPlotData, block.Count);

        }

        private void SetEvents(BlockPacket block, double scale)
        {
            Clear();
            for (int i = 0; i < block.NumEvents; i++)
            {
                var ev = block.EventData[i];
                if (ev.Kind == EventKind.NONE) continue;

                ref Vertex v = ref _vertexData[_vertexCount];

                double value = 1024.0 * scale;
                v.Position.X = (float)ev.StateTime * 1000.0f;  // milliseconds for subplot visibility
                v.Position.Y = (float)ev.Kind;
                _vertexCount++;
                CheckSize();
            }
        }

        /// <summary>
        /// Replaces the buffer content with new data.
        /// </summary>
        /// <param name="data">Flat array of floats (must be multiple of stride)</param>
        /// <param name="vertexCount">Number of vertices (not floats!)</param>
        public void Set(ref Vertex[] data, int vertexCount)
        {
            if (vertexCount > vertexCapacity)       throw new ArgumentOutOfRangeException(nameof(vertexCount));
            if (data.Length < vertexCount) throw new ArgumentException("Data array too small for vertex count");

            _vertexCount = vertexCount;
            // Copy only the used portion
            Array.Copy(data, _vertexData, vertexCount);

            _latestXCount = vertexCount;
            if (_latestX.Length < _latestXCount)
                _latestX = new float[_latestXCount * 2];

            for (int i = 0; i < _latestXCount; i++)
                _latestX[i] = _vertexData[i].Position.X;
        }

        /// <summary>
        /// Uploads the current CPU vertex data to the GPU.
        /// Call this after you've finished adding vertices for the frame.
        /// </summary>
        public void Upload()
        {
            if (_vertexCount < 2) return;

            GL.BindBuffer   (BufferTarget.ArrayBuffer, _vbo);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _vertexCount * Vertex.Size, _vertexData);
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

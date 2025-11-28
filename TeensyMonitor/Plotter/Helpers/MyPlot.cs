using OpenTK.Graphics.OpenGL4;
using TeensyMonitor.Plotter.UserControls;

namespace TeensyMonitor.Plotter.Helpers
{
    public class MyPlot
    {
        public float LastX { get; private set; } = 0;
        public Color Colour { get; set; } = MyColours.GetNextColour();
        public double XCounter { get; set; } = -Math.Pow(2, 22) - 2; // X value counter, for signals without timestamps

        private readonly object _lock = new();

        // OpenGL handles
        private int _vboHandle;
        private int _vaoHandle;

        // Configuration
        private readonly int _bufferCapacity; // The total size of our vertex buffer
        private readonly int _historyLength;  // The number of recent points we want to keep contiguous for drawing

        // Data and state
        private readonly float[] _vertexData;
        private int _writeIndex = 0; // Where to write the next data point

        public MyPlot(int historyLength, MyGLControl myGL)
        {
            _historyLength = historyLength;
            // Make the buffer larger than the history to avoid copying every single frame
            _bufferCapacity = (int)(historyLength * 1.5);
            _vertexData = new float[_bufferCapacity * 3];

            myGL.Enqueue(Init, Shutdown);
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
        }

        public void Add(double y) => Add(XCounter++, y);

        /// <summary>
        /// Adds a new Y data point to the plot. The X value is automatically incremented.
        /// </summary>
        public void Add(double x, double y)
        {
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
                LastX = fX;

                _vertexData[_writeIndex * 3 + 0] = fX;
                _vertexData[_writeIndex * 3 + 1] = (float)y;
                _vertexData[_writeIndex * 3 + 2] = 0.0f;

                _writeIndex++;
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
                GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, _writeIndex * 3 * sizeof(float), _vertexData);

                GL.BindVertexArray(_vaoHandle);

                // The magic: always one simple draw call.
                GL.DrawArrays(PrimitiveType.LineStrip, 0, _writeIndex);
            }
        }


        /// <summary>
        /// Releases the GPU resources (VBO and VAO).
        /// </summary>
        public void Shutdown()
        {
  //          lock (_lock)
            {
                if (_vboHandle != 0) GL.DeleteBuffer(_vboHandle);
                if (_vaoHandle != 0) GL.DeleteVertexArray(_vaoHandle);
            }
        }
    }
}
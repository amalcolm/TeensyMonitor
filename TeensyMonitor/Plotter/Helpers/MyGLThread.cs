using OpenTK.GLControl;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace TeensyMonitor.Plotter.Helpers
{
    //// <summary>
    /// Manages a dedicated thread for OpenGL rendering to ensure smooth,
    /// non-blocking UI performance.It handles task queuing, frame pacing,
    /// and graceful shutdown.
    //// </summary>
    public class MyGLThread : IDisposable
    {
        public Action? RenderAction
        {
            get => _renderAction;
            set
            {
                if (_isRunning) throw new InvalidOperationException("Cannot change RenderAction while the thread is running.");

                _renderAction = value;
            }
        }
        private Action? _renderAction;


        private readonly Thread _thread;
        private readonly CancellationTokenSource _cts = new();

        // A queue for one-off actions to be executed on the GL thread (e.g., creating a VBO).
        private readonly ConcurrentQueue<Action> _taskQueue = new();

        // A stack for shutdown actions, ensuring LIFO cleanup (e.g., disposing VBOs before the context).
        private readonly ConcurrentStack<Action> _shutdownStack = new();

        private readonly ManualResetEventSlim _frameDone = new(initialState: true);
        public volatile bool _shutdownRequested;

        private readonly GLControl _glControl;

        private volatile bool _isRunning = false;
        private int RefreshRate;
        public MyGLThread(GLControl glControl)
        {
            _glControl = glControl;

            _thread = new(Run)
            {
                IsBackground = true,
                Name = "MyGLThread",
                Priority = ThreadPriority.AboveNormal
            };

            _glControl.HandleCreated += (s,e) =>
            {
                // Ensure the GLControl is initialized before starting the thread
                if (_glControl.Context == null)
                    throw new InvalidOperationException("GLControl context is not initialized.");

                RefreshRate = ScreenHelper.GetCurrentRefreshRate(_glControl);

                _glControl.Context.MakeNoneCurrent();

                _isRunning = true;
                _thread.Start();
            };
        }

        //// <summary>
        /// Starts the rendering thread with a specific action to be performed each frame.
        //// </summary>
        /// <param name="initAction">An optional one-time setup action (e.g., GL.Enable states).</param>
        /// <param name="shutdownAction">An optional action to be run when the thread disposes.</param>
        public bool Enqueue(Action? initAction = null, Action? shutdownAction = null)
        {
            if (_shutdownRequested) return false;

            if (    initAction != null) _taskQueue .Enqueue(    initAction);
            if (shutdownAction != null) _shutdownStack.Push(shutdownAction);
            
            return true;
        }


        /// <summary>
        /// Enqueues a task to be executed on the GL thread.
        /// </summary>
        public void Invoke(Action action)
        {
            if (!_isRunning || _cts.IsCancellationRequested) return;

            _taskQueue.Enqueue(action);
        }

        private void Run()
        {
            var mainTimer = Stopwatch.StartNew();

            int second = 0;
            
            var stopwatch = new Stopwatch();
            double targetFrameTime = 1000.0 / RefreshRate; 
            long nTotalFrames = 0, nFramesThisSecond = 0;
            try
            {
                _glControl.MakeCurrent();                                     if (_glControl.Context == null) throw new InvalidOperationException("GLControl context is not initialized.");
                _glControl.Context.SwapInterval = 0;

                while (!_cts.IsCancellationRequested)
                {
                    _frameDone.Reset();
                    stopwatch.Restart();

                    try
                    {
                        RenderAction?.Invoke();
                        nTotalFrames++;
                        nFramesThisSecond++;
                        do
                        {
                            while (_taskQueue.TryDequeue(out var action))
                                action.Invoke();
                        }
                        while (stopwatch.Elapsed.TotalMilliseconds < targetFrameTime && !_cts.IsCancellationRequested);

                        int currentSeconds = (int)mainTimer.Elapsed.TotalSeconds;
                        if (currentSeconds != second)
                        {
                            second = currentSeconds;   // Debug.WriteLine($"[MyGLThread] FPS: {nFramesThisSecond}");
                            nFramesThisSecond = 0;
                        }
                    }
                    finally
                    {
                        _frameDone.Set();
                    }
                }
            }
//            catch (Exception ex) { Debug.WriteLine($"[MyGLThread] Exception: {ex.Message}"); }
            finally
            {
                while (_shutdownStack.TryPop(out var action))
                    action.Invoke();
            }
        }

        public void Dispose()
        {
            if (!_isRunning) return;
            _cts.Cancel();
            _isRunning = false;
            _shutdownRequested = true;
            _frameDone.Wait();
            try { _thread.Join(); } catch { /* ignored */ }

            _cts.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
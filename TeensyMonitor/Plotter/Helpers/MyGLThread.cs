using OpenTK.GLControl;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

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

        public bool AutoStart { get; set; } = true;

        private readonly Thread _thread;
        private readonly CancellationTokenSource _cts = new();

        // A queue for one-off actions to be executed on the GL thread (e.g., creating a VBO).
        private readonly BlockingCollection<Action> _tasksToDo = [];

        // A stack for shutdown actions, ensuring LIFO cleanup (e.g., disposing VBOs before the context).
        private readonly ConcurrentStack<Action> _shutdownStack = new();

        private readonly ManualResetEventSlim _frameDone = new(initialState: true);
        public volatile bool _shutdownRequested;

        private readonly GLControl _glControl;

        private volatile bool _isRunning = false;
        private int RefreshRate;

        public bool IsDisposed => _shutdownRequested;

        public void RenderOnce()
        {
            if (_glControl.IsDisposed || _isRunning) return;

            _glControl.MakeCurrent();

            // process enqueued work
            while (_tasksToDo.TryTake(out var task, 0))
            {
                try { task.Invoke(); }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Render task failed: {ex.Message}");
                }
            }

            RenderAction?.Invoke();

            _glControl.SwapBuffers();
        }


        static readonly List<MyGLThread> ActiveGLThreads = [];

        public MyGLThread(GLControl glControl)
        {
            ActiveGLThreads.Add(this);

            _glControl = glControl;

            Scheduler.Register(this);

            _thread = new(Run)
            {
                IsBackground = true,
                Name = $"MyGLThread_{ActiveGLThreads.Count:01}",
                Priority = ThreadPriority.Highest
            };


            _glControl.HandleCreated += (s,e) =>
            {
                Debug.WriteLine($"[MyGLThread] Staring GL thread for {RenderAction?.Target?.GetType().Name}.");

                // Ensure the GLControl is initialized before starting the thread
                if (_glControl.Context == null)
                    throw new InvalidOperationException("GLControl context is not initialized.");


                RefreshRate = ScreenHelper.GetCurrentRefreshRate(_glControl);

                _glControl.Context.MakeNoneCurrent();

                if (AutoStart)
                {
                    _isRunning = true;
                    _thread.Start();
                }
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

            if (    initAction != null) _tasksToDo    .Add (    initAction);
            if (shutdownAction != null) _shutdownStack.Push(shutdownAction);
            
            return true;
        }


        /// <summary>
        /// Enqueues a task to be executed on the GL thread.
        /// </summary>
        public void Invoke(Action action)
        {
            if (!_isRunning || _cts.IsCancellationRequested) return;

            _tasksToDo.Add(action);
        }

        private void Run()
        {
            var mainTimer = Stopwatch.StartNew();

            int second = 0;
            
            var stopwatch = new Stopwatch();
            long framePeriod_Ticks = Stopwatch.Frequency / RefreshRate;
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
                        try
                        {
                            _glControl.MakeCurrent();                        if (_glControl.Context == null) throw new InvalidOperationException("GLControl context is not initialized.");
                            RenderAction?.Invoke();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[MyGLThread] RenderAction Exception: {ex.Message}");
                        }
                        finally
                        {
                            _glControl.SwapBuffers();
                        }
                        
                        nTotalFrames++;
                        nFramesThisSecond++;

                        while (stopwatch.ElapsedTicks < framePeriod_Ticks && !_cts.IsCancellationRequested)
                        {
                            TimeSpan remaining = TimeSpan.FromTicks(framePeriod_Ticks - stopwatch.ElapsedTicks);
                            if (remaining <= TimeSpan.Zero) break;

                            if (_tasksToDo.TryTake(out var action, remaining))
                                do
                                {
                                    try { action.Invoke(); }
                                    catch (Exception ex) { Debug.WriteLine($"[MyGLThread] Task Exception: {ex.Message}"); }

                                } while (_tasksToDo.TryTake(out action, 0));  // drain burst non-blocking
                        }

                        int currentSeconds = (int)mainTimer.Elapsed.TotalSeconds;
                        if (currentSeconds != second)
                        {
                            second = currentSeconds;   // Debug.WriteLine($"[MyGLThread] FPS: {nFramesThisSecond}");
                        //    Debug.WriteLine($"[MyGLThread] Total Frames: {nTotalFrames}, FPS: {nFramesThisSecond}, Frame Time: {frameTime:F5} ms");
                            nFramesThisSecond = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[MyGLThread] Main Loop Exception: {ex.Message}");
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


            Debug.WriteLine($"[MyGLThread] Exiting GL thread for {RenderAction?.Target?.GetType().Name}.");
        }

        public void Dispose()
        {
            if (!_isRunning || _shutdownRequested) return;
            _cts.Cancel();
            _isRunning = false;
            _shutdownRequested = true;

            _frameDone.Wait();

            try { _thread.Join(); } catch { /* ignored */ }

            _cts.Dispose();
            GC.SuppressFinalize(this);

            Scheduler.Unregister(this);
        }
    }
}
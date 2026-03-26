
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace TeensyMonitor.Plotter.Helpers
{
    public static class Scheduler
    {
        private static readonly List<MyGLThread> _threads = [];
        private static readonly object _lock = new();
        private static CancellationTokenSource? cts = null;

        private static ConcurrentQueue<MyGLThread> _pendingThreads = new();
        private static ConcurrentQueue<MyGLThread> _exitingThreads = new();

        public static bool IsPaused { get; set; } = false;
        public static void Register(MyGLThread thread)
        {
            _pendingThreads.Enqueue(thread);
            lock (_lock)
            {
                if (cts == null)
                    StartScheduler();
            }
        }

        public static void Unregister(MyGLThread thread)
        {
            _exitingThreads.Enqueue(thread);
        }

        private static void StartScheduler()
        {
            cts = new CancellationTokenSource();
            SW.Restart();
            Task.Run(Run, cts.Token);
        }

        public static void Reset() => SW.Restart();


        private static readonly Stopwatch SW = new();
        public static double Time { get; private set; } = 0.0;

        private static void Run()
        {
            var token = cts?.Token ?? throw new InvalidOperationException("Scheduler not started.");

            while (token.IsCancellationRequested == false)
            {
                Time = SW.Elapsed.TotalSeconds;

                while (_pendingThreads.TryDequeue(out var pending))
                    _threads.Add(pending);

                while (_exitingThreads.TryDequeue(out var exiting))
                    _threads.Remove(exiting);

                if (_threads.Count == 0)
                    break;

                if (!IsPaused)
                    foreach (var t in _threads)
                    {
                        if (t?.IsDisposed ?? true)
                            continue;

                        // Serial render dispatch
                        try
                        {
                            t.RenderNow.Set();
                            t.FrameDone.Wait(token);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"RenderScheduler: {ex.Message}");
                        }
                    }

                Thread.Sleep(1);
            }

            lock (_lock)
            {
                cts?.Dispose();
                cts = null;

                if (!_pendingThreads.IsEmpty)
                    StartScheduler();
            }
        }
    }
}

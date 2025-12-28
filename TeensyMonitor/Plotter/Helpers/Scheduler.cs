
namespace TeensyMonitor.Plotter.Helpers
{
    public static class Scheduler
    {
        private static readonly List<MyGLThread> _threads = new();
        private static readonly System.Windows.Forms.Timer _timer = new() { Interval = 16 };
        private static readonly object _lock = new();

        public static void Register(MyGLThread thread)
        {
            lock (_lock)
            {
                if (!_threads.Contains(thread))
                    _threads.Add(thread);

//                if (!_timer.Enabled)
//                    StartScheduler();
            }
        }

        public static void Unregister(MyGLThread thread)
        {
            lock (_lock)
            {
                _threads.Remove(thread);
                if (_threads.Count == 0)
                    StopScheduler();
            }
        }

        private static void StartScheduler()
        {
            _timer.Tick += (s, e) => Run();
            _timer.Start();
        }

        private static void StopScheduler()
        {
            _timer.Stop();
        }

        private static void Run()
        {
//            while (_running)
            {
                lock (_lock)
                {
                    foreach (var t in _threads)
                    {
                        if (t?.IsDisposed ?? true)
                            continue;

                        // Serial render dispatch
                        try
                        {
                            t.RenderOnce();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"RenderScheduler: {ex.Message}");
                        }
                    }
                }

                Thread.Sleep(1);
            }
        }
    }
}

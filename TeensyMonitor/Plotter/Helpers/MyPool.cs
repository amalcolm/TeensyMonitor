using System.Collections.Concurrent;

namespace TeensyMonitor.Plotter.Helpers
{
    /// <summary>
    /// A simple, thread-safe reusable object pool.
    /// Designed to reduce GC pressure for frequently allocated items
    /// like buffers, structs, or small temporary objects.
    /// </summary>
    /// <remarks>
    /// Create a new pool with an optional capacity limit and reset action.
    /// </remarks>
    /// <param name="maxCapacity">Maximum retained objects (0 = unlimited)</param>
    /// <param name="resetAction">Action to reset object before reuse (optional)</param>
    internal class MyPool<T>(int maxCapacity = 0, Action<T>? resetAction = null) where T : class, new()
    {
        private readonly ConcurrentStack<T> _availableItems = new();
        private readonly int _maxCapacity = maxCapacity;
        private readonly Action<T>? _resetAction = resetAction;

        /// <summary>
        /// Take an item from the pool, or create a new one if empty.
        /// </summary>
        public T Rent()
        {
            if (_availableItems.TryPop(out var item))
                return item;

            return new T();
        }

        /// <summary>
        /// Return an item to the pool for reuse.
        /// Optionally resets the item using the configured reset action.
        /// </summary>
        public void Return(T item)
        {
            if (item == null) return;

            _resetAction?.Invoke(item);

            // Avoid unbounded growth if capped
            if (_maxCapacity > 0 && _availableItems.Count >= _maxCapacity)
                return;

            _availableItems.Push(item);
        }

        /// <summary>
        /// Clears all cached objects from the pool.
        /// </summary>
        public void Clear()
        {
            while (_availableItems.TryPop(out _)) { }
        }

        /// <summary>
        /// The current number of pooled items.
        /// </summary>
        public int Count => _availableItems.Count;
    }
}
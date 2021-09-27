using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DotNetCore.CAP.Dashboard
{
    #region Cache<T> class

    /// <summary>
    /// This is a generic cache subsystem based on key/value pairs, where key is generic, too. Key must be unique.
    /// Every cache entry has its own timeout.
    /// Cache is thread safe and will delete expired entries on its own using System.Threading.Timers (which run on
    /// <see cref="ThreadPool" /> threads).
    /// </summary>
    // ReSharper disable once InheritdocConsiderUsage
    // ReSharper disable once InconsistentNaming
    public class Cache<K, T> : IDisposable
    {
        #region Constructor and class members

        private readonly Dictionary<K, T> _cache = new Dictionary<K, T>();
        private readonly Dictionary<K, Timer> _timers = new Dictionary<K, Timer>();
        private readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();

        #endregion

        #region IDisposable implementation & Clear

        private bool disposed;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;

                if (disposing)
                {
                    // Dispose managed resources.
                    Clear();
                    _locker.Dispose();
                }

                // Dispose unmanaged resources
            }
        }

        /// <summary>
        /// Clears the entire cache and disposes all active timers.
        /// </summary>
        public void Clear()
        {
            _locker.EnterWriteLock();
            try
            {
                try
                {
                    foreach (var t in _timers.Values)
                    {
                        t.Dispose();
                    }
                }
                catch
                {
                }

                _timers.Clear();
                _cache.Clear();
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }

        #endregion

        #region CheckTimer

        // Checks whether a specific timer already exists and adds a new one, if not 
        private void CheckTimer(K key, TimeSpan? cacheTimeout, bool restartTimerIfExists)
        {
            Timer timer;

            if (_timers.TryGetValue(key, out timer))
            {
                if (restartTimerIfExists)
                {
                    timer.Change(
                        cacheTimeout ?? Timeout.InfiniteTimeSpan,
                        Timeout.InfiniteTimeSpan);
                }
            }
            else
            {
                _timers.Add(
                    key,
                    new Timer(
                        RemoveByTimer,
                        key,
                        cacheTimeout ?? Timeout.InfiniteTimeSpan,
                        Timeout.InfiniteTimeSpan));
            }
        }

        private void RemoveByTimer(object state)
        {
            Remove((K)state);
        }

        #endregion

        #region AddOrUpdate, Get, Remove, Exists, Clear

        /// <summary>
        /// Adds or updates the specified cache-key with the specified cacheObject and applies a specified timeout (in seconds)
        /// to this key.
        /// </summary>
        /// <param name="key">The cache-key to add or update.</param>
        /// <param name="cacheObject">The cache object to store.</param>
        /// <param name="cacheTimeout">
        /// The cache timeout (lifespan) of this object. Must be 1 or greater.
        /// Specify Timeout.Infinite to keep the entry forever.
        /// </param>
        /// <param name="restartTimerIfExists">
        /// (Optional). If set to <c>true</c>, the timer for this cacheObject will be reset if the object already
        /// exists in the cache. (Default = false).
        /// </param>
        public void AddOrUpdate(K key, T cacheObject, TimeSpan? cacheTimeout, bool restartTimerIfExists = false)
        {
            if (disposed)
            {
                return;
            }

            _locker.EnterWriteLock();
            try
            {
                CheckTimer(key, cacheTimeout, restartTimerIfExists);

                if (!_cache.ContainsKey(key))
                {
                    _cache.Add(key, cacheObject);
                }
                else
                {
                    _cache[key] = cacheObject;
                }
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Adds or updates the specified cache-key with the specified cacheObject and applies <c>Timeout.Infinite</c> to this
        /// key.
        /// </summary>
        /// <param name="key">The cache-key to add or update.</param>
        /// <param name="cacheObject">The cache object to store.</param>
        public void AddOrUpdate(K key, T cacheObject)
        {
            AddOrUpdate(key, cacheObject, Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Gets the cache entry with the specified key or returns <c>default(T)</c> if the key is not found.
        /// </summary>
        /// <param name="key">The cache-key to retrieve.</param>
        /// <returns>The object from the cache or <c>default(T)</c>, if not found.</returns>
        public T this[K key] => Get(key);

        /// <summary>
        /// Gets the cache entry with the specified key or return <c>default(T)</c> if the key is not found.
        /// </summary>
        /// <param name="key">The cache-key to retrieve.</param>
        /// <returns>The object from the cache or <c>default(T)</c>, if not found.</returns>
        public T Get(K key)
        {
            if (disposed)
            {
                return default(T);
            }

            _locker.EnterReadLock();
            try
            {
                T rv;
                return _cache.TryGetValue(key, out rv) ? rv : default(T);
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }

        /// <summary>
        /// Tries to gets the cache entry with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">(out) The value, if found, or <c>default(T)</c>, if not.</param>
        /// <returns><c>True</c>, if <c>key</c> exists, otherwise <c>false</c>.</returns>
        public bool TryGet(K key, out T value)
        {
            if (disposed)
            {
                value = default(T);
                return false;
            }

            _locker.EnterReadLock();
            try
            {
                return _cache.TryGetValue(key, out value);
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }

        /// <summary>
        /// Removes a series of cache entries in a single call for all key that match the specified key pattern.
        /// </summary>
        /// <param name="keyPattern">The key pattern to remove. The Predicate has to return true to get key removed.</param>
        public void Remove(Predicate<K> keyPattern)
        {
            if (disposed)
            {
                return;
            }

            _locker.EnterWriteLock();
            try
            {
                var removers = (from k in _cache.Keys.Cast<K>()
                                where keyPattern(k)
                                select k).ToList();

                foreach (var workKey in removers)
                {
                    try
                    {
                        _timers[workKey].Dispose();
                    }
                    catch
                    {
                    }

                    _timers.Remove(workKey);
                    _cache.Remove(workKey);
                }
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes the specified cache entry with the specified key.
        /// If the key is not found, no exception is thrown, the statement is just ignored.
        /// </summary>
        /// <param name="key">The cache-key to remove.</param>
        public void Remove(K key)
        {
            if (disposed)
            {
                return;
            }

            _locker.EnterWriteLock();
            try
            {
                if (_cache.ContainsKey(key))
                {
                    try
                    {
                        _timers[key].Dispose();
                    }
                    catch
                    {
                    }

                    _timers.Remove(key);
                    _cache.Remove(key);
                }
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Checks if a specified key exists in the cache.
        /// </summary>
        /// <param name="key">The cache-key to check.</param>
        /// <returns><c>True</c> if the key exists in the cache, otherwise <c>False</c>.</returns>
        public bool Exists(K key)
        {
            if (disposed)
            {
                return false;
            }

            _locker.EnterReadLock();
            try
            {
                return _cache.ContainsKey(key);
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }

        #endregion
    }

    #endregion

    #region Other Cache classes (derived)

    /// <summary>
    /// This is a generic cache subsystem based on key/value pairs, where key is a string.
    /// You can add any item to this cache as long as the key is unique, so treat keys as something like namespaces and
    /// build them with a
    /// specific system/syntax in your application.
    /// Every cache entry has its own timeout.
    /// Cache is thread safe and will delete expired entries on its own using System.Threading.Timers (which run on
    /// <see cref="ThreadPool" /> threads).
    /// </summary>
    /// <summary>
    /// The non-generic Cache class instanciates a Cache{object} that can be used with any type of (mixed) contents.
    /// It also publishes a static <c>.Global</c> member, so a cache can be used even without creating a dedicated
    /// instance.
    /// The <c>.Global</c> member is lazy instanciated.
    /// </summary>
    public class CapCache : Cache<string, object>
    {
        #region Static Global Cache instance 

        private static readonly Lazy<CapCache> global = new Lazy<CapCache>();

        /// <summary>
        /// Gets the global shared cache instance valid for the entire process.
        /// </summary>
        /// <value>
        /// The global shared cache instance.
        /// </value>
        public static CapCache Global => global.Value;

        #endregion
    }

    #endregion
}

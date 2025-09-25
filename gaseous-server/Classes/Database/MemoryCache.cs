using System;
using System.Collections.Concurrent;
using System.Threading;

namespace gaseous_server.Classes
{
    /// <summary>
    /// Provides an in-memory, thread-safe cache with simple expiration for objects.
    /// </summary>
    public class MemoryCache
    {
        // Stats counters (Int64 for atomic increments)
        private long _hits = 0;
        private long _misses = 0;
        private long _evictions = 0; // total evictions (expired + size)
        private long _expirationEvictions = 0; // evictions due to expiration
        private long _sizeEvictions = 0; // evictions due to size limit
        private DateTime _lastResetUtc = DateTime.UtcNow;

        // LRU list management (optional when _maxSize > 0)
        private readonly object _lruLock = new();
        private MemoryCacheItem? _lruHead = null; // most recently used
        private MemoryCacheItem? _lruTail = null; // least recently used
        private readonly int _maxSize = 0; // 0 = unlimited
        private long _lastStatsLogTick = Environment.TickCount64;

        /// <summary>
        /// Represents a cached item with its associated object and expiration details.
        /// </summary>
        private sealed class MemoryCacheItem
        {
            /// <summary>
            /// Initializes a new instance of the MemoryCacheItem class with the specified object to cache.
            /// </summary>
            /// <param name="CacheObject">
            /// The object instance to be cached.
            /// </param>
            public MemoryCacheItem(string key, object CacheObject)
            {
                Key = key;
                cacheObject = CacheObject;
            }

            /// <summary>
            /// Initializes a new instance of the MemoryCacheItem class with the specified object to cache and expiration time.
            /// </summary>
            /// <param name="CacheObject">
            /// The object instance to be cached.
            /// </param>
            /// <param name="ExpirationSeconds">
            /// The number of seconds before the cached object expires.
            /// </param>
            public MemoryCacheItem(string key, object CacheObject, int ExpirationSeconds)
            {
                Key = key;
                cacheObject = CacheObject;
                expirationSeconds = ExpirationSeconds;
            }

            /// <summary>
            /// The time the object was added to the cache in ticks
            /// </summary>
            public long addedTime { get; } = Environment.TickCount64;

            /// <summary>
            /// The time the object will expire in ticks
            /// </summary>
            public long expirationTime
            {
                get
                {
                    return addedTime + _expirationTicks;
                }
            }

            /// <summary>
            /// The number of seconds the object will be cached
            /// </summary>
            public int expirationSeconds
            {
                get
                {
                    return (int)TimeSpan.FromTicks(_expirationTicks).TotalSeconds;
                }
                set
                {
                    _expirationTicks = TimeSpan.FromSeconds(value).Ticks;
                }
            }
            private long _expirationTicks = TimeSpan.FromSeconds(2).Ticks;

            /// <summary>
            /// The object to be cached
            /// </summary>
            public object cacheObject { get; set; }

            // LRU tracking
            public MemoryCacheItem? Prev { get; set; }
            public MemoryCacheItem? Next { get; set; }
            public long lastAccessed { get; set; } = Environment.TickCount64;
            public string Key { get; }
        }
        // Use a thread-safe concurrent dictionary while keeping existing calling code intact.
        // NOTE: Add `using System.Collections.Concurrent;` at the top of the file.
        private sealed class ConcurrentMemoryCache : ConcurrentDictionary<string, MemoryCacheItem>
        {
            // Provide Add/Remove to mimic Dictionary API already used elsewhere.
            public void Add(string key, MemoryCacheItem value)
            {
                // Mimic Dictionary.Add (throw if key exists) so existing logic (which removes first) still behaves.
                if (!TryAdd(key, value))
                {
                    throw new ArgumentException("An item with the same key has already been added. Key: " + key);
                }
            }

            public void Remove(string key)
            {
                TryRemove(key, out _);
            }

            public static implicit operator Dictionary<string, MemoryCacheItem>(ConcurrentMemoryCache source)
                => new Dictionary<string, MemoryCacheItem>(source);
        }

        // Static initializer ensures the timer starts as soon as the type is first referenced.
        private readonly ConcurrentMemoryCache memoryCache;
        private readonly Timer cacheTimer;

        /// <summary>
        /// Initializes a new MemoryCache with unlimited size (time-based expiration only).
        /// </summary>
        public MemoryCache() : this(0) { }

        /// <summary>
        /// Initializes a new MemoryCache with an optional maximum size. When maxSize &gt; 0 an LRU policy is applied.
        /// </summary>
        /// <param name="maxSize">Maximum number of items allowed (0 = unlimited).</param>
        public MemoryCache(int maxSize)
        {
            _maxSize = maxSize < 0 ? 0 : maxSize;
            memoryCache = new ConcurrentMemoryCache();
            cacheTimer = new Timer(CacheTimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        private void CacheTimerCallback(object? state)
        {
            ClearExpiredCache();
            // Log stats every 5 minutes using the same timer thread (1s interval)
            try
            {
                long now = Environment.TickCount64;
                if (now - _lastStatsLogTick >= TimeSpan.FromMinutes(5).TotalMilliseconds)
                {
                    _lastStatsLogTick = now;
                    var stats = GetStats();
                    Logging.Log(Logging.LogType.Information, "Cache", $"Stats: Items={stats.ItemCount}/{(stats.MaxSize == 0 ? (object)"inf" : stats.MaxSize)} Hits={stats.Hits} Misses={stats.Misses} HitRate={stats.HitRate:P2} Evictions={stats.Evictions} (Exp={stats.ExpirationEvictions}, Size={stats.SizeEvictions}) Requests={stats.Requests}");
                }
            }
            catch (Exception ex)
            {
                Logging.Log(Logging.LogType.Debug, "Cache", "Error while logging cache statistics", ex);
            }
        }

        /// <summary>
        /// Retrieves a cached object by key if it exists and has not expired; otherwise returns null (also cleans up expired entry).
        /// </summary>
        /// <param name="CacheKey">The unique cache key.</param>
        /// <returns>The cached object instance, or null if missing or expired.</returns>
        public object? GetCacheObject(string CacheKey)
        {
            try
            {
                if (memoryCache.TryGetValue(CacheKey, out var cacheItem))
                {
                    if (cacheItem.expirationTime < Environment.TickCount64)
                    {
                        // Expired – treat as miss & eviction
                        memoryCache.Remove(CacheKey);
                        Interlocked.Increment(ref _misses);
                        Interlocked.Increment(ref _evictions);
                        Interlocked.Increment(ref _expirationEvictions);
                        // Remove from LRU list if size tracking enabled
                        if (_maxSize > 0)
                        {
                            lock (_lruLock)
                            {
                                RemoveNode(cacheItem);
                            }
                        }
                        return null;
                    }
                    // Hit
                    Interlocked.Increment(ref _hits);
                    if (_maxSize > 0)
                    {
                        lock (_lruLock)
                        {
                            cacheItem.lastAccessed = Environment.TickCount64;
                            MoveToHead(cacheItem);
                        }
                    }
                    return cacheItem.cacheObject;
                }
                // Miss (no key)
                Interlocked.Increment(ref _misses);
                return null;
            }
            catch
            {
                // On error consider it a miss
                Interlocked.Increment(ref _misses);
                return null;
            }
        }
        /// <summary>
        /// Adds or replaces an object in the in-memory cache with an optional expiration time in seconds.
        /// </summary>
        /// <param name="CacheKey">The unique key used to identify the cached object.</param>
        /// <param name="CacheObject">The object instance to cache.</param>
        /// <param name="ExpirationSeconds">How many seconds the object should remain cached (default is 2 seconds).</param>
        /// <remarks>
        /// If an existing item with the same key is present it is removed before adding the new one; on failure the cache is cleared and the error is logged.
        /// </remarks>
        public void SetCacheObject(string CacheKey, object CacheObject, int ExpirationSeconds = 2)
        {
            try
            {
                MemoryCacheItem? existing = null;
                if (memoryCache.TryGetValue(CacheKey, out existing))
                {
                    // Replace existing
                    memoryCache.Remove(CacheKey);
                    if (_maxSize > 0 && existing != null)
                    {
                        lock (_lruLock)
                        {
                            RemoveNode(existing);
                        }
                    }
                }

                var newItem = new MemoryCacheItem(CacheKey, CacheObject, ExpirationSeconds);
                memoryCache.Add(CacheKey, newItem);
                if (_maxSize > 0)
                {
                    lock (_lruLock)
                    {
                        AddToHead(newItem);
                        EnforceSizeLimit();
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Log(Logging.LogType.Debug, "Cache", "Error while setting cache object", ex);
                ClearCache();
            }
        }
        /// <summary>
        /// Removes a cached object by key if it exists.
        /// </summary>
        /// <param name="CacheKey">
        /// The unique cache key.
        /// </param>
        public void RemoveCacheObject(string CacheKey)
        {
            if (memoryCache.TryGetValue(CacheKey, out var existing))
            {
                memoryCache.Remove(CacheKey);
                if (_maxSize > 0 && existing != null)
                {
                    lock (_lruLock)
                    {
                        RemoveNode(existing);
                    }
                }
            }
        }
        /// <summary>
        /// Removes multiple cached objects by their keys if they exist.
        /// </summary>
        /// <param name="CacheKeys">
        /// A list of unique cache keys.
        /// </param>
        public void RemoveCacheObject(List<string> CacheKeys)
        {
            foreach (string key in CacheKeys)
            {
                if (memoryCache.TryGetValue(key, out var existing))
                {
                    memoryCache.Remove(key);
                    if (_maxSize > 0 && existing != null)
                    {
                        lock (_lruLock)
                        {
                            RemoveNode(existing);
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Clears all items from the in-memory cache.
        /// </summary>
        public void ClearCache()
        {
            memoryCache.Clear();
            if (_maxSize > 0)
            {
                lock (_lruLock)
                {
                    _lruHead = null;
                    _lruTail = null;
                }
            }
        }
        private void ClearExpiredCache()
        {
            try
            {
                long currTime = Environment.TickCount64;

                List<string> toRemove = new();
                foreach (var kvp in memoryCache)
                {
                    var item = kvp.Value;
                    if (item.expirationTime < currTime)
                    {
                        toRemove.Add(kvp.Key);
                    }
                }
                foreach (var key in toRemove)
                {
                    if (memoryCache.TryGetValue(key, out var item))
                    {
                        Console.WriteLine("\x1b[95mPurging expired cache item " + key + ". Added: " + item.addedTime + ". Expired: " + item.expirationTime);
                        memoryCache.Remove(key);
                        Interlocked.Increment(ref _evictions);
                        Interlocked.Increment(ref _expirationEvictions);
                        if (_maxSize > 0)
                        {
                            lock (_lruLock)
                            {
                                RemoveNode(item);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Log(Logging.LogType.Debug, "Cache", "Error while clearing expired cache", ex);
            }
        }

        /// <summary>
        /// Returns current cache statistics (snapshot values).
        /// </summary>
        public MemoryCacheStats GetStats()
        {
            long hits = Interlocked.Read(ref _hits);
            long misses = Interlocked.Read(ref _misses);
            long evictions = Interlocked.Read(ref _evictions);
            long expirationEvictions = Interlocked.Read(ref _expirationEvictions);
            long sizeEvictions = Interlocked.Read(ref _sizeEvictions);
            return new MemoryCacheStats
            {
                Hits = hits,
                Misses = misses,
                Evictions = evictions,
                ExpirationEvictions = expirationEvictions,
                SizeEvictions = sizeEvictions,
                ItemCount = memoryCache.Count,
                MaxSize = _maxSize,
                LastResetUtc = _lastResetUtc
            };
        }

        /// <summary>
        /// Resets cache statistics counters to zero.
        /// </summary>
        public void ResetStats()
        {
            Interlocked.Exchange(ref _hits, 0);
            Interlocked.Exchange(ref _misses, 0);
            Interlocked.Exchange(ref _evictions, 0);
            Interlocked.Exchange(ref _expirationEvictions, 0);
            Interlocked.Exchange(ref _sizeEvictions, 0);
            _lastResetUtc = DateTime.UtcNow;
        }

        // LRU helper methods (guard calls with _lruLock)
        private void AddToHead(MemoryCacheItem item)
        {
            item.Prev = null;
            item.Next = _lruHead;
            if (_lruHead != null) _lruHead.Prev = item;
            _lruHead = item;
            if (_lruTail == null) _lruTail = item; // first item
        }

        private void MoveToHead(MemoryCacheItem item)
        {
            if (_lruHead == item) return;
            RemoveNode(item);
            AddToHead(item);
        }

        private void RemoveNode(MemoryCacheItem item)
        {
            var prev = item.Prev;
            var next = item.Next;
            if (prev != null) prev.Next = next; else if (_lruHead == item) _lruHead = next;
            if (next != null) next.Prev = prev; else if (_lruTail == item) _lruTail = prev;
            item.Prev = null;
            item.Next = null;
        }

        private void EnforceSizeLimit()
        {
            if (_maxSize <= 0) return;
            while (memoryCache.Count > _maxSize && _lruTail != null)
            {
                var toEvict = _lruTail;
                RemoveNode(toEvict);
                memoryCache.Remove(toEvict.Key);
                Interlocked.Increment(ref _evictions);
                Interlocked.Increment(ref _sizeEvictions);
            }
        }
    }

    /// <summary>
    /// Snapshot of memory cache statistics.
    /// </summary>
    public class MemoryCacheStats
    {
        /// <summary>Total number of successful cache lookups.</summary>
        public long Hits { get; init; }
        /// <summary>Total number of failed cache lookups (missing or expired).</summary>
        public long Misses { get; init; }
        /// <summary>Total number of items removed because they expired.</summary>
        public long Evictions { get; init; }
        /// <summary>Number of evictions caused by expiration.</summary>
        public long ExpirationEvictions { get; init; }
        /// <summary>Number of evictions caused by size limit (LRU policy).</summary>
        public long SizeEvictions { get; init; }
        /// <summary>Current number of items stored in the cache.</summary>
        public int ItemCount { get; init; }
        /// <summary>Configured maximum size (0 = unlimited).</summary>
        public int MaxSize { get; init; }
        /// <summary>UTC timestamp when statistics were last reset.</summary>
        public DateTime LastResetUtc { get; init; }

        /// <summary>Total number of Get attempts (Hits + Misses).</summary>
        public long Requests => Hits + Misses;
        /// <summary>Fraction of requests that were hits (0 if no requests).</summary>
        public double HitRate => Requests == 0 ? 0 : (double)Hits / Requests;
        /// <summary>Fraction of requests that were misses (0 if no requests).</summary>
        public double MissRate => Requests == 0 ? 0 : (double)Misses / Requests;
    }

    /// <summary>
    /// Options for configuring the behavior of the DatabaseMemoryCache.
    /// </summary>
    public class DatabaseMemoryCacheOptions
    {
        /// <summary>
        /// Initializes a new instance of the DatabaseMemoryCacheOptions class with specified settings.
        /// </summary>
        /// <param name="CacheEnabled">
        /// Whether caching is enabled.
        /// </param>
        /// <param name="ExpirationSeconds">
        /// The number of seconds before a cached item expires.
        /// </param>
        /// <param name="CacheKey">
        /// The unique key used to identify the cached object.
        /// </param>
        public DatabaseMemoryCacheOptions(bool CacheEnabled = false, int ExpirationSeconds = 1, string? CacheKey = null)
        {
            this.CacheEnabled = CacheEnabled;
            this.ExpirationSeconds = ExpirationSeconds;
            this.CacheKey = CacheKey;
        }

        /// <summary>
        /// Gets or sets a value indicating whether caching is enabled.
        /// </summary>
        public bool CacheEnabled { get; set; }

        /// <summary>
        /// Gets or sets the number of seconds before a cached item expires.
        /// </summary>
        public int ExpirationSeconds { get; set; }

        /// <summary>
        /// Gets or sets the unique key used to identify the cached object.
        /// </summary>
        public string? CacheKey { get; set; } = null;
    }
}
using System.Collections.Concurrent;

namespace gaseous_server.Classes.Plugins.MetadataProviders
{
    /// <summary>
    /// Serializes forced refreshes per provider/entity and suppresses repeat forced refreshes within a cool-down window.
    /// Non-forced callers are not locked.
    /// </summary>
    public sealed class ForceRefreshGuard : IDisposable
    {
        private static readonly TimeSpan CoolDown = TimeSpan.FromMinutes(5);
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();
        private static readonly ConcurrentDictionary<string, DateTime> LastRefreshed = new();

        private readonly string key;
        private SemaphoreSlim? held;

        private ForceRefreshGuard(string key, SemaphoreSlim? held, bool forceRefresh)
        {
            this.key = key;
            this.held = held;
            ForceRefresh = forceRefresh;
        }

        /// <summary>
        /// The effective force-refresh flag the caller should use. False if not requested or a refresh completed within the cool-down window.
        /// </summary>
        public bool ForceRefresh { get; }

        /// <summary>
        /// Enters the guard. Only takes the lock when <paramref name="forceRefresh"/> is true.
        /// </summary>
        /// <param name="scope">Provider identifier, e.g. the provider type name.</param>
        /// <param name="id">Entity identifier.</param>
        /// <param name="forceRefresh">Whether a forced refresh was requested.</param>
        public static async Task<ForceRefreshGuard> EnterAsync(string scope, long id, bool forceRefresh)
        {
            string key = $"{scope}:{id}";
            if (!forceRefresh)
            {
                return new ForceRefreshGuard(key, null, false);
            }

            var semaphore = Locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
            await semaphore.WaitAsync();

            // another caller may have refreshed while we waited
            bool stillRequired = !(LastRefreshed.TryGetValue(key, out var last) && DateTime.UtcNow - last < CoolDown);
            return new ForceRefreshGuard(key, semaphore, stillRequired);
        }

        /// <summary>
        /// Records a successful forced refresh so subsequent forced requests are downgraded for the cool-down window.
        /// </summary>
        public void MarkCompleted()
        {
            if (held != null)
            {
                LastRefreshed[key] = DateTime.UtcNow;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            held?.Release();
            held = null;
        }
    }
}

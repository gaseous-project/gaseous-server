using System;
using System.Threading;

namespace gaseous_server.Classes.Plugins.MetadataProviders
{
    /// <summary>
    /// Ambient flag that allows expired metadata to be refreshed from remote sources.
    /// Expired content is otherwise always served from local storage. Only the metadata
    /// refresh methods (background process) may begin a scope - request paths must never do so.
    /// </summary>
    public static class MetadataRefreshScope
    {
        private static readonly AsyncLocal<bool> _refreshExpired = new AsyncLocal<bool>();
        private static readonly AsyncLocal<bool> _forceRefresh = new AsyncLocal<bool>();

        /// <summary>
        /// True when the current async flow is permitted to refresh expired content from remote sources.
        /// </summary>
        public static bool RefreshExpired => _refreshExpired.Value;

        public static bool ForceRefresh => _forceRefresh.Value;

        /// <summary>
        /// Enables refreshing of expired content for the current async flow until the returned scope is disposed.
        /// </summary>
        public static IDisposable BeginRefreshExpired(bool forceRefresh = false)
        {
            bool previous = _refreshExpired.Value;
            bool previousForceRefresh = _forceRefresh.Value;
            _refreshExpired.Value = true;
            _forceRefresh.Value = previousForceRefresh || forceRefresh;
            return new Scope(previous, previousForceRefresh);
        }

        private sealed class Scope : IDisposable
        {
            private readonly bool _previous;
            private readonly bool _previousForceRefresh;
            private bool _disposed;

            public Scope(bool previous, bool previousForceRefresh)
            {
                _previous = previous;
                _previousForceRefresh = previousForceRefresh;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
                _refreshExpired.Value = _previous;
                _forceRefresh.Value = _previousForceRefresh;
            }
        }
    }
}

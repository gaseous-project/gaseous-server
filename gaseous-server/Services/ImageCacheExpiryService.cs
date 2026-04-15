using gaseous_server.Classes.Metadata;

namespace gaseous_server.Services
{
    /// <summary>
    /// Background service that periodically expires old and infrequently accessed entries from the image cache.
    /// Runs every 5 minutes to keep the cache within memory (50K entries) and disk (2GB) limits.
    /// Ensures frequently accessed image path lookups stay in memory while old entries are offloaded to disk or deleted entirely.
    /// </summary>
    public class ImageCacheExpiryService : BackgroundService
    {
        private readonly ILogger<ImageCacheExpiryService> _logger;
        private const int ExpiryIntervalMinutes = 5;

        public ImageCacheExpiryService(ILogger<ImageCacheExpiryService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ImageCacheExpiryService starting. Image cache will be expired every {Minutes} minutes.", ExpiryIntervalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(ExpiryIntervalMinutes), stoppingToken);

                    _logger.LogDebug("Running image cache expiration task.");
                    await ImageHandling.ExpireImageCache();
                    _logger.LogDebug("Image cache expiration task completed.");
                }
                catch (OperationCanceledException)
                {
                    // Expected when the service is stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during image cache expiration.");
                }
            }

            _logger.LogInformation("ImageCacheExpiryService stopped.");
        }
    }
}

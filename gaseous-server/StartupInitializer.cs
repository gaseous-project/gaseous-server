using System.Reflection;
using System.IO;
using gaseous_server.Classes;
using gaseous_server.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Authentication;
using gaseous_server.Classes.Metadata;
using gaseous_server;

namespace gaseous_server
{
    public class StartupInitializer : BackgroundService
    {
        private readonly IServiceProvider _services;

        public StartupInitializer(IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                Logging.WriteToDiskOnly = true;
                Logging.LogKey(Logging.LogType.Information, "process.startup", "startup.starting_server", null, new string[] { Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "" });

                // Wait for DB online
                var db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionStringNoDatabase);
                while (!stoppingToken.IsCancellationRequested)
                {
                    Logging.LogKey(Logging.LogType.Information, "process.startup", "startup.waiting_for_database");
                    if (db.TestConnection()) break;
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }

                db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

                // DB init and static data
                db.InitDB();
                await Storage.CreateRelationsTables<IGDB.Models.Game>();
                await Storage.CreateRelationsTables<IGDB.Models.Platform>();
                await AgeRatings.PopulateAgeMapAsync();

                // Settings
                Config.InitSettings();
                Config.UpdateConfig();
                await GameLibrary.UpdateDefaultLibraryPathAsync();

                // API metadata source
                Communications.MetadataSource = Config.MetadataConfiguration.DefaultMetadataSource;
                HasheousClient.WebApp.HttpHelper.BaseUri = Config.MetadataConfiguration.HasheousHost;

                // Storage cleanup
                if (Directory.Exists(Config.LibraryConfiguration.LibraryTempDirectory))
                    Directory.Delete(Config.LibraryConfiguration.LibraryTempDirectory, true);
                if (Directory.Exists(Config.LibraryConfiguration.LibraryUploadDirectory))
                    Directory.Delete(Config.LibraryConfiguration.LibraryUploadDirectory, true);

                // Delayed upgrade tasks
                var queueItem = new ProcessQueue.QueueProcessor.QueueItem(
                    ProcessQueue.QueueItemType.BackgroundDatabaseUpgrade,
                    1,
                    false,
                    true);
                queueItem.ForceExecute();
                ProcessQueue.QueueProcessor.QueueItems.Add(queueItem);

                // Roles and system setup
                using (var scope = _services.CreateScope())
                {
                    var roleManager = scope.ServiceProvider.GetRequiredService<RoleStore>();
                    var roles = new[] { "Admin", "Gamer", "Player" };
                    foreach (var role in roles)
                    {
                        if (await roleManager.FindByNameAsync(role, CancellationToken.None) == null)
                        {
                            ApplicationRole applicationRole = new ApplicationRole();
                            applicationRole.Name = role;
                            applicationRole.NormalizedName = role.ToUpper();
                            await roleManager.CreateAsync(applicationRole, CancellationToken.None);
                        }
                    }
                }

                // Library and platform setup
                Config.LibraryConfiguration.InitLibrary();
                foreach (FileSignature.MetadataSources source in Enum.GetValues(typeof(FileSignature.MetadataSources)))
                {
                    await Platforms.GetPlatform(0, source);
                }
                await PlatformMapping.ExtractPlatformMap();
                Bios.MigrateToNewFolderStructure();

                // Background tasks
                ProcessQueue.QueueProcessor.QueueItems.Add(new ProcessQueue.QueueProcessor.QueueItem(ProcessQueue.QueueItemType.SignatureIngestor));
                ProcessQueue.QueueProcessor.QueueItems.Add(new ProcessQueue.QueueProcessor.QueueItem(ProcessQueue.QueueItemType.TitleIngestor));
                ProcessQueue.QueueProcessor.QueueItems.Add(new ProcessQueue.QueueProcessor.QueueItem(ProcessQueue.QueueItemType.ImportQueueProcessor, 1, false));
                ProcessQueue.QueueProcessor.QueueItems.Add(new ProcessQueue.QueueProcessor.QueueItem(ProcessQueue.QueueItemType.MetadataRefresh));
                ProcessQueue.QueueProcessor.QueueItems.Add(new ProcessQueue.QueueProcessor.QueueItem(ProcessQueue.QueueItemType.OrganiseLibrary));
                ProcessQueue.QueueProcessor.QueueItems.Add(new ProcessQueue.QueueProcessor.QueueItem(ProcessQueue.QueueItemType.LibraryScan));

                // Maintenance tasks
                ProcessQueue.QueueProcessor.QueueItems.Add(new ProcessQueue.QueueProcessor.QueueItem(ProcessQueue.QueueItemType.DailyMaintainer));
                ProcessQueue.QueueProcessor.QueueItems.Add(new ProcessQueue.QueueProcessor.QueueItem(ProcessQueue.QueueItemType.WeeklyMaintainer));
                ProcessQueue.QueueProcessor.QueueItems.Add(new ProcessQueue.QueueProcessor.QueueItem(ProcessQueue.QueueItemType.TempCleanup));

                Logging.WriteToDiskOnly = false;
                Logging.LogKey(Logging.LogType.Information, "process.startup", "startup.initialization_complete");
            }
            catch (OperationCanceledException)
            {
                // Shutting down
            }
            catch (Exception ex)
            {
                Logging.LogKey(Logging.LogType.Critical, "process.startup", "startup.initialization_failed", null, null, ex);
            }
        }
    }
}
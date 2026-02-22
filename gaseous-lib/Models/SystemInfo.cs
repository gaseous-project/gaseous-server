using System.Reflection;
using gaseous_server.Classes;

namespace gaseous_server.Models
{
    public class SystemInfoModel
    {
        public Version ApplicationVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version;
            }
        }
        public List<PathItem>? Paths { get; set; }
        public long DatabaseSize { get; set; }
        public List<PlatformStatisticsItem>? PlatformStatistics { get; set; }

        public class PathItem
        {
            public string Name { get; set; }
            public string LibraryPath { get; set; }
            public long SpaceUsed { get; set; }
            public long SpaceAvailable { get; set; }
            public long TotalSpace { get; set; }
        }

        public class PlatformStatisticsItem
        {
            public string Platform { get; set; }
            public long RomCount { get; set; }
            public long TotalSize { get; set; }
        }
    }

    public class SystemSettingsModel
    {
        public bool AlwaysLogToDisk { get; set; }
        public int MinimumLogRetentionPeriod { get; set; }
        public bool EmulatorDebugMode { get; set; }
        public SignatureSourceItem SignatureSource { get; set; }
        public List<MetadataSourceItem> MetadataSources { get; set; }

        public class SignatureSourceItem
        {
            public HasheousClient.Models.MetadataModel.SignatureSources Source { get; set; }
            public string HasheousHost { get; set; }
            public string HasheousAPIKey { get; set; }
            public bool HasheousSubmitFixes { get; set; }
        }

        public class MetadataSourceItem
        {
            public MetadataSourceItem()
            {

            }

            public MetadataSourceItem(FileSignature.MetadataSources source, bool useHasheousProxy, string clientId, string secret, FileSignature.MetadataSources defaultSource)
            {
                Source = source;
                UseHasheousProxy = useHasheousProxy;
                ClientId = clientId;
                Secret = secret;
                if (Source == defaultSource)
                {
                    Default = true;
                }
                else
                {
                    Default = false;
                }
            }

            public FileSignature.MetadataSources Source { get; set; }
            public bool UseHasheousProxy { get; set; }
            public string ClientId { get; set; }
            public string Secret { get; set; }
            public bool Default { get; set; }
            public bool? Configured
            {
                get
                {
                    switch (Source)
                    {
                        case FileSignature.MetadataSources.None:
                            return true;
                        case FileSignature.MetadataSources.IGDB:
                            if ((!String.IsNullOrEmpty(ClientId) && !String.IsNullOrEmpty(Secret)) || UseHasheousProxy == true)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        case FileSignature.MetadataSources.TheGamesDb:
                            if ((!String.IsNullOrEmpty(ClientId) && !String.IsNullOrEmpty(Secret)) || UseHasheousProxy == true)
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        default:
                            return false;
                    }
                }
            }
            public bool? UsesProxy
            {
                get
                {
                    switch (Source)
                    {
                        case FileSignature.MetadataSources.None:
                            return false;
                        case FileSignature.MetadataSources.IGDB:
                            return true;
                        case FileSignature.MetadataSources.TheGamesDb:
                            return true;
                        default:
                            return false;
                    }
                }
            }
            public bool? UsesClientIdAndSecret
            {
                get
                {
                    switch (Source)
                    {
                        case FileSignature.MetadataSources.None:
                            return false;
                        case FileSignature.MetadataSources.IGDB:
                            return true;
                        case FileSignature.MetadataSources.TheGamesDb:
                            return false;
                        default:
                            return false;
                    }
                }
            }
        }
    }
}
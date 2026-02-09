using Newtonsoft.Json;

namespace gaseous_server.Classes.Configuration.Models
{
    public class Library
    {
        public string LibraryRootDirectory
        {
            get
            {
                return Path.Combine(Config.ConfigurationPath, "Data");
            }
        }

        public string LibraryImportDirectory
        {
            get
            {
                return Path.Combine(LibraryRootDirectory, "Import");
            }
        }

        public string LibraryImportErrorDirectory
        {
            get
            {
                return Path.Combine(LibraryRootDirectory, "Import Errors");
            }
        }

        public string LibraryImportDuplicatesDirectory
        {
            get
            {
                return Path.Combine(LibraryImportErrorDirectory, "Duplicates");
            }
        }

        public string LibraryImportGeneralErrorDirectory
        {
            get
            {
                return Path.Combine(LibraryImportErrorDirectory, "Error");
            }
        }

        public string LibraryBIOSDirectory
        {
            get
            {
                return Path.Combine(LibraryRootDirectory, "BIOS");
            }
        }

        public string LibraryFirmwareDirectory
        {
            get
            {
                return Path.Combine(LibraryRootDirectory, "Firmware");
            }
        }

        public string LibraryUploadDirectory
        {
            get
            {
                return Path.Combine(LibraryRootDirectory, "Upload");
            }
        }

        public string LibraryMetadataDirectory
        {
            get
            {
                return Path.Combine(LibraryRootDirectory, "Metadata");
            }
        }

        public string LibraryContentDirectory
        {
            get
            {
                return Path.Combine(LibraryRootDirectory, "Content");
            }
        }

        public string LibraryTempDirectory
        {
            get
            {
                return Path.Combine(LibraryRootDirectory, "Temp");
            }
        }

        public string LibraryCollectionsDirectory
        {
            get
            {
                return Path.Combine(LibraryRootDirectory, "Collections");
            }
        }

        public string LibraryMediaGroupDirectory
        {
            get
            {
                return Path.Combine(LibraryRootDirectory, "Media Groups");
            }
        }

        public string LibraryMetadataDirectory_Platform(HasheousClient.Models.Metadata.IGDB.Platform platform)
        {
            string MetadataPath = Path.Combine(LibraryMetadataDirectory, "Platforms", platform.Slug);
            if (!Directory.Exists(MetadataPath)) { Directory.CreateDirectory(MetadataPath); }
            return MetadataPath;
        }

        public string LibraryMetadataDirectory_Game(gaseous_server.Models.Game game)
        {
            string MetadataPath = Path.Combine(LibraryMetadataDirectory, "Games", game.Slug);
            if (!Directory.Exists(MetadataPath)) { Directory.CreateDirectory(MetadataPath); }
            return MetadataPath;
        }

        public string LibraryMetadataDirectory_Company(HasheousClient.Models.Metadata.IGDB.Company company)
        {
            string MetadataPath = Path.Combine(LibraryMetadataDirectory, "Companies", company.Slug);
            if (!Directory.Exists(MetadataPath)) { Directory.CreateDirectory(MetadataPath); }
            return MetadataPath;
        }

        public string LibraryMetadataDirectory_Hasheous()
        {
            string MetadataPath = Path.Combine(LibraryMetadataDirectory, "Hasheous");
            if (!Directory.Exists(MetadataPath)) { Directory.CreateDirectory(MetadataPath); }
            return MetadataPath;
        }

        public string LibraryMetadataDirectory_TheGamesDB()
        {
            string MetadataPath = Path.Combine(LibraryMetadataDirectory, "TheGamesDB");
            if (!Directory.Exists(MetadataPath)) { Directory.CreateDirectory(MetadataPath); }
            return MetadataPath;
        }

        /// <summary>
        /// Gets the metadata directory path for game bundles based on the metadata source and game ID.
        /// </summary>
        /// <param name="SourceType">The metadata source type.</param>
        /// <param name="GameId">The game ID.</param>
        /// <returns>The full path to the game bundles metadata directory.</returns>
        public string LibraryMetadataDirectory_GameBundles(FileSignature.MetadataSources SourceType, long GameId)
        {
            return LibraryMetadataDirectory_GameBundles(SourceType, "Direct", GameId);
        }

        /// <summary>
        /// Gets the metadata directory path for game bundles based on the metadata source and game ID.
        /// </summary>
        /// <param name="SourceType">The metadata source type.</param>
        /// <param name="ProxyName">The proxy name.</param>
        /// <param name="GameId">The game ID.</param>
        /// <returns>The full path to the game bundles metadata directory.</returns>
        public string LibraryMetadataDirectory_GameBundles(FileSignature.MetadataSources SourceType, string ProxyName, long GameId)
        {
            string MetadataPath = Path.Combine(LibraryMetadataDirectory, "GameMetadata", "Bundles", SourceType.ToString(), ProxyName, GameId.ToString());
            return MetadataPath;
        }

        public string LibrarySignaturesDirectory
        {
            get
            {
                return Path.Combine(LibraryRootDirectory, "Signatures");
            }
        }

        public string LibrarySignaturesProcessedDirectory
        {
            get
            {
                return Path.Combine(LibraryRootDirectory, "Signatures - Processed");
            }
        }

        public void InitLibrary()
        {
            if (!Directory.Exists(LibraryRootDirectory)) { Directory.CreateDirectory(LibraryRootDirectory); }
            if (!Directory.Exists(LibraryImportDirectory)) { Directory.CreateDirectory(LibraryImportDirectory); }
            if (!Directory.Exists(LibraryFirmwareDirectory)) { Directory.CreateDirectory(LibraryFirmwareDirectory); }
            if (!Directory.Exists(LibraryUploadDirectory)) { Directory.CreateDirectory(LibraryUploadDirectory); }
            if (!Directory.Exists(LibraryMetadataDirectory)) { Directory.CreateDirectory(LibraryMetadataDirectory); }
            if (!Directory.Exists(LibraryContentDirectory)) { Directory.CreateDirectory(LibraryContentDirectory); }
            if (!Directory.Exists(LibraryTempDirectory)) { Directory.CreateDirectory(LibraryTempDirectory); }
            if (!Directory.Exists(LibraryCollectionsDirectory)) { Directory.CreateDirectory(LibraryCollectionsDirectory); }
            if (!Directory.Exists(LibrarySignaturesDirectory)) { Directory.CreateDirectory(LibrarySignaturesDirectory); }
            if (!Directory.Exists(LibrarySignaturesProcessedDirectory)) { Directory.CreateDirectory(LibrarySignaturesProcessedDirectory); }
        }
    }
}
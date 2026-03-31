using System.Collections.Concurrent;
using System.Configuration;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using gaseous_server.Classes.Metadata;
using gaseous_server.Classes.Plugins.FileSignatures;
using gaseous_server.Models;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NuGet.Common;

namespace gaseous_server.Classes
{
    /// <summary>
    /// Provides functionality for file signature detection and analysis, including support for compressed archives.
    /// </summary>
    public class FileSignature
    {
        private static List<IDecompressionPlugin> _decompressionPlugins = new List<IDecompressionPlugin>();

        private static List<IDecompressionPlugin> DecompressionPlugins
        {
            get
            {
                if (_decompressionPlugins.Count > 0)
                {
                    return _decompressionPlugins;
                }

                // Clear existing items
                _decompressionPlugins.Clear();

                // Dynamically discover all classes that implement IDecompressionPlugin
                var assembly = Assembly.GetExecutingAssembly();
                var pluginType = typeof(IDecompressionPlugin);

                var pluginTypes = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && pluginType.IsAssignableFrom(t))
                    .ToList();

                foreach (var type in pluginTypes)
                {
                    try
                    {
                        var plugin = Activator.CreateInstance(type) as IDecompressionPlugin;
                        if (plugin != null)
                        {
                            _decompressionPlugins.Add(plugin);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.LogKey(Logging.LogType.Warning, "process.get_signature", "decompression_plugin_load_failed", null, new string[] { type.Name }, ex);
                    }
                }

                return _decompressionPlugins;
            }
        }

        private static string[] SupportedCompressionExtensions
        {
            get
            {
                return [.. DecompressionPlugins.Select(p => p.Extension)];
            }
        }

        /// <summary>
        /// Gets the file hashes for a given file path, including analysis of compressed archives to extract hashes of contained files if applicable.
        /// </summary>
        /// <param name="library">The library item containing the file for which to compute hashes.</param>
        /// <param name="filePath">The path to the file for which to compute hashes.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the file hash information.</returns>
        public async static Task<FileHash> GetFileHashesAsync(GameLibrary.LibraryItem library, string filePath)
        {
            string ImportedFileExtension = Path.GetExtension(filePath);

            FileInfo fi = new FileInfo(filePath);

            FileHash fileHash = new FileHash
            {
                Library = library,
                FileName = Path.GetRelativePath(library.Path, filePath),
                Hash = new HashObject(filePath)
            };

            if (SupportedCompressionExtensions.Contains(ImportedFileExtension) && (fi.Length < 1073741824))
            {
                // file is a zip and less than 1 GiB
                // extract the zip file and search the contents
                string ExtractPath = Path.Combine(Config.LibraryConfiguration.LibraryTempDirectory, library.Id.ToString(), Path.GetRandomFileName());

                Logging.LogKey(Logging.LogType.Information, "process.get_signature", "getsignature.decompressing_file_to_path", null, new string[] { filePath, ExtractPath });
                if (!Directory.Exists(ExtractPath)) { Directory.CreateDirectory(ExtractPath); }
                try
                {
                    var matchingPlugin = DecompressionPlugins
                        .FirstOrDefault(plugin => plugin.Extension.Equals(ImportedFileExtension, StringComparison.OrdinalIgnoreCase));

                    if (matchingPlugin != null)
                    {
                        await matchingPlugin.DecompressFile(filePath, ExtractPath);
                    }

                    Logging.LogKey(Logging.LogType.Information, "process.get_signature", "getsignature.processing_decompressed_files_for_signature_matches");

                    // extract hashes of all files in the archive and add to fileHash.ArchiveContents
                    List<ArchiveData> archiveFiles = new List<ArchiveData>();
                    foreach (string file in Directory.GetFiles(ExtractPath, "*.*", SearchOption.AllDirectories).Where(File.Exists))
                    {
                        FileInfo zfi = new FileInfo(file);
                        HashObject zhash = new HashObject(file);

                        ArchiveData archiveData = new ArchiveData
                        {
                            FileName = Path.GetFileName(file),
                            FilePath = (zfi.DirectoryName ?? string.Empty).Replace(ExtractPath, ""),
                            Size = zfi.Length,
                            MD5 = zhash.md5hash,
                            SHA1 = zhash.sha1hash,
                            SHA256 = zhash.sha256hash,
                            CRC = zhash.crc32hash,
                            isSignatureSelector = false
                        };
                        archiveFiles.Add(archiveData);
                    }

                    fileHash.ArchiveContents = archiveFiles;
                }
                catch (Exception ex)
                {
                    Logging.LogKey(Logging.LogType.Critical, "process.get_signature", "getsignature.error_processing_compressed_file", null, new string[] { filePath }, ex);
                }
            }

            return fileHash;
        }

        /// <summary>
        /// Gets the file signature for a game file, including analysis of compressed archives.
        /// </summary>
        /// <param name="library">The library item containing the game file.</param>
        /// <param name="fileHash">The file hash object containing file checksums and archive contents.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the discovered file signature.</returns>
        public async Task<(FileHash, Signatures_Games)> GetFileSignatureAsync(GameLibrary.LibraryItem library, FileHash fileHash)
        {
            Logging.LogKey(Logging.LogType.Information, "process.get_signature", "getsignature.getting_signature_for_file", null, new string[] { fileHash.FullFilePath });
            gaseous_server.Models.Signatures_Games discoveredSignature = new gaseous_server.Models.Signatures_Games();

            FileInfo fi = new FileInfo(fileHash.FullFilePath);

            string ImportedFileExtension = Path.GetExtension(fileHash.FullFilePath);

            // get signature for the file itself first - if it's a zip, we'll then use this as the baseline signature to compare against signatures of the contained files to find the best match and determine if we need to apply a signature selector flag to any of the contained files in order to identify them as the primary signature match for the overall archive
            discoveredSignature = await _GetFileSignatureAsync(fileHash.Hash, fi.Name, fi.Extension, fi.Length, fileHash.FileName, false);

            Logging.LogKey(Logging.LogType.Information, "process.get_signature", "getsignature.processing_decompressed_files_for_signature_matches");
            // loop through archive contents until we find the first signature match
            bool signatureFound = false;
            bool signatureSelectorAlreadyApplied = false;
            if (fileHash.ArchiveContents != null)
            {
                foreach (var file in fileHash.ArchiveContents)
                {
                    file.isSignatureSelector = false;
                    HashObject zhash = new HashObject(file.MD5, file.SHA1, file.SHA256, file.CRC);

                    Logging.LogKey(Logging.LogType.Information, "process.get_signature", "getsignature.checking_signature_of_decompressed_file", null, new string[] { file.FilePath });

                    if (!signatureFound)
                    {
                        gaseous_server.Models.Signatures_Games zDiscoveredSignature = await _GetFileSignatureAsync(zhash, file.FileName, Path.GetExtension(file.FileName), file.Size, file.FilePath, true);
                        zDiscoveredSignature.Rom.Name = Path.ChangeExtension(zDiscoveredSignature.Rom.Name, ImportedFileExtension);

                        if (zDiscoveredSignature.Score > discoveredSignature.Score)
                        {
                            if (
                                zDiscoveredSignature.Rom.SignatureSource == gaseous_server.Models.Signatures_Games.RomItem.SignatureSourceType.MAMEArcade ||
                                zDiscoveredSignature.Rom.SignatureSource == gaseous_server.Models.Signatures_Games.RomItem.SignatureSourceType.MAMEMess
                            )
                            {
                                zDiscoveredSignature.Rom.Name = zDiscoveredSignature.Game.Description + ImportedFileExtension;
                            }
                            zDiscoveredSignature.Rom.Crc = discoveredSignature.Rom.Crc;
                            zDiscoveredSignature.Rom.Md5 = discoveredSignature.Rom.Md5;
                            zDiscoveredSignature.Rom.Sha1 = discoveredSignature.Rom.Sha1;
                            zDiscoveredSignature.Rom.Sha256 = discoveredSignature.Rom.Sha256;
                            zDiscoveredSignature.Rom.Size = discoveredSignature.Rom.Size;
                            discoveredSignature = zDiscoveredSignature;

                            signatureFound = true;

                            if (!signatureSelectorAlreadyApplied)
                            {
                                file.isSignatureSelector = true;
                                signatureSelectorAlreadyApplied = true;
                            }
                        }
                    }
                }
            }

            if (discoveredSignature.Rom.Attributes == null)
            {
                discoveredSignature.Rom.Attributes = new Dictionary<string, object>();
            }

            discoveredSignature.Rom.Attributes.Add(
                "ZipContents", Newtonsoft.Json.JsonConvert.SerializeObject(fileHash.ArchiveContents)
            );


            // get discovered platform
            Platform? determinedPlatform = null;
            if (library.DefaultPlatformId == null || library.DefaultPlatformId == 0)
            {
                determinedPlatform = await Metadata.Platforms.GetPlatform((long)discoveredSignature.Flags.PlatformId);
                if (determinedPlatform == null)
                {
                    determinedPlatform = new Platform();
                }
            }
            else
            {
                determinedPlatform = await Metadata.Platforms.GetPlatform((long)library.DefaultPlatformId);
                if (determinedPlatform != null && determinedPlatform.Id.HasValue)
                {
                    discoveredSignature.MetadataSources.AddPlatform((long)determinedPlatform.Id, determinedPlatform.Name ?? "Unknown Platform", FileSignature.MetadataSources.None);
                }
            }

            // get discovered game
            if (discoveredSignature.Flags.GameId == 0)
            {
                string gameName = discoveredSignature.Game?.Name ?? "Unknown Game";
                discoveredSignature.MetadataSources.AddGame(0, gameName, FileSignature.MetadataSources.None);
            }

            return (fileHash, discoveredSignature);
        }

        private async Task<Signatures_Games> _GetFileSignatureAsync(HashObject hash, string ImageName, string ImageExtension, long ImageSize, string GameFileImportPath, bool IsInZip)
        {
            Logging.LogKey(Logging.LogType.Information, "process.import_game", "importgame.checking_signature_for_file", null, new string[] { GameFileImportPath, hash.md5hash, hash.sha1hash, hash.sha256hash, hash.crc32hash });

            // setup plugins
            List<Plugins.FileSignatures.IFileSignaturePlugin> plugins = new List<Plugins.FileSignatures.IFileSignaturePlugin>();

            if (Config.MetadataConfiguration.SignatureSource != HasheousClient.Models.MetadataModel.SignatureSources.LocalOnly)
            {
                Logging.LogKey(Logging.LogType.Information, "process.import_game", "importgame.hasheous_enabled_searching_remote_then_local");
                plugins.Add(new Plugins.FileSignatures.Hasheous());
            }
            else
            {
                Logging.LogKey(Logging.LogType.Information, "process.import_game", "importgame.hasheous_disabled_searching_local_only");
            }
            plugins.Add(new Plugins.FileSignatures.Database());
            plugins.Add(new Plugins.FileSignatures.InspectFile());

            gaseous_server.Models.Signatures_Games? discoveredSignature = null;

            // loop plugins - first to return a signature wins
            foreach (Plugins.FileSignatures.IFileSignaturePlugin plugin in plugins)
            {
                discoveredSignature = await plugin.GetSignature(hash, ImageName, ImageExtension, ImageSize, GameFileImportPath);
                if (discoveredSignature != null)
                {
                    break;
                }
            }

            gaseous_server.Models.PlatformMapping.GetIGDBPlatformMapping(ref discoveredSignature, ImageExtension, false);

            string gameNameArg = discoveredSignature.Game?.Name ?? "";
            int parsedYear = 0;
            if (!string.IsNullOrWhiteSpace(discoveredSignature.Game?.Year))
            {
                int.TryParse(discoveredSignature.Game.Year, out parsedYear);
            }
            string gameYearArg = parsedYear.ToString();
            string gameSystemArg = discoveredSignature.Game?.System ?? "";
            Logging.LogKey(Logging.LogType.Information, "process.import_game", "importgame.determined_import_file_as", null, new string[] { gameNameArg, gameYearArg, gameSystemArg });
            string platformNameArg = discoveredSignature.Flags?.PlatformName ?? "";
            string platformIdArg = (discoveredSignature.Flags?.PlatformId ?? 0).ToString();
            Logging.LogKey(Logging.LogType.Information, "process.import_game", "importgame.platform_determined_to_be", null, new string[] { platformNameArg, platformIdArg });

            return discoveredSignature;
        }

        /// <summary>
        /// Represents the hash information for a file, including any relevant metadata about archive contents if the file is a compressed archive.
        /// </summary>
        public class FileHash
        {
            /// <summary>
            /// Gets or sets the library item associated with the file for which the hash information is being stored.
            /// </summary>
            public required GameLibrary.LibraryItem Library { get; set; }

            /// <summary>
            /// Gets or sets the name of the file and path for which the hash information is being stored. Should be relative to the library path and include teh file name and extension (e.g. "Super Mario World.smc" or "Archive.zip"). If the file is a compressed archive, this should be the name of the archive file itself, not the individual files contained within the archive.
            /// </summary>
            public required string FileName { get; set; }

            /// <summary>
            /// Gets the full file path by combining the library path and the file name. This is a convenience property to easily access the full path of the file for which the hash information is being stored.
            /// </summary>
            public string FullFilePath
            {
                get
                {
                    return Path.Combine(Library.Path, FileName);
                }
            }

            /// <summary>
            /// Gets or sets the hash information of the file.
            /// </summary>
            public required HashObject Hash { get; set; }

            /// <summary>
            /// Gets or sets the list of files contained within the archive, if the file is a compressed archive.
            /// </summary>
            public List<ArchiveData> ArchiveContents { get; set; } = new List<ArchiveData>();
        }

        /// <summary>
        /// Represents data about a file contained within an archive.
        /// </summary>
        public class ArchiveData
        {
            /// <summary>
            /// Gets or sets the name of the file.
            /// </summary>
            public required string FileName { get; set; }

            /// <summary>
            /// Gets or sets the path of the file within the archive.
            /// </summary>
            public required string FilePath { get; set; }

            /// <summary>
            /// Gets or sets the size of the file in bytes.
            /// </summary>
            public long Size { get; set; } = 0;

            /// <summary>
            /// Gets or sets the MD5 hash of the file.
            /// </summary>
            public required string MD5 { get; set; }

            /// <summary>
            /// Gets or sets the SHA1 hash of the file.
            /// </summary>
            public required string SHA1 { get; set; }

            /// <summary>
            /// Gets or sets the SHA256 hash of the file.
            /// </summary>
            public required string SHA256 { get; set; }

            /// <summary>
            /// Gets or sets the CRC32 hash of the file.
            /// </summary>
            public required string CRC { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether this file is used as a signature selector.
            /// </summary>
            public bool isSignatureSelector { get; set; } = false;
        }

        /// <summary>
        /// Specifies the various metadata sources available for game information.
        /// </summary>
        public enum MetadataSources
        {
            /// <summary>
            /// No metadata source specified.
            /// </summary>
            None = 0,

            /// <summary>
            /// Internet Game Database (IGDB)
            /// </summary>
            IGDB = 1,

            /// <summary>
            /// TheGamesDb.net
            /// </summary>
            TheGamesDb = 2,

            /// <summary>
            /// RetroAchievements
            /// </summary>
            RetroAchievements = 3,

            /// <summary>
            /// GiantBomb
            /// </summary>
            GiantBomb = 4,

            /// <summary>
            /// Steam
            /// </summary>
            Steam = 5,

            /// <summary>
            /// Good Old Games (GOG)
            /// </summary>
            GOG = 6,

            /// <summary>
            /// Epic Games Store
            /// </summary>
            EpicGameStore = 7,

            /// <summary>
            /// Wikipedia
            /// </summary>
            Wikipedia = 8,

            /// <summary>
            /// SteamGridDb
            /// </summary>
            SteamGridDb = 9,

            /// <summary>
            /// ScreenScraper
            /// </summary>
            ScreenScraper = 10,

            /// <summary>
            /// Unknown metadata source.
            /// </summary>
            Unknown = 9999
        }
    }
}
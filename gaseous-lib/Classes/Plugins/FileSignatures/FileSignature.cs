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

        private static readonly HashSet<string> FastGuardExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".exe",
            ".com",
            ".bat",
            ".cmd"
        };

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

        private static IDecompressionPlugin? FindDecompressionPlugin(string filePath, bool useExtension)
        {
            var plugins = DecompressionPlugins;
            if (plugins.Count == 0) return null;

            int maxMagicBytes = plugins.Max(p => p.MagicBytes.Length);
            if (maxMagicBytes == 0) return null;

            byte[] header = new byte[maxMagicBytes];
            int bytesRead;
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                bytesRead = fs.Read(header, 0, maxMagicBytes);
            }

            foreach (var plugin in plugins)
            {
                if (useExtension)
                {
                    if (string.Equals(Path.GetExtension(filePath), plugin.Extension, StringComparison.OrdinalIgnoreCase))
                    {
                        return plugin;
                    }
                }
                else
                {
                    if (plugin.MagicBytes.Length > bytesRead) continue;
                    bool match = true;
                    for (int i = 0; i < plugin.MagicBytes.Length; i++)
                    {
                        if (header[i] != plugin.MagicBytes[i]) { match = false; break; }
                    }
                    if (match) return plugin;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the file hashes for a given file path, including analysis of compressed archives to extract hashes of contained files if applicable.
        /// </summary>
        /// <param name="library">The library item containing the file for which to compute hashes.</param>
        /// <param name="filePath">The path to the file for which to compute hashes.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the file hash information.</returns>
        public async static Task<FileHash> GetFileHashesAsync(GameLibrary.LibraryItem library, string filePath)
        {
            FileInfo fi = new FileInfo(filePath);

            FileHash fileHash = new FileHash
            {
                Library = library,
                FileName = Path.GetRelativePath(library.Path, filePath),
                FileExtension = fi.Extension,
                Hash = new HashObject(filePath)
            };

            if (FastGuardExtensions.Contains(fi.Extension))
            {
                return fileHash;
            }

            var matchingPlugin = FindDecompressionPlugin(filePath, true);

            if (matchingPlugin != null && (fi.Length < 1073741824))
            {
                // file is a compressed archive and less than 1 GiB
                // extract the archive and search the contents
                string ExtractPath = Path.Combine(Config.LibraryConfiguration.LibraryTempDirectory, library.Id.ToString(), Path.GetRandomFileName());

                fileHash.FileExtension = matchingPlugin.Extension;

                Logging.LogKey(Logging.LogType.Information, "process.get_signature", "getsignature.decompressing_file_to_path", null, new string[] { filePath, ExtractPath });
                if (!Directory.Exists(ExtractPath)) { Directory.CreateDirectory(ExtractPath); }
                try
                {
                    await matchingPlugin.DecompressFile(filePath, ExtractPath);

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
                            Hash = zhash,
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

            discoveredSignature = await _GetFileSignatureAsync(fileHash, fi.Name, fi.Extension, fi.Length, fileHash.FileName, false);

            Logging.LogKey(Logging.LogType.Information, "process.get_signature", "getsignature.processing_decompressed_files_for_signature_matches");

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

        private async Task<Signatures_Games> _GetFileSignatureAsync(FileHash hash, string ImageName, string ImageExtension, long ImageSize, string GameFileImportPath, bool IsInZip)
        {
            Logging.LogKey(Logging.LogType.Information, "process.import_game", "importgame.checking_signature_for_file", null, new string[] { GameFileImportPath, hash.Hash.md5hash, hash.Hash.sha1hash, hash.Hash.sha256hash, hash.Hash.crc32hash });

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
            /// Gets or sets the file extension of the file for which the hash information is being stored. This property is used when storing the file in the library to ensure that the file is saved with the correct extension.
            /// </summary>
            public required string FileExtension { get; set; }

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

            /// <summary>
            /// Gets the top 5 candidate files within the archive that are most likely to be the primary signature match for the overall archive
            /// </summary>
            [System.Text.Json.Serialization.JsonIgnore]
            [Newtonsoft.Json.JsonIgnore]
            public List<ArchiveData> MatchCandidates
            {
                get
                {
                    if (ArchiveContents == null || ArchiveContents.Count == 0)
                    {
                        return new List<ArchiveData>();
                    }

                    var imagesInArchive = ArchiveContents.Any(a => new string[] { ".iso", ".bin", ".cue", ".img", ".ima" }.Contains(Path.GetExtension(a.FileName).ToLower()));

                    if (imagesInArchive)
                    {
                        // if archive contains .cue, then we need to look for a .bin file and use that as the signature match candidate instead of the .cue file, as .cue files often contain metadata and may not be the primary signature match for the overall archive. if not, fall back to looking for .iso files as the primary signature match candidate, as these are more likely to be the primary signature match for the overall archive than other file types based on common usage in game ROMs and emulation.
                        var cueFilesInArchive = ArchiveContents.Any(a => Path.GetExtension(a.FileName).ToLower() == ".cue");
                        if (cueFilesInArchive)
                        {
                            var binFilesInArchive = ArchiveContents.Where(a => Path.GetExtension(a.FileName).ToLower() == ".bin").OrderByDescending(a => a.Score).Take(1).ToList();
                            if (binFilesInArchive.Count > 0)
                            {
                                return binFilesInArchive;
                            }
                        }

                        var isoFilesInArchive = ArchiveContents.Where(a => new string[] { ".iso", ".img", ".ima" }.Contains(Path.GetExtension(a.FileName).ToLower())).OrderByDescending(a => a.Score).Take(1).ToList();
                        if (isoFilesInArchive.Count > 0)
                        {
                            return isoFilesInArchive;
                        }
                    }

                    var topCandidates = ArchiveContents.OrderByDescending(a => a.Score).Take(5).ToList();
                    return topCandidates;
                }
            }
        }

        /// <summary>
        /// Represents data about a file contained within an archive.
        /// </summary>
        public class ArchiveData
        {
            private readonly string[] superLikelyExtensions = new string[] { ".exe", ".com" };
            private readonly string[] likelyPrimaryExtensions = new string[] { ".smc", ".sfc", ".nes", ".gb", ".gba", ".bin", ".cue", ".iso", ".img", ".ima", ".rom", ".zip", ".7z", ".rar", ".exe", ".dll" };
            private readonly string[] unlikelyPrimaryExtensions = new string[] { ".txt", ".pdf", ".docx", ".xlsx", ".pptx", ".nfo", ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".mp3", ".ogg", ".wav", ".flac", ".aac" };
            private readonly string[] excludedFileNames = new string[] { "readme.txt", "readme.md", "license.txt", "license.md", "changelog.txt", "changelog.md", "setup.exe", "install.exe", "uninstall.exe", "autorun.exe", "thumbs.db", "desktop.ini", ".ds_store", "intro.exe", "demo.exe" };

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
            public long Size { get; set; } = 1;

            /// <summary>
            /// Gets the score of the file as a potential signature match for the overall archive, based on factors such as file type and size. This score is used to determine which file within the archive is most likely to be the primary signature match for the overall archive when comparing against known signatures in the database.
            /// </summary>
            [System.Text.Json.Serialization.JsonIgnore]
            [Newtonsoft.Json.JsonIgnore]
            public int Score
            {
                get
                {
                    if (Size == 0)
                    {
                        return 0;
                    }

                    int score = 1;

                    // check the file type - certain file types are more likely to be the primary signature match for an archive than others based on common usage in game ROMs and emulation (e.g. .smc, .sfc, .nes, .gb, .gba, .bin, .cue, etc. are more likely to be primary signature matches than .txt, .pdf, .docx, etc.)
                    if (excludedFileNames.Contains(FileName.ToLower()) || excludedFileNames.Contains(FilePath.ToLower()))
                    {
                        return 0;
                    }

                    if (
                        superLikelyExtensions.Contains(Path.GetExtension(FileName).ToLower()) ||
                        superLikelyExtensions.Contains(Path.GetExtension(FilePath).ToLower())
                        )
                    {
                        score += 200;
                    }
                    if (
                        likelyPrimaryExtensions.Contains(Path.GetExtension(FileName).ToLower()) ||
                        likelyPrimaryExtensions.Contains(Path.GetExtension(FilePath).ToLower())
                        )
                    {
                        score += 100;
                    }
                    else if (
                        unlikelyPrimaryExtensions.Contains(Path.GetExtension(FileName).ToLower()) ||
                        unlikelyPrimaryExtensions.Contains(Path.GetExtension(FilePath).ToLower())
                        )
                    {
                        score += 50;
                    }
                    return score;
                }
            }

            /// <summary>
            /// Gets or sets the hash object for the archive file
            /// </summary>
            public required HashObject Hash { get; set; }

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
            /// Launchbox
            /// </summary>
            Launchbox = 11,

            /// <summary>
            /// Hasheous file signature database
            /// </summary>
            Hasheous = 9000,

            /// <summary>
            /// Unknown metadata source.
            /// </summary>
            Unknown = 9999
        }
    }
}
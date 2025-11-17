using System.Collections.Concurrent;
using System.Configuration;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using gaseous_server.Classes.Metadata;
using gaseous_server.Models;
using HasheousClient.Models.Metadata.IGDB;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NuGet.Common;
using SevenZip;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.Zip;
using SharpCompress.Common;

namespace gaseous_server.Classes
{
    public class FileSignature
    {
        public async Task<Signatures_Games> GetFileSignatureAsync(GameLibrary.LibraryItem library, HashObject hash, FileInfo fi, string GameFileImportPath)
        {
            Logging.LogKey(Logging.LogType.Information, "process.get_signature", "getsignature.getting_signature_for_file", null, new string[] { GameFileImportPath });
            gaseous_server.Models.Signatures_Games discoveredSignature = new gaseous_server.Models.Signatures_Games();
            discoveredSignature = await _GetFileSignatureAsync(hash, fi.Name, fi.Extension, fi.Length, GameFileImportPath, false);

            string[] CompressionExts = { ".zip", ".rar", ".7z" };
            string ImportedFileExtension = Path.GetExtension(GameFileImportPath);

            if (CompressionExts.Contains(ImportedFileExtension) && (fi.Length < 1073741824))
            {
                // file is a zip and less than 1 GiB
                // extract the zip file and search the contents

                string ExtractPath = Path.Combine(Config.LibraryConfiguration.LibraryTempDirectory, library.Id.ToString(), Path.GetRandomFileName());
                Logging.LogKey(Logging.LogType.Information, "process.get_signature", "getsignature.decompressing_file_to_path", null, new string[] { GameFileImportPath, ExtractPath });
                if (!Directory.Exists(ExtractPath)) { Directory.CreateDirectory(ExtractPath); }
                try
                {
                    switch (ImportedFileExtension)
                    {
                        case ".zip":
                            Logging.LogKey(Logging.LogType.Information, "process.get_signature", "getsignature.decompressing_using_zip");
                            try
                            {
                                using (var archive = SharpCompress.Archives.Zip.ZipArchive.Open(GameFileImportPath))
                                {
                                    foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                                    {
                                        Logging.LogKey(Logging.LogType.Information, "process.get_signature", "getsignature.extracting_file", null, new string[] { entry.Key });
                                        entry.WriteToDirectory(ExtractPath, new ExtractionOptions()
                                        {
                                            ExtractFullPath = true,
                                            Overwrite = true
                                        });
                                    }
                                }
                            }
                            catch (Exception zipEx)
                            {
                                Logging.LogKey(Logging.LogType.Warning, "process.get_signature", "getsignature.unzip_error", null, null, zipEx);
                                throw;
                            }
                            break;

                        case ".rar":
                            Logging.LogKey(Logging.LogType.Information, "process.get_signature", "getsignature.decompressing_using_rar");
                            try
                            {
                                using (var archive = RarArchive.Open(GameFileImportPath))
                                {
                                    foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                                    {
                                        Logging.LogKey(Logging.LogType.Information, "process.get_signature", "getsignature.extracting_file", null, new string[] { entry.Key });
                                        entry.WriteToDirectory(ExtractPath, new ExtractionOptions()
                                        {
                                            ExtractFullPath = true,
                                            Overwrite = true
                                        });
                                    }
                                }
                            }
                            catch (Exception zipEx)
                            {
                                Logging.LogKey(Logging.LogType.Warning, "process.get_signature", "getsignature.unrar_error", null, null, zipEx);
                                throw;
                            }
                            break;

                        case ".7z":
                            Logging.LogKey(Logging.LogType.Information, "process.get_signature", "getsignature.decompressing_using_7z");
                            try
                            {
                                using (var archive = SharpCompress.Archives.SevenZip.SevenZipArchive.Open(GameFileImportPath))
                                {
                                    foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                                    {
                                        Logging.LogKey(Logging.LogType.Information, "process.get_signature", "getsignature.extracting_file", null, new string[] { entry.Key });
                                        entry.WriteToDirectory(ExtractPath, new ExtractionOptions()
                                        {
                                            ExtractFullPath = true,
                                            Overwrite = true
                                        });
                                    }
                                }
                            }
                            catch (Exception zipEx)
                            {
                                Logging.LogKey(Logging.LogType.Warning, "process.get_signature", "getsignature.sevenzip_error", null, null, zipEx);
                                throw;
                            }
                            break;
                    }

                    Logging.LogKey(Logging.LogType.Information, "process.get_signature", "getsignature.processing_decompressed_files_for_signature_matches");
                    // loop through contents until we find the first signature match
                    List<ArchiveData> archiveFiles = new List<ArchiveData>();
                    bool signatureFound = false;
                    bool signatureSelectorAlreadyApplied = false;
                    foreach (string file in Directory.GetFiles(ExtractPath, "*.*", SearchOption.AllDirectories))
                    {
                        bool signatureSelector = false;
                        if (File.Exists(file))
                        {
                            FileInfo zfi = new FileInfo(file);
                            HashObject zhash = new HashObject(file);

                            Logging.LogKey(Logging.LogType.Information, "process.get_signature", "getsignature.checking_signature_of_decompressed_file", null, new string[] { file });

                            if (zfi != null)
                            {
                                if (signatureFound == false)
                                {
                                    gaseous_server.Models.Signatures_Games zDiscoveredSignature = await _GetFileSignatureAsync(zhash, zfi.Name, zfi.Extension, zfi.Length, file, true);
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

                                        if (signatureSelectorAlreadyApplied == false)
                                        {
                                            signatureSelector = true;
                                            signatureSelectorAlreadyApplied = true;
                                        }
                                    }
                                }

                                ArchiveData archiveData = new ArchiveData
                                {
                                    FileName = Path.GetFileName(file),
                                    FilePath = zfi.Directory.FullName.Replace(ExtractPath, ""),
                                    Size = zfi.Length,
                                    MD5 = zhash.md5hash,
                                    SHA1 = zhash.sha1hash,
                                    SHA256 = zhash.sha256hash,
                                    CRC = zhash.crc32hash,
                                    isSignatureSelector = signatureSelector
                                };
                                archiveFiles.Add(archiveData);
                            }
                        }
                    }

                    if (discoveredSignature.Rom.Attributes == null)
                    {
                        discoveredSignature.Rom.Attributes = new Dictionary<string, object>();
                    }

                    discoveredSignature.Rom.Attributes.Add(
                         "ZipContents", Newtonsoft.Json.JsonConvert.SerializeObject(archiveFiles)
                    );
                }
                catch (Exception ex)
                {
                    Logging.LogKey(Logging.LogType.Critical, "process.get_signature", "getsignature.error_processing_compressed_file", null, new string[] { GameFileImportPath }, ex);
                }
            }

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

            return discoveredSignature;
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

        public class ArchiveData
        {
            public string FileName { get; set; }
            public string FilePath { get; set; }
            public long Size { get; set; }
            public string MD5 { get; set; }
            public string SHA1 { get; set; }
            public string SHA256 { get; set; }
            public string CRC { get; set; }
            public bool isSignatureSelector { get; set; } = false;
        }

        public enum MetadataSources
        {
            None,
            IGDB,
            TheGamesDb,
            RetroAchievements,
            GiantBomb,
            Steam,
            GOG,
            EpicGameStore,
            Wikipedia
        }
    }
}
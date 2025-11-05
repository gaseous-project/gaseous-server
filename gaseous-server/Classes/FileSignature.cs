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


            gaseous_server.Models.Signatures_Games? discoveredSignature = null;

            // begin signature search
            switch (Config.MetadataConfiguration.SignatureSource)
            {
                case HasheousClient.Models.MetadataModel.SignatureSources.LocalOnly:
                    Logging.LogKey(Logging.LogType.Information, "process.import_game", "importgame.hasheous_disabled_searching_local_only");

                    discoveredSignature = await _GetFileSignatureFromDatabase(hash, ImageName, ImageExtension, ImageSize, GameFileImportPath);

                    break;

                case HasheousClient.Models.MetadataModel.SignatureSources.Hasheous:
                    Logging.LogKey(Logging.LogType.Information, "process.import_game", "importgame.hasheous_enabled_searching_remote_then_local");

                    discoveredSignature = await _GetFileSignatureFromHasheous(hash, ImageName, ImageExtension, ImageSize, GameFileImportPath);

                    if (discoveredSignature == null)
                    {
                        Logging.LogKey(Logging.LogType.Information, "process.import_game", "importgame.signature_not_found_remote_checking_local");

                        discoveredSignature = await _GetFileSignatureFromDatabase(hash, ImageName, ImageExtension, ImageSize, GameFileImportPath);
                    }
                    else
                    {
                        Logging.LogKey(Logging.LogType.Information, "process.import_game", "importgame.signature_retrieved_remote_for_game", null, new string[] { discoveredSignature.Game.Name });
                    }
                    break;

            }

            if (discoveredSignature == null)
            {
                // construct a signature from file data
                Logging.LogKey(Logging.LogType.Information, "process.import_game", "importgame.signature_not_found_generating_from_file_data");

                discoveredSignature = await _GetFileSignatureFromFileData(hash, ImageName, ImageExtension, ImageSize, GameFileImportPath);

                Logging.LogKey(Logging.LogType.Information, "process.import_game", "importgame.signature_generated_for_game", null, new string[] { discoveredSignature.Game.Name });
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

        private async Task<gaseous_server.Models.Signatures_Games?> _GetFileSignatureFromDatabase(HashObject hash, string ImageName, string ImageExtension, long ImageSize, string GameFileImportPath)
        {
            // check 1: do we have a signature for it?
            gaseous_server.Classes.SignatureManagement sc = new SignatureManagement();

            Logging.LogKey(Logging.LogType.Information, "process.get_signature", "getsignature.checking_local_database_for_hash", null, new string[] { hash.sha256hash });

            List<gaseous_server.Models.Signatures_Games> signatures = await sc.GetSignature(hash);

            gaseous_server.Models.Signatures_Games? discoveredSignature = null;
            if (signatures.Count == 1)
            {
                // only 1 signature found!
                discoveredSignature = signatures.ElementAt(0);

                return discoveredSignature;
            }
            else if (signatures.Count > 1)
            {
                // more than one signature found - find one with highest score
                // start with first returned element
                discoveredSignature = signatures.First();
                foreach (gaseous_server.Models.Signatures_Games Sig in signatures)
                {
                    if (Sig.Score > discoveredSignature.Score)
                    {
                        discoveredSignature = Sig;
                    }
                }

                return discoveredSignature;
            }

            return null;
        }

        private async Task<gaseous_server.Models.Signatures_Games?> _GetFileSignatureFromHasheous(HashObject hash, string ImageName, string ImageExtension, long ImageSize, string GameFileImportPath)
        {
            // check if hasheous is enabled, and if so use it's signature database
            if (Config.MetadataConfiguration.SignatureSource == HasheousClient.Models.MetadataModel.SignatureSources.Hasheous)
            {
                HasheousClient.Hasheous hasheous = new HasheousClient.Hasheous();
                if (HasheousClient.WebApp.HttpHelper.Headers.ContainsKey("CacheControl"))
                {
                    HasheousClient.WebApp.HttpHelper.Headers["CacheControl"] = "no-cache";
                }
                else
                {
                    HasheousClient.WebApp.HttpHelper.Headers.Add("CacheControl", "no-cache");
                }
                if (HasheousClient.WebApp.HttpHelper.Headers.ContainsKey("Pragma"))
                {
                    HasheousClient.WebApp.HttpHelper.Headers["Pragma"] = "no-cache";
                }
                else
                {
                    HasheousClient.WebApp.HttpHelper.Headers.Add("Pragma", "no-cache");
                }

                Console.WriteLine(HasheousClient.WebApp.HttpHelper.BaseUri);
                HasheousClient.Models.LookupItemModel? HasheousResult = null;
                try
                {
                    // check the cache first
                    if (!Directory.Exists(Config.LibraryConfiguration.LibraryMetadataDirectory_Hasheous()))
                    {
                        Directory.CreateDirectory(Config.LibraryConfiguration.LibraryMetadataDirectory_Hasheous());
                    }
                    // create file name from hash object
                    string cacheFileName = hash.md5hash + "_" + hash.sha1hash + "_" + hash.crc32hash + ".json";
                    string cacheFilePath = Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_Hasheous(), cacheFileName);
                    // use cache file if it exists and is less than 30 days old, otherwise fetch from hasheous. if the fetch from hasheous is successful, save it to the cache, if it fails, use the cache if it exists even if it's old
                    if (File.Exists(cacheFilePath))
                    {
                        FileInfo cacheFile = new FileInfo(cacheFilePath);
                        if (cacheFile.LastWriteTimeUtc > DateTime.UtcNow.AddDays(-30))
                        {
                            Logging.LogKey(Logging.LogType.Information, "process.get_signature", "getsignature.using_cached_signature_from_hasheous");
                            HasheousResult = Newtonsoft.Json.JsonConvert.DeserializeObject<HasheousClient.Models.LookupItemModel>(await File.ReadAllTextAsync(cacheFilePath));
                        }
                    }

                    try
                    {
                        if (HasheousResult == null)
                        {
                            // fetch from hasheous
                            HasheousResult = hasheous.RetrieveFromHasheous(new HasheousClient.Models.HashLookupModel
                            {
                                MD5 = hash.md5hash,
                                SHA1 = hash.sha1hash,
                                SHA256 = hash.sha256hash,
                                CRC = hash.crc32hash
                            }, false);

                            if (HasheousResult != null)
                            {
                                // save to cache
                                await File.WriteAllTextAsync(cacheFilePath, Newtonsoft.Json.JsonConvert.SerializeObject(HasheousResult));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex.Message.Contains("404"))
                        {
                            Logging.LogKey(Logging.LogType.Information, "process.get_signature", "getsignature.no_signature_found_in_hasheous");
                        }
                        else if (ex.Message.Contains("403"))
                        {
                            Logging.LogKey(Logging.LogType.Warning, "process.get_signature", "getsignature.hasheous_api_key_invalid_or_expired_using_cached_signature");
                        }
                        else
                        {

                            if (File.Exists(cacheFilePath))
                            {
                                Logging.LogKey(Logging.LogType.Warning, "process.get_signature", "getsignature.error_retrieving_signature_from_hasheous_using_cached_signature", null, null, ex);
                                HasheousResult = Newtonsoft.Json.JsonConvert.DeserializeObject<HasheousClient.Models.LookupItemModel>(await File.ReadAllTextAsync(cacheFilePath));
                            }
                            else
                            {
                                Logging.LogKey(Logging.LogType.Warning, "process.get_signature", "getsignature.error_retrieving_signature_from_hasheous", null, null, ex);
                            }
                        }
                    }

                    if (HasheousResult != null)
                    {
                        if (HasheousResult.Signature != null)
                        {
                            gaseous_server.Models.Signatures_Games signature = new Models.Signatures_Games();
                            string gameJson = Newtonsoft.Json.JsonConvert.SerializeObject(HasheousResult.Signature.Game);
                            string romJson = Newtonsoft.Json.JsonConvert.SerializeObject(HasheousResult.Signature.Rom);
                            signature.Game = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.Signatures_Games.GameItem>(gameJson);
                            signature.Rom = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.Signatures_Games.RomItem>(romJson);

                            // get platform metadata
                            if (HasheousResult.Platform != null)
                            {
                                if (HasheousResult.Platform.metadata.Count > 0)
                                {
                                    foreach (HasheousClient.Models.MetadataItem metadataResult in HasheousResult.Platform.metadata)
                                    {
                                        if (Enum.TryParse<MetadataSources>(metadataResult.Source, out MetadataSources metadataSource))
                                        {
                                            // only IGDB metadata is supported
                                            if (metadataSource == MetadataSources.IGDB)
                                            {
                                                // check if the immutable id is a long
                                                if (metadataResult.ImmutableId.Length > 0 && long.TryParse(metadataResult.ImmutableId, out long immutableId) == true)
                                                {
                                                    // use immutable id
                                                    Platform hasheousPlatform = await Platforms.GetPlatform(immutableId);
                                                    signature.MetadataSources.AddPlatform((long)hasheousPlatform.Id, hasheousPlatform.Name, metadataSource);
                                                }
                                                else
                                                {
                                                    // immutable id is a string
                                                    Platform hasheousPlatform = await Platforms.GetPlatform(metadataResult.ImmutableId);
                                                    if (hasheousPlatform != null)
                                                    {
                                                        signature.MetadataSources.AddPlatform((long)hasheousPlatform.Id, hasheousPlatform.Name, metadataSource);
                                                    }
                                                    else
                                                    {
                                                        // unresolvable immutableid - use unknown platform
                                                        signature.MetadataSources.AddPlatform(0, "Unknown Platform", MetadataSources.None);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            // get game metadata
                            if (HasheousResult.Metadata != null)
                            {
                                if (HasheousResult.Metadata.Count > 0)
                                {
                                    foreach (HasheousClient.Models.MetadataItem metadataResult in HasheousResult.Metadata)
                                    {
                                        if (Enum.TryParse<MetadataSources>(metadataResult.Source, out MetadataSources metadataSource))
                                        {
                                            if (metadataResult.ImmutableId.Length > 0)
                                            {
                                                switch (metadataSource)
                                                {
                                                    case FileSignature.MetadataSources.IGDB:
                                                        // check if the immutable id is a long
                                                        if (metadataResult.ImmutableId.Length > 0 && long.TryParse(metadataResult.ImmutableId, out long immutableId) == true)
                                                        {
                                                            // use immutable id
                                                            gaseous_server.Models.Game hasheousGame = await Games.GetGame(FileSignature.MetadataSources.IGDB, immutableId);
                                                            signature.MetadataSources.AddGame((long)hasheousGame.Id, hasheousGame.Name, metadataSource);
                                                        }
                                                        else
                                                        {
                                                            // immutable id is a string
                                                            gaseous_server.Models.Game hasheousGame = await Games.GetGame(FileSignature.MetadataSources.IGDB, metadataResult.ImmutableId);
                                                            if (hasheousGame != null)
                                                            {
                                                                signature.MetadataSources.AddGame((long)hasheousGame.Id, hasheousGame.Name, metadataSource);
                                                            }
                                                            else
                                                            {
                                                                // unresolvable immutable id - use unknown game
                                                                signature.MetadataSources.AddGame(0, "Unknown Game", FileSignature.MetadataSources.None);
                                                            }
                                                        }
                                                        break;

                                                    default:
                                                        if (long.TryParse(metadataResult.ImmutableId, out long id) == true)
                                                        {
                                                            signature.MetadataSources.AddGame(id, HasheousResult.Name, metadataSource);
                                                        }
                                                        else
                                                        {
                                                            signature.MetadataSources.AddGame(0, "Unknown Game", FileSignature.MetadataSources.None);
                                                        }
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                // unresolvable immutable id - use unknown game
                                                signature.MetadataSources.AddGame(0, "Unknown Game", FileSignature.MetadataSources.None);
                                            }
                                        }
                                    }
                                }
                            }

                            // check attributes for a user manual link
                            if (HasheousResult.Attributes != null)
                            {
                                if (HasheousResult.Attributes.Count > 0)
                                {
                                    foreach (HasheousClient.Models.AttributeItem attribute in HasheousResult.Attributes)
                                    {
                                        if (attribute.attributeName == HasheousClient.Models.AttributeItem.AttributeName.VIMMManualId)
                                        {
                                            signature.Game.UserManual = attribute.GetType().GetProperty("Link").GetValue(attribute).ToString();
                                        }
                                    }
                                }
                            }

                            return signature;
                        }
                    }
                }
                catch (AggregateException aggEx)
                {
                    foreach (Exception ex in aggEx.InnerExceptions)
                    {
                        // get exception type
                        if (ex is HttpRequestException)
                        {
                            if (ex.Message.Contains("404 (Not Found)"))
                            {
                                Logging.LogKey(Logging.LogType.Information, "process.get_signature", "getsignature.no_signature_found_in_hasheous");
                            }
                            else
                            {
                                Logging.LogKey(Logging.LogType.Warning, "process.get_signature", "getsignature.error_retrieving_signature_from_hasheous", null, null, ex);
                                throw;
                            }
                        }
                        else
                        {
                            Logging.LogKey(Logging.LogType.Warning, "process.get_signature", "getsignature.error_retrieving_signature_from_hasheous", null, null, ex);
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogKey(Logging.LogType.Warning, "process.get_signature", "getsignature.error_retrieving_signature_from_hasheous", null, null, ex);
                }
            }

            return null;
        }

        private async Task<gaseous_server.Models.Signatures_Games> _GetFileSignatureFromFileData(HashObject hash, string ImageName, string ImageExtension, long ImageSize, string GameFileImportPath)
        {
            SignatureManagement signatureManagement = new SignatureManagement();

            gaseous_server.Models.Signatures_Games discoveredSignature = new gaseous_server.Models.Signatures_Games();

            // no signature match found - try name search
            List<gaseous_server.Models.Signatures_Games> signatures = await signatureManagement.GetByTosecName(ImageName);

            if (signatures.Count == 1)
            {
                // only 1 signature found!
                discoveredSignature = signatures.ElementAt(0);

                return discoveredSignature;
            }
            else if (signatures.Count > 1)
            {
                // more than one signature found - find one with highest score
                foreach (gaseous_server.Models.Signatures_Games Sig in signatures)
                {
                    if (Sig.Score > discoveredSignature.Score)
                    {
                        discoveredSignature = Sig;
                    }
                }

                return discoveredSignature;
            }
            else
            {
                // still no search - try alternate method
                gaseous_server.Models.Signatures_Games.GameItem gi = new gaseous_server.Models.Signatures_Games.GameItem();
                gaseous_server.Models.Signatures_Games.RomItem ri = new gaseous_server.Models.Signatures_Games.RomItem();

                discoveredSignature.Game = gi;
                discoveredSignature.Rom = ri;

                // game title is the file name without the extension or path
                gi.Name = Path.GetFileNameWithoutExtension(GameFileImportPath);

                // remove everything after brackets - leaving (hopefully) only the name
                if (gi.Name.Contains("("))
                {
                    gi.Name = gi.Name.Substring(0, gi.Name.IndexOf("(")).Trim();
                }

                // remove special characters like dashes
                gi.Name = gi.Name.Replace("-", "").Trim();

                // get rom data
                ri.Name = Path.GetFileName(GameFileImportPath);
                ri.Md5 = hash.md5hash;
                ri.Sha1 = hash.sha1hash;
                ri.Crc = hash.crc32hash;
                ri.Size = ImageSize;
                ri.SignatureSource = gaseous_server.Models.Signatures_Games.RomItem.SignatureSourceType.None;

                return discoveredSignature;
            }
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
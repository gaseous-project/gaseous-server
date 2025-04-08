using System.Collections.Concurrent;
using System.Configuration;
using System.IO.Compression;
using System.Net;
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
        public gaseous_server.Models.Signatures_Games GetFileSignature(GameLibrary.LibraryItem library, Common.hashObject hash, FileInfo fi, string GameFileImportPath)
        {
            Logging.Log(Logging.LogType.Information, "Get Signature", "Getting signature for file: " + GameFileImportPath);
            gaseous_server.Models.Signatures_Games discoveredSignature = new gaseous_server.Models.Signatures_Games();
            discoveredSignature = _GetFileSignature(hash, fi.Name, fi.Extension, fi.Length, GameFileImportPath, false);

            string[] CompressionExts = { ".zip", ".rar", ".7z" };
            string ImportedFileExtension = Path.GetExtension(GameFileImportPath);

            if (CompressionExts.Contains(ImportedFileExtension) && (fi.Length < 1073741824))
            {
                // file is a zip and less than 1 GiB
                // extract the zip file and search the contents

                string ExtractPath = Path.Combine(Config.LibraryConfiguration.LibraryTempDirectory, library.Id.ToString(), Path.GetRandomFileName());
                Logging.Log(Logging.LogType.Information, "Get Signature", "Decompressing " + GameFileImportPath + " to " + ExtractPath + " examine contents");
                if (!Directory.Exists(ExtractPath)) { Directory.CreateDirectory(ExtractPath); }
                try
                {
                    switch (ImportedFileExtension)
                    {
                        case ".zip":
                            Logging.Log(Logging.LogType.Information, "Get Signature", "Decompressing using zip");
                            try
                            {
                                using (var archive = SharpCompress.Archives.Zip.ZipArchive.Open(GameFileImportPath))
                                {
                                    foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                                    {
                                        Logging.Log(Logging.LogType.Information, "Get Signature", "Extracting file: " + entry.Key);
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
                                Logging.Log(Logging.LogType.Warning, "Get Signature", "Unzip error", zipEx);
                                throw;
                            }
                            break;

                        case ".rar":
                            Logging.Log(Logging.LogType.Information, "Get Signature", "Decompressing using rar");
                            try
                            {
                                using (var archive = RarArchive.Open(GameFileImportPath))
                                {
                                    foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                                    {
                                        Logging.Log(Logging.LogType.Information, "Get Signature", "Extracting file: " + entry.Key);
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
                                Logging.Log(Logging.LogType.Warning, "Get Signature", "Unrar error", zipEx);
                                throw;
                            }
                            break;

                        case ".7z":
                            Logging.Log(Logging.LogType.Information, "Get Signature", "Decompressing using 7z");
                            try
                            {
                                using (var archive = SharpCompress.Archives.SevenZip.SevenZipArchive.Open(GameFileImportPath))
                                {
                                    foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                                    {
                                        Logging.Log(Logging.LogType.Information, "Get Signature", "Extracting file: " + entry.Key);
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
                                Logging.Log(Logging.LogType.Warning, "Get Signature", "7z error", zipEx);
                                throw;
                            }
                            break;
                    }

                    Logging.Log(Logging.LogType.Information, "Get Signature", "Processing decompressed files for signature matches");
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
                            Common.hashObject zhash = new Common.hashObject(file);

                            Logging.Log(Logging.LogType.Information, "Get Signature", "Checking signature of decompressed file " + file);

                            if (zfi != null)
                            {
                                if (signatureFound == false)
                                {
                                    gaseous_server.Models.Signatures_Games zDiscoveredSignature = _GetFileSignature(zhash, zfi.Name, zfi.Extension, zfi.Length, file, true);
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
                    Logging.Log(Logging.LogType.Critical, "Get Signature", "Error processing compressed file: " + GameFileImportPath, ex);
                }
            }

            // get discovered platform
            Platform? determinedPlatform = null;
            if (library.DefaultPlatformId == null || library.DefaultPlatformId == 0)
            {
                determinedPlatform = Metadata.Platforms.GetPlatform((long)discoveredSignature.Flags.PlatformId);
                if (determinedPlatform == null)
                {
                    determinedPlatform = new Platform();
                }
            }
            else
            {
                determinedPlatform = Metadata.Platforms.GetPlatform((long)library.DefaultPlatformId);
                discoveredSignature.MetadataSources.AddPlatform((long)determinedPlatform.Id, determinedPlatform.Name, HasheousClient.Models.MetadataSources.None);
            }

            // get discovered game
            if (discoveredSignature.Flags.GameId == 0)
            {
                discoveredSignature.MetadataSources.AddGame(0, discoveredSignature.Game.Name, HasheousClient.Models.MetadataSources.None);
            }

            return discoveredSignature;
        }

        private gaseous_server.Models.Signatures_Games _GetFileSignature(Common.hashObject hash, string ImageName, string ImageExtension, long ImageSize, string GameFileImportPath, bool IsInZip)
        {
            Logging.Log(Logging.LogType.Information, "Import Game", "Checking signature for file: " + GameFileImportPath + "\nMD5 hash: " + hash.md5hash + "\nSHA1 hash: " + hash.sha1hash);


            gaseous_server.Models.Signatures_Games? discoveredSignature = null;

            // begin signature search
            switch (Config.MetadataConfiguration.SignatureSource)
            {
                case HasheousClient.Models.MetadataModel.SignatureSources.LocalOnly:
                    Logging.Log(Logging.LogType.Information, "Import Game", "Hasheous disabled - searching local database only");

                    discoveredSignature = _GetFileSignatureFromDatabase(hash, ImageName, ImageExtension, ImageSize, GameFileImportPath);

                    break;

                case HasheousClient.Models.MetadataModel.SignatureSources.Hasheous:
                    Logging.Log(Logging.LogType.Information, "Import Game", "Hasheous enabled - searching Hashesous and then local database if not found");

                    discoveredSignature = _GetFileSignatureFromHasheous(hash, ImageName, ImageExtension, ImageSize, GameFileImportPath);

                    if (discoveredSignature == null)
                    {
                        Logging.Log(Logging.LogType.Information, "Import Game", "Signature not found in Hasheous - checking local database");

                        discoveredSignature = _GetFileSignatureFromDatabase(hash, ImageName, ImageExtension, ImageSize, GameFileImportPath);
                    }
                    else
                    {
                        Logging.Log(Logging.LogType.Information, "Import Game", "Signature retrieved from Hasheous for game: " + discoveredSignature.Game.Name);
                    }
                    break;

            }

            if (discoveredSignature == null)
            {
                // construct a signature from file data
                Logging.Log(Logging.LogType.Information, "Import Game", "Signature not found in local database or Hasheous (if enabled) - generating from file data");

                discoveredSignature = _GetFileSignatureFromFileData(hash, ImageName, ImageExtension, ImageSize, GameFileImportPath);

                Logging.Log(Logging.LogType.Information, "Import Game", "Signature generated from provided file for game: " + discoveredSignature.Game.Name);
            }

            gaseous_server.Models.PlatformMapping.GetIGDBPlatformMapping(ref discoveredSignature, ImageExtension, false);

            Logging.Log(Logging.LogType.Information, "Import Game", "  Determined import file as: " + discoveredSignature.Game.Name + " (" + discoveredSignature.Game.Year + ") " + discoveredSignature.Game.System);
            Logging.Log(Logging.LogType.Information, "Import Game", "  Platform determined to be: " + discoveredSignature.Flags.PlatformName + " (" + discoveredSignature.Flags.PlatformId + ")");

            return discoveredSignature;
        }

        private gaseous_server.Models.Signatures_Games? _GetFileSignatureFromDatabase(Common.hashObject hash, string ImageName, string ImageExtension, long ImageSize, string GameFileImportPath)
        {
            Logging.Log(Logging.LogType.Information, "Get Signature", "Checking local database for MD5: " + hash.md5hash);

            // check 1: do we have a signature for it?
            gaseous_server.Classes.SignatureManagement sc = new SignatureManagement();
            List<gaseous_server.Models.Signatures_Games> signatures = sc.GetSignature(hash.md5hash);
            if (signatures == null || signatures.Count == 0)
            {
                Logging.Log(Logging.LogType.Information, "Get Signature", "Checking local database for SHA1: " + hash.sha1hash);

                // no md5 signature found - try sha1
                signatures = sc.GetSignature("", hash.sha1hash);
            }

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

        private gaseous_server.Models.Signatures_Games? _GetFileSignatureFromHasheous(Common.hashObject hash, string ImageName, string ImageExtension, long ImageSize, string GameFileImportPath)
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
                    string cacheFileName = hash.md5hash + "_" + hash.sha1hash + ".json";
                    string cacheFilePath = Path.Combine(Config.LibraryConfiguration.LibraryMetadataDirectory_Hasheous(), cacheFileName);
                    // use cache file if it exists and is less than 30 days old, otherwise fetch from hasheous. if the fetch from hasheous is successful, save it to the cache, if it fails, use the cache if it exists even if it's old
                    if (File.Exists(cacheFilePath))
                    {
                        FileInfo cacheFile = new FileInfo(cacheFilePath);
                        if (cacheFile.LastWriteTimeUtc > DateTime.UtcNow.AddDays(-30))
                        {
                            Logging.Log(Logging.LogType.Information, "Get Signature", "Using cached signature from Hasheous");
                            HasheousResult = Newtonsoft.Json.JsonConvert.DeserializeObject<HasheousClient.Models.LookupItemModel>(File.ReadAllText(cacheFilePath));
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
                                SHA1 = hash.sha1hash
                            }, false);

                            if (HasheousResult != null)
                            {
                                // save to cache
                                File.WriteAllText(cacheFilePath, Newtonsoft.Json.JsonConvert.SerializeObject(HasheousResult));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (File.Exists(cacheFilePath))
                        {
                            Logging.Log(Logging.LogType.Warning, "Get Signature", "Error retrieving signature from Hasheous - using cached signature", ex);
                            HasheousResult = Newtonsoft.Json.JsonConvert.DeserializeObject<HasheousClient.Models.LookupItemModel>(File.ReadAllText(cacheFilePath));
                        }
                        else
                        {
                            Logging.Log(Logging.LogType.Warning, "Get Signature", "Error retrieving signature from Hasheous", ex);
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
                                        // only IGDB metadata is supported
                                        if (metadataResult.Source == HasheousClient.Models.MetadataSources.IGDB)
                                        {
                                            if (metadataResult.ImmutableId.Length > 0)
                                            {
                                                // use immutable id
                                                Platform hasheousPlatform = Platforms.GetPlatform(long.Parse(metadataResult.ImmutableId));
                                                signature.MetadataSources.AddPlatform((long)hasheousPlatform.Id, hasheousPlatform.Name, metadataResult.Source);
                                            }
                                            else if (metadataResult.Id.Length > 0)
                                            {
                                                // fall back to id
                                                Platform hasheousPlatform = Platforms.GetPlatform(metadataResult.Id);
                                                signature.MetadataSources.AddPlatform((long)hasheousPlatform.Id, hasheousPlatform.Name, metadataResult.Source);
                                            }
                                            else
                                            {
                                                // no id or immutable id - use unknown platform
                                                signature.MetadataSources.AddPlatform(0, "Unknown Platform", HasheousClient.Models.MetadataSources.None);
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
                                        if (metadataResult.ImmutableId.Length > 0)
                                        {
                                            signature.MetadataSources.AddGame(long.Parse(metadataResult.ImmutableId), HasheousResult.Name, metadataResult.Source);
                                        }
                                        else if (metadataResult.Id.Length > 0)
                                        {
                                            switch (metadataResult.Source)
                                            {
                                                case HasheousClient.Models.MetadataSources.IGDB:
                                                    gaseous_server.Models.Game hasheousGame = Games.GetGame(HasheousClient.Models.MetadataSources.IGDB, metadataResult.Id);
                                                    signature.MetadataSources.AddGame((long)hasheousGame.Id, hasheousGame.Name, metadataResult.Source);
                                                    break;

                                                default:
                                                    if (long.TryParse(metadataResult.Id, out long id) == true)
                                                    {
                                                        signature.MetadataSources.AddGame(id, HasheousResult.Name, metadataResult.Source);
                                                    }
                                                    else
                                                    {
                                                        signature.MetadataSources.AddGame(0, "Unknown Game", HasheousClient.Models.MetadataSources.None);
                                                    }
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            // no id or immutable id - use unknown game
                                            signature.MetadataSources.AddGame(0, "Unknown Game", HasheousClient.Models.MetadataSources.None);
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
                                Logging.Log(Logging.LogType.Information, "Get Signature", "No signature found in Hasheous");
                            }
                            else
                            {
                                Logging.Log(Logging.LogType.Warning, "Get Signature", "Error retrieving signature from Hasheous", ex);
                                throw;
                            }
                        }
                        else
                        {
                            Logging.Log(Logging.LogType.Warning, "Get Signature", "Error retrieving signature from Hasheous", ex);
                            throw;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.Log(Logging.LogType.Warning, "Get Signature", "Error retrieving signature from Hasheous", ex);
                    throw;
                }
            }

            return null;
        }

        private gaseous_server.Models.Signatures_Games _GetFileSignatureFromFileData(Common.hashObject hash, string ImageName, string ImageExtension, long ImageSize, string GameFileImportPath)
        {
            SignatureManagement signatureManagement = new SignatureManagement();

            gaseous_server.Models.Signatures_Games discoveredSignature = new gaseous_server.Models.Signatures_Games();

            // no signature match found - try name search
            List<gaseous_server.Models.Signatures_Games> signatures = signatureManagement.GetByTosecName(ImageName);

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
            public bool isSignatureSelector { get; set; } = false;
        }
    }
}
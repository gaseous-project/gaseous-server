using System.IO.Compression;
using HasheousClient.Models;

namespace gaseous_server.Classes
{
    public class FileSignature
    {
        public static gaseous_server.Models.Signatures_Games GetFileSignature(Common.hashObject hash, FileInfo fi, string GameFileImportPath)
        {
            gaseous_server.Models.Signatures_Games discoveredSignature = new gaseous_server.Models.Signatures_Games();
            discoveredSignature = _GetFileSignature(hash, fi, GameFileImportPath, false);

            if ((Path.GetExtension(GameFileImportPath) == ".zip") && (fi.Length < 1073741824))
            {
                // file is a zip and less than 1 GiB
                // extract the zip file and search the contents
                string ExtractPath = Path.Combine(Config.LibraryConfiguration.LibraryTempDirectory, Path.GetRandomFileName());
                if (!Directory.Exists(ExtractPath)) { Directory.CreateDirectory(ExtractPath); }
                try
                {
                    ZipFile.ExtractToDirectory(GameFileImportPath, ExtractPath);

                    // loop through contents until we find the first signature match
                    foreach (string file in Directory.GetFiles(ExtractPath, "*.*", SearchOption.AllDirectories))
                    {
                        FileInfo zfi = new FileInfo(file);
                        Common.hashObject zhash = new Common.hashObject(file);

                        gaseous_server.Models.Signatures_Games zDiscoveredSignature = _GetFileSignature(zhash, zfi, file, true);
                        zDiscoveredSignature.Rom.Name = Path.ChangeExtension(zDiscoveredSignature.Rom.Name, ".zip");

                        if (zDiscoveredSignature.Score > discoveredSignature.Score)
                        {
                            if (
                                zDiscoveredSignature.Rom.SignatureSource == gaseous_server.Models.Signatures_Games.RomItem.SignatureSourceType.MAMEArcade || 
                                zDiscoveredSignature.Rom.SignatureSource == gaseous_server.Models.Signatures_Games.RomItem.SignatureSourceType.MAMEMess
                            )
                            {
                                zDiscoveredSignature.Rom.Name = zDiscoveredSignature.Game.Description + ".zip";
                            }
                            zDiscoveredSignature.Rom.Crc = discoveredSignature.Rom.Crc;
                            zDiscoveredSignature.Rom.Md5 = discoveredSignature.Rom.Md5;
                            zDiscoveredSignature.Rom.Sha1 = discoveredSignature.Rom.Sha1;
                            zDiscoveredSignature.Rom.Size = discoveredSignature.Rom.Size;
                            discoveredSignature = zDiscoveredSignature;

                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.Log(Logging.LogType.Critical, "Get Signature", "Error processing zip file: " + GameFileImportPath, ex);
                }

                if (Directory.Exists(ExtractPath)) { Directory.Delete(ExtractPath, true); }
            }

            return discoveredSignature;
        }

        private static gaseous_server.Models.Signatures_Games _GetFileSignature(Common.hashObject hash, FileInfo fi, string GameFileImportPath, bool IsInZip)
        {
            gaseous_server.Models.Signatures_Games discoveredSignature = new gaseous_server.Models.Signatures_Games();

            // do database search first
            gaseous_server.Models.Signatures_Games? dbSignature = _GetFileSignatureFromDatabase(hash, fi, GameFileImportPath);

            if (dbSignature != null)
            {
                // local signature found
                discoveredSignature = dbSignature;
            }
            else
            {
                // no local signature attempt to pull from Hasheous
                dbSignature = _GetFileSignatureFromHasheous(hash, fi, GameFileImportPath);

                if (dbSignature != null)
                {
                    // signature retrieved from Hasheous
                    discoveredSignature = dbSignature;
                }
                else
                {
                    // construct a signature from file data
                    dbSignature = _GetFileSignatureFromFileData(hash, fi, GameFileImportPath);
                    discoveredSignature = dbSignature;
                }
            }

            Logging.Log(Logging.LogType.Information, "Import Game", "  Determined import file as: " + discoveredSignature.Game.Name + " (" + discoveredSignature.Game.Year + ") " + discoveredSignature.Game.System);

            return discoveredSignature;
        }

		private static gaseous_server.Models.Signatures_Games? _GetFileSignatureFromDatabase(Common.hashObject hash, FileInfo fi, string GameFileImportPath)
		{
            // check 1: do we have a signature for it?
            gaseous_server.Classes.SignatureManagement sc = new SignatureManagement();
            List<gaseous_server.Models.Signatures_Games> signatures = sc.GetSignature(hash.md5hash);
            if (signatures.Count == 0)
            {
                // no md5 signature found - try sha1
                signatures = sc.GetSignature("", hash.sha1hash);
            }

            gaseous_server.Models.Signatures_Games? discoveredSignature = null;
            if (signatures.Count == 1)
            {
                // only 1 signature found!
                discoveredSignature = signatures.ElementAt(0);
                gaseous_server.Models.PlatformMapping.GetIGDBPlatformMapping(ref discoveredSignature, fi, false);

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
                        gaseous_server.Models.PlatformMapping.GetIGDBPlatformMapping(ref discoveredSignature, fi, false);
                    }
                }

                return discoveredSignature;
            }

            return null;
        }

        private static gaseous_server.Models.Signatures_Games? _GetFileSignatureFromHasheous(Common.hashObject hash, FileInfo fi, string GameFileImportPath)
        {
            // check if hasheous is enabled, and if so use it's signature database
            if (Config.MetadataConfiguration.SignatureSource == HasheousClient.Models.MetadataModel.SignatureSources.Hasheous)
            {
                SignatureLookupItem? HasheousResult = HasheousClient.Hasheous.RetrieveFromHasheousAsync(new HashLookupModel{
                    MD5 = hash.md5hash,
                    SHA1 = hash.sha1hash
                });

                if (HasheousResult != null)
                {
                    if (HasheousResult.Signature != null)
                    {
                        gaseous_server.Models.Signatures_Games signature = new Models.Signatures_Games();
                        signature.Game = HasheousResult.Signature.Game;
                        signature.Rom = HasheousResult.Signature.Rom;
                        
                        if (HasheousResult.MetadataResults != null)
                        {
                            if (HasheousResult.MetadataResults.Count > 0)
                            {
                                foreach (SignatureLookupItem.MetadataResult metadataResult in HasheousResult.MetadataResults)
                                {
                                    if (metadataResult.Source == MetadataModel.MetadataSources.IGDB)
                                    {
                                        signature.Flags.IGDBPlatformId = (long)metadataResult.PlatformId;
                                        signature.Flags.IGDBGameId = (long)metadataResult.GameId;
                                    }
                                }
                            }
                        }
    
                        gaseous_server.Models.PlatformMapping.GetIGDBPlatformMapping(ref signature, fi, false);

                        return signature;
                    }
                }
            }

            return null;
        }

        private static gaseous_server.Models.Signatures_Games _GetFileSignatureFromFileData(Common.hashObject hash, FileInfo fi, string GameFileImportPath)
        {
            SignatureManagement signatureManagement = new SignatureManagement();

            gaseous_server.Models.Signatures_Games discoveredSignature = new gaseous_server.Models.Signatures_Games();

            // no signature match found - try name search
            List<gaseous_server.Models.Signatures_Games> signatures = signatureManagement.GetByTosecName(fi.Name);

            if (signatures.Count == 1)
            {
                // only 1 signature found!
                discoveredSignature = signatures.ElementAt(0);
                gaseous_server.Models.PlatformMapping.GetIGDBPlatformMapping(ref discoveredSignature, fi, false);

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
                        gaseous_server.Models.PlatformMapping.GetIGDBPlatformMapping(ref discoveredSignature, fi, false);
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

                // guess platform
                gaseous_server.Models.PlatformMapping.GetIGDBPlatformMapping(ref discoveredSignature, fi, true);

                // get rom data
                ri.Name = Path.GetFileName(GameFileImportPath);
                ri.Md5 = hash.md5hash;
                ri.Sha1 = hash.sha1hash;
                ri.Size = fi.Length;
                ri.SignatureSource = gaseous_server.Models.Signatures_Games.RomItem.SignatureSourceType.None;

                return discoveredSignature;
            }
        }
    }
}
using gaseous_server.Models;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;
using gaseous_server.Classes.Metadata;
using static gaseous_server.Classes.FileSignature;
using Newtonsoft.Json;

namespace gaseous_server.Classes.Plugins.FileSignatures
{
    /// <summary>
    /// File signature plugin for Hasheous integration.
    /// </summary>
    public class Hasheous : IFileSignaturePlugin
    {
        private static HTTPComms comms = new HTTPComms();

        private static readonly JsonSerializerSettings HasheousJsonSerializerSettings = CreateHasheousJsonSerializerSettings();

        /// <inheritdoc/>
        public string Name { get; } = "Hasheous";

        /// <inheritdoc/>
        public Dictionary<string, object>? Settings { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public bool UsesInternet { get; } = true;

        private List<MetadataSources> SupportedMetadataSources = new List<MetadataSources>()
        {
            MetadataSources.IGDB,
            MetadataSources.TheGamesDb,
            MetadataSources.Hasheous
        };

        private static JsonSerializerSettings CreateHasheousJsonSerializerSettings()
        {
            JsonSerializerSettings serializerSettings = new JsonSerializerSettings();
            serializerSettings.Converters.Add(new UnknownEnumFallbackConverter());
            return serializerSettings;
        }

        private static HasheousClient.Models.LookupItemModel? DeserializeLookupItemModel(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonConvert.DeserializeObject<HasheousClient.Models.LookupItemModel>(json, HasheousJsonSerializerSettings);
        }

        /// <inheritdoc/>
        public async Task<Signatures_Games?> GetSignature(FileHash hash, string ImageName, string ImageExtension, long ImageSize, string GameFileImportPath)
        {
            // check if hasheous is enabled, and if so use it's signature database
            if (Config.MetadataConfiguration.SignatureSource == HasheousClient.Models.MetadataModel.SignatureSources.Hasheous)
            {
                HasheousClient.Models.LookupItemModel? HasheousResult = null;
                try
                {
                    try
                    {
                        if (HasheousResult == null)
                        {
                            var body = new List<HasheousClient.Models.HashLookupModel>();
                            if (hash.ArchiveContents == null || hash.ArchiveContents.Count == 0)
                            {
                                body.Add(new HasheousClient.Models.HashLookupModel
                                {
                                    MD5 = hash.Hash.md5hash,
                                    SHA1 = hash.Hash.sha1hash,
                                    SHA256 = hash.Hash.sha256hash,
                                    CRC = hash.Hash.crc32hash
                                });
                            }
                            else
                            {
                                foreach (ArchiveData archiveData in hash.MatchCandidates)
                                {
                                    body.Add(new HasheousClient.Models.HashLookupModel
                                    {
                                        MD5 = archiveData.Hash.md5hash,
                                        SHA1 = archiveData.Hash.sha1hash,
                                        SHA256 = archiveData.Hash.sha256hash,
                                        CRC = archiveData.Hash.crc32hash
                                    });
                                }
                            }

                            string sourceList = "";

                            HasheousResult = await SearchHasheous(sourceList, body);

                            if (HasheousResult == null)
                            {
                                // fall back to full archive search
                                body.Clear();
                                foreach (ArchiveData archiveData in hash.ArchiveContents)
                                {
                                    if (archiveData.Score == 0)
                                    {
                                        continue;
                                    }

                                    body.Add(new HasheousClient.Models.HashLookupModel
                                    {
                                        MD5 = archiveData.Hash.md5hash,
                                        SHA1 = archiveData.Hash.sha1hash,
                                        SHA256 = archiveData.Hash.sha256hash,
                                        CRC = archiveData.Hash.crc32hash
                                    });
                                }

                                if (body.Count > 0)
                                {
                                    foreach (HasheousClient.Models.HashLookupModel lookupModel in body)
                                    {
                                        HasheousResult = await SearchHasheous(sourceList, new List<HasheousClient.Models.HashLookupModel> { lookupModel });

                                        if (HasheousResult != null)
                                        {
                                            break;
                                        }
                                    }
                                }
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

                            // if (File.Exists(cacheFilePath))
                            // {
                            //     Logging.LogKey(Logging.LogType.Warning, "process.get_signature", "getsignature.error_retrieving_signature_from_hasheous_using_cached_signature", null, null, ex);
                            //     HasheousResult = DeserializeLookupItemModel(await File.ReadAllTextAsync(cacheFilePath));
                            // }
                            // else
                            // {
                            Logging.LogKey(Logging.LogType.Warning, "process.get_signature", "getsignature.error_retrieving_signature_from_hasheous", null, null, ex);
                            // }
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
                                                    // unresolvable immutableid - use unknown platform
                                                    signature.MetadataSources.AddPlatform(0, "Unknown Platform", MetadataSources.None);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            // add the Hasheous id
                            if (HasheousResult.Name != null)
                            {
                                signature.MetadataSources.AddGame(HasheousResult.Id, HasheousResult.Name, MetadataSources.Hasheous);
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
                                            if (!SupportedMetadataSources.Contains(metadataSource))
                                            {
                                                continue;
                                            }

                                            if (metadataResult.ImmutableId.Length > 0)
                                            {
                                                switch (metadataSource)
                                                {
                                                    case FileSignature.MetadataSources.IGDB:
                                                        // check if the immutable id is a long
                                                        if (metadataResult.ImmutableId.Length > 0 && long.TryParse(metadataResult.ImmutableId, out long immutableId) == true)
                                                        {
                                                            // use immutable id
                                                            gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes.Game hasheousGame = await Games.GetGame(FileSignature.MetadataSources.IGDB, immutableId);
                                                            signature.MetadataSources.AddGame((long)hasheousGame.Id, hasheousGame.Name, metadataSource);
                                                        }
                                                        else
                                                        {
                                                            // unresolvable immutable id - use unknown game
                                                            signature.MetadataSources.AddGame(0, "Unknown Game", FileSignature.MetadataSources.None);
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

        private async Task<HasheousClient.Models.LookupItemModel?> SearchHasheous(string sourceList, List<HasheousClient.Models.HashLookupModel>? body)
        {
            HasheousClient.Models.LookupItemModel? HasheousResult = null;

            Dictionary<string, string> headers = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(Config.MetadataConfiguration.HasheousAPIKey))
            {
                headers.Add("X-API-Key", Config.MetadataConfiguration.HasheousAPIKey);
            }
            headers.Add("X-Client-API-Key", Config.MetadataConfiguration.HasheousClientAPIKey);
            // headers.Add("CacheControl", "no-cache");
            // headers.Add("Pragma", "no-cache");

            var response = await comms.SendRequestAsync<string>(HTTPComms.HttpMethod.POST, new Uri("https://hasheous.org/api/v1/Lookup/ByHash" + sourceList), headers, body, contentType: "application/json", returnRawResponse: true);
            if (response != null && response.StatusCode == 200)
            {
                if (!string.IsNullOrWhiteSpace(response.Body) && response.Body != "The provided hash was not found in the signature database.")
                {
                    HasheousResult = DeserializeLookupItemModel(response.Body);
                }
            }

            return HasheousResult;
        }
    }
}
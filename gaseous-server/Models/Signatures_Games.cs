using System;
using System.Text.Json.Serialization;
using gaseous_signature_parser.models.RomSignatureObject;

namespace gaseous_server.Models
{
    public class Signatures_Games : HasheousClient.Models.LookupResponseModel
    {
        public Signatures_Games()
        {
        }

        public SignatureFlags Flags = new SignatureFlags();

        public class SignatureFlags
        {
            public long IGDBPlatformId { get; set; }
            public string IGDBPlatformName { get; set; }
            public long IGDBGameId { get; set; }
            public SignatureGenerationSource GenerationSource { get; set; }
            public enum SignatureGenerationSource
            {
                File,
                Database
            }
            public int SignatureScore
            {
                get
                {
                    int scoreSourceBooster = 0;
                    switch (GenerationSource)
                    {
                        case SignatureGenerationSource.File:
                            scoreSourceBooster = 0;
                            break;

                        case SignatureGenerationSource.Database:
                            scoreSourceBooster = 2;
                            break;

                        default:
                            scoreSourceBooster = 0;
                            break;
                    }

                    return scoreSourceBooster;
                }
            }
        }
    }
}

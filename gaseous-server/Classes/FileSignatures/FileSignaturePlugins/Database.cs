using gaseous_server.Models;

namespace gaseous_server.Classes.Plugins.FileSignatures
{
    /// <summary>
    /// Database file signature plugin for identifying game files using database lookups.
    /// </summary>
    public class Database : IFileSignaturePlugin
    {
        /// <inheritdoc/>
        public Dictionary<string, object>? Settings { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public async Task<Signatures_Games?> GetSignature(HashObject hash, string ImageName, string ImageExtension, long ImageSize, string GameFileImportPath)
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
    }
}
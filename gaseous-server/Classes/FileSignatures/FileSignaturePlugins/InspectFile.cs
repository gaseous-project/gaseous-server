using gaseous_server.Models;

namespace gaseous_server.Classes.Plugins.FileSignatures
{
    /// <summary>
    /// File signature plugin for identifying game files.
    /// </summary>
    public class InspectFile : IFileSignaturePlugin
    {
        /// <inheritdoc/>
        public string Name { get; } = "InspectFile";

        /// <inheritdoc/>
        public Dictionary<string, object>? Settings { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <inheritdoc/>
        public bool UsesInternet { get; } = false;

        /// <inheritdoc/>
        public async Task<Signatures_Games?> GetSignature(HashObject hash, string ImageName, string ImageExtension, long ImageSize, string GameFileImportPath)
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
    }
}
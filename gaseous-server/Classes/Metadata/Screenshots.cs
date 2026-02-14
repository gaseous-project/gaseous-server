using System;
using gaseous_server.Classes.Plugins.MetadataProviders.MetadataTypes;


namespace gaseous_server.Classes.Metadata
{
    public class Screenshots
    {
        public Screenshots()
        {
        }

        public static async Task<Screenshot?> GetScreenshotAsync(FileSignature.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Screenshot? RetVal = await Metadata.GetMetadataAsync<Screenshot>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}


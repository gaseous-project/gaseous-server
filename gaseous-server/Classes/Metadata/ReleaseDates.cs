using System;
using System.Threading.Tasks;
using HasheousClient.Models.Metadata.IGDB;


namespace gaseous_server.Classes.Metadata
{
    public class ReleaseDates
    {
        public ReleaseDates()
        {
        }

        public static async Task<ReleaseDate?> GetReleaseDates(HasheousClient.Models.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                ReleaseDate? RetVal = await Metadata.GetMetadataAsync<ReleaseDate>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}


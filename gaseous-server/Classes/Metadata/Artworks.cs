﻿using System;
using System.Threading.Tasks;
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Classes.Metadata
{
    public class Artworks
    {
        public Artworks()
        {
        }

        public static async Task<Artwork?> GetArtwork(HasheousClient.Models.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Artwork? RetVal = await Metadata.GetMetadataAsync<Artwork>(SourceType, (long)Id, false);
                return RetVal;
            }
        }
    }
}


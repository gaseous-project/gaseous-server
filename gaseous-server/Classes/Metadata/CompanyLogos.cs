﻿using System;
using System.Threading.Tasks;
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Classes.Metadata
{
    public class CompanyLogos
    {
        public CompanyLogos()
        {
        }

        public static async Task<CompanyLogo?> GetCompanyLogo(long? Id, string ImagePath)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                CompanyLogo? RetVal = await Metadata.GetMetadataAsync<CompanyLogo>((long)Id, false);
                return RetVal;
            }
        }
    }
}


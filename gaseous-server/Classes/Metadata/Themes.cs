﻿using System;
using HasheousClient.Models.Metadata.IGDB;


namespace gaseous_server.Classes.Metadata
{
    public class Themes
    {
        static List<ThemeItem> themeItemCache = new List<ThemeItem>();

        public Themes()
        {
        }

        public static async Task<Theme?> GetGame_ThemesAsync(HasheousClient.Models.MetadataSources SourceType, long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                // check cache for Theme
                if (themeItemCache.Find(x => x.Id == Id && x.SourceType == SourceType) != null)
                {
                    ThemeItem themeItem = themeItemCache.Find(x => x.Id == Id && x.SourceType == SourceType);

                    Theme? nTheme = new Theme
                    {
                        Id = themeItem.Id,
                        Name = themeItem.Name
                    };

                    return nTheme;
                }

                Theme? RetVal = await Metadata.GetMetadataAsync<Theme>(SourceType, (long)Id, false);

                if (RetVal != null)
                {
                    // add Theme to cache
                    if (themeItemCache.Find(x => x.Id == Id && x.SourceType == SourceType) == null)
                    {
                        ThemeItem themeItem = new ThemeItem();
                        themeItem.Id = (long)Id;
                        themeItem.SourceType = SourceType;
                        themeItem.Name = RetVal.Name;
                        themeItemCache.Add(themeItem);
                    }
                }

                return RetVal;
            }
        }
    }

    class ThemeItem
    {
        public long Id { get; set; }
        public HasheousClient.Models.MetadataSources SourceType { get; set; }
        public string Name { get; set; }
    }
}


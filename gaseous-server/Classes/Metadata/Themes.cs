using System;
using gaseous_tools;
using IGDB;
using IGDB.Models;
using static gaseous_tools.Config.ConfigFile;

namespace gaseous_server.Classes.Metadata
{
    public class Themes
    {
        const string fieldList = "fields checksum,created_at,name,slug,updated_at,url;";

        public Themes()
        {
        }

        private static IGDBClient igdb = new IGDBClient(
                    // Found in Twitch Developer portal for your app
                    Config.IGDB.ClientId,
                    Config.IGDB.Secret
                );

        public static Theme? GetGame_Themes(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<Theme> RetVal = _GetGame_Themes(SearchUsing.id, Id);
                return RetVal.Result;
            }
        }

        public static Theme GetGame_Themes(string Slug)
        {
            Task<Theme> RetVal = _GetGame_Themes(SearchUsing.slug, Slug);
            return RetVal.Result;
        }

        private static async Task<Theme> _GetGame_Themes(SearchUsing searchUsing, object searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("Theme", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("Theme", (string)searchValue);
            }

            // set up where clause
            string WhereClause = "";
            switch (searchUsing)
            {
                case SearchUsing.id:
                    WhereClause = "where id = " + searchValue;
                    break;
                case SearchUsing.slug:
                    WhereClause = "where slug = " + searchValue;
                    break;
                default:
                    throw new Exception("Invalid search type");
            }

            Theme returnValue = new Theme();
            bool forceImageDownload = false;
            switch (cacheStatus)
            {
                case Storage.CacheStatus.NotPresent:
                    returnValue = await GetObjectFromServer(WhereClause);
                    Storage.NewCacheValue(returnValue);
                    forceImageDownload = true;
                    break;
                case Storage.CacheStatus.Expired:
                    try
                    {
                        returnValue = await GetObjectFromServer(WhereClause);
                        Storage.NewCacheValue(returnValue, true);
                        return returnValue;
                    }
                    catch (Exception ex)
                    {
                        gaseous_tools.Logging.Log(gaseous_tools.Logging.LogType.Warning, "Metadata: " + returnValue.GetType().Name, "An error occurred while connecting to IGDB. WhereClause: " + WhereClause, ex);
                        return Storage.GetCacheValue<Theme>(returnValue, "id", (long)searchValue);
                    }
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<Theme>(returnValue, "id", (long)searchValue);
                    break;
                default:
                    throw new Exception("How did you get here?");
            }

            return returnValue;
        }

        private enum SearchUsing
        {
            id,
            slug
        }

        private static async Task<Theme> GetObjectFromServer(string WhereClause)
        {
            // get Game_Themes metadata
            var results = await igdb.QueryAsync<Theme>(IGDBClient.Endpoints.Themes, query: fieldList + " " + WhereClause + ";");
            var result = results.First();

            return result;
        }
    }
}


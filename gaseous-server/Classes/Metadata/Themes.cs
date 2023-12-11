using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
    public class Themes
    {
        const string fieldList = "fields checksum,created_at,name,slug,updated_at,url;";

        public Themes()
        {
        }

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
                        Logging.Log(Logging.LogType.Warning, "Metadata: " + returnValue.GetType().Name, "An error occurred while connecting to IGDB. WhereClause: " + WhereClause, ex);
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
            Communications comms = new Communications();
            var results = await comms.APIComm<Theme>(IGDBClient.Endpoints.Themes, fieldList, WhereClause);
            var result = results.First();

            return result;
        }
    }
}


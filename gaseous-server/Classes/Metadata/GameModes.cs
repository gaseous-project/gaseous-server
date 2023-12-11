using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
    public class GameModes
    {
        const string fieldList = "fields checksum,created_at,name,slug,updated_at,url;";

        public GameModes()
        {
        }

        public static GameMode? GetGame_Modes(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<GameMode> RetVal = _GetGame_Modes(SearchUsing.id, Id);
                return RetVal.Result;
            }
        }

        public static GameMode GetGame_Modes(string Slug)
        {
            Task<GameMode> RetVal = _GetGame_Modes(SearchUsing.slug, Slug);
            return RetVal.Result;
        }

        private static async Task<GameMode> _GetGame_Modes(SearchUsing searchUsing, object searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("GameMode", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("GameMode", (string)searchValue);
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

            GameMode returnValue = new GameMode();
            switch (cacheStatus)
            {
                case Storage.CacheStatus.NotPresent:
                    returnValue = await GetObjectFromServer(WhereClause);
                    Storage.NewCacheValue(returnValue);
                    break;
                case Storage.CacheStatus.Expired:
                    try
                    {
                        returnValue = await GetObjectFromServer(WhereClause);
                        Storage.NewCacheValue(returnValue, true);
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(Logging.LogType.Warning, "Metadata: " + returnValue.GetType().Name, "An error occurred while connecting to IGDB. WhereClause: " + WhereClause, ex);
                        returnValue = Storage.GetCacheValue<GameMode>(returnValue, "id", (long)searchValue);
                    }
                    break;
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<GameMode>(returnValue, "id", (long)searchValue);
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

        private static async Task<GameMode> GetObjectFromServer(string WhereClause)
        {
            // get Game_Modes metadata
            var results = await Communications.APIComm<GameMode>(IGDBClient.Endpoints.GameModes, fieldList, WhereClause);
            var result = results.First();

            return result;
        }
    }
}


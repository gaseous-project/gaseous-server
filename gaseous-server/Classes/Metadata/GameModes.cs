using System;
using gaseous_tools;
using IGDB;
using IGDB.Models;
using MySqlX.XDevAPI.Common;
using static gaseous_tools.Config.ConfigFile;

namespace gaseous_server.Classes.Metadata
{
    public class GameModes
    {
        const string fieldList = "fields checksum,created_at,name,slug,updated_at,url;";

        public GameModes()
        {
        }

        private static IGDBClient igdb = new IGDBClient(
                    // Found in Twitch Developer portal for your app
                    Config.IGDB.ClientId,
                    Config.IGDB.Secret
                );

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
            bool forceImageDownload = false;
            switch (cacheStatus)
            {
                case Storage.CacheStatus.NotPresent:
                    returnValue = await GetObjectFromServer(WhereClause);
                    Storage.NewCacheValue(returnValue);
                    forceImageDownload = true;
                    break;
                case Storage.CacheStatus.Expired:
                    returnValue = await GetObjectFromServer(WhereClause);
                    Storage.NewCacheValue(returnValue, true);
                    forceImageDownload = true;
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
            var results = await igdb.QueryAsync<GameMode>(IGDBClient.Endpoints.GameModes, query: fieldList + " " + WhereClause + ";");
            var result = results.First();

            return result;
        }
    }
}


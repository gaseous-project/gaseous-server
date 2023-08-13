using System;
using gaseous_tools;
using IGDB;
using IGDB.Models;
using MySqlX.XDevAPI.Common;
using static gaseous_tools.Config.ConfigFile;

namespace gaseous_server.Classes.Metadata
{
    public class MultiplayerModes
    {
        const string fieldList = "fields campaigncoop,checksum,dropin,game,lancoop,offlinecoop,offlinecoopmax,offlinemax,onlinecoop,onlinecoopmax,onlinemax,platform,splitscreen,splitscreenonline;";

        public MultiplayerModes()
        {
        }

        private static IGDBClient igdb = new IGDBClient(
                    // Found in Twitch Developer portal for your app
                    Config.IGDB.ClientId,
                    Config.IGDB.Secret
                );

        public static MultiplayerMode? GetGame_MultiplayerModes(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<MultiplayerMode> RetVal = _GetGame_MultiplayerModes(SearchUsing.id, Id);
                return RetVal.Result;
            }
        }

        public static MultiplayerMode GetGame_MultiplayerModes(string Slug)
        {
            Task<MultiplayerMode> RetVal = _GetGame_MultiplayerModes(SearchUsing.slug, Slug);
            return RetVal.Result;
        }

        private static async Task<MultiplayerMode> _GetGame_MultiplayerModes(SearchUsing searchUsing, object searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("MultiplayerMode", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("MultiplayerMode", (string)searchValue);
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

            MultiplayerMode returnValue = new MultiplayerMode();
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
                    returnValue = Storage.GetCacheValue<MultiplayerMode>(returnValue, "id", (long)searchValue);
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

        private static async Task<MultiplayerMode> GetObjectFromServer(string WhereClause)
        {
            // get Game_MultiplayerModes metadata
            var results = await igdb.QueryAsync<MultiplayerMode>(IGDBClient.Endpoints.MultiplayerModes, query: fieldList + " " + WhereClause + ";");
            var result = results.First();

            return result;
        }
    }
}


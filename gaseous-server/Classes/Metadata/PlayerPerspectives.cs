using System;
using gaseous_tools;
using IGDB;
using IGDB.Models;
using MySqlX.XDevAPI.Common;
using static gaseous_tools.Config.ConfigFile;

namespace gaseous_server.Classes.Metadata
{
    public class PlayerPerspectives
    {
        const string fieldList = "fields checksum,created_at,name,slug,updated_at,url;";

        public PlayerPerspectives()
        {
        }

        private static IGDBClient igdb = new IGDBClient(
                    // Found in Twitch Developer portal for your app
                    Config.IGDB.ClientId,
                    Config.IGDB.Secret
                );

        public static PlayerPerspective? GetGame_PlayerPerspectives(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<PlayerPerspective> RetVal = _GetGame_PlayerPerspectives(SearchUsing.id, Id);
                return RetVal.Result;
            }
        }

        public static PlayerPerspective GetGame_PlayerPerspectives(string Slug)
        {
            Task<PlayerPerspective> RetVal = _GetGame_PlayerPerspectives(SearchUsing.slug, Slug);
            return RetVal.Result;
        }

        private static async Task<PlayerPerspective> _GetGame_PlayerPerspectives(SearchUsing searchUsing, object searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("PlayerPerspective", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("PlayerPerspective", (string)searchValue);
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

            PlayerPerspective returnValue = new PlayerPerspective();
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
                    }
                    catch (Exception ex)
                    {
                        gaseous_tools.Logging.Log(gaseous_tools.Logging.LogType.Warning, "Metadata: " + returnValue.GetType().Name, "An error occurred while connecting to IGDB. WhereClause: " + WhereClause, ex);
                        returnValue = Storage.GetCacheValue<PlayerPerspective>(returnValue, "id", (long)searchValue);
                    }
                    break;
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<PlayerPerspective>(returnValue, "id", (long)searchValue);
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

        private static async Task<PlayerPerspective> GetObjectFromServer(string WhereClause)
        {
            // get Game_PlayerPerspectives metadata
            var results = await igdb.QueryAsync<PlayerPerspective>(IGDBClient.Endpoints.PlayerPerspectives, query: fieldList + " " + WhereClause + ";");
            var result = results.First();

            return result;
        }
    }
}


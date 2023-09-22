using System;
using gaseous_tools;
using IGDB;
using IGDB.Models;
using MySqlX.XDevAPI.Common;
using static gaseous_tools.Config.ConfigFile;

namespace gaseous_server.Classes.Metadata
{
	public class GamesVideos
    {
        const string fieldList = "fields checksum,game,name,video_id;";

        public GamesVideos()
        {
        }

        private static IGDBClient igdb = new IGDBClient(
                    // Found in Twitch Developer portal for your app
                    Config.IGDB.ClientId,
                    Config.IGDB.Secret
                );

        public static GameVideo? GetGame_Videos(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<GameVideo> RetVal = _GetGame_Videos(SearchUsing.id, Id);
                return RetVal.Result;
            }
        }

        public static GameVideo GetGame_Videos(string Slug)
        {
            Task<GameVideo> RetVal = _GetGame_Videos(SearchUsing.slug, Slug);
            return RetVal.Result;
        }

        private static async Task<GameVideo> _GetGame_Videos(SearchUsing searchUsing, object searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("GameVideo", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("GameVideo", (string)searchValue);
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

            GameVideo returnValue = new GameVideo();
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
                        gaseous_tools.Logging.Log(gaseous_tools.Logging.LogType.Warning, "Metadata: " + returnValue.GetType().Name, "An error occurred while connecting to IGDB. WhereClause: " + WhereClause, ex);
                        returnValue = Storage.GetCacheValue<GameVideo>(returnValue, "id", (long)searchValue);
                    }
                    break;
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<GameVideo>(returnValue, "id", (long)searchValue);
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

        private static async Task<GameVideo> GetObjectFromServer(string WhereClause)
        {
            // get Game_Videos metadata
            var results = await igdb.QueryAsync<GameVideo>(IGDBClient.Endpoints.GameVideos, query: fieldList + " " + WhereClause + ";");
            var result = results.First();

            return result;
        }
	}
}


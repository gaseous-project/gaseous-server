﻿using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
	public class ExternalGames
    {
        const string fieldList = "fields category,checksum,countries,created_at,game,media,name,platform,uid,updated_at,url,year;";

        public ExternalGames()
        {
        }

        public static ExternalGame? GetExternalGames(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<ExternalGame> RetVal = _GetExternalGames(SearchUsing.id, Id);
                return RetVal.Result;
            }
        }

        public static ExternalGame GetExternalGames(string Slug)
        {
            Task<ExternalGame> RetVal = _GetExternalGames(SearchUsing.slug, Slug);
            return RetVal.Result;
        }

        private static async Task<ExternalGame> _GetExternalGames(SearchUsing searchUsing, object searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("ExternalGame", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("ExternalGame", (string)searchValue);
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

            ExternalGame returnValue = new ExternalGame();
            switch (cacheStatus)
            {
                case Storage.CacheStatus.NotPresent:
                    returnValue = await GetObjectFromServer(WhereClause);
                    if (returnValue != null)
                    {
                        Storage.NewCacheValue(returnValue);
                    }
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
                        returnValue = Storage.GetCacheValue<ExternalGame>(returnValue, "id", (long)searchValue);
                    }
                    break;
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<ExternalGame>(returnValue, "id", (long)searchValue);
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

        private static async Task<ExternalGame?> GetObjectFromServer(string WhereClause)
        {
            // get ExternalGames metadata
            Communications comms = new Communications();
            var results = await comms.APIComm<ExternalGame>(IGDBClient.Endpoints.ExternalGames, fieldList, WhereClause);
            if (results.Length > 0)
            {
                var result = results.First();

                return result;
            }
            else
            {
                return null;
            }
        }
	}
}


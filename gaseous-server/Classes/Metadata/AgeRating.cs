﻿using System;
using gaseous_tools;
using IGDB;
using IGDB.Models;
using MySqlX.XDevAPI.Common;
using static gaseous_tools.Config.ConfigFile;

namespace gaseous_server.Classes.Metadata
{
	public class AgeRatings
    {
        const string fieldList = "fields category,checksum,content_descriptions,rating,rating_cover_url,synopsis;";

        public AgeRatings()
        {
        }

        private static IGDBClient igdb = new IGDBClient(
                    // Found in Twitch Developer portal for your app
                    Config.IGDB.ClientId,
                    Config.IGDB.Secret
                );

        public static AgeRating? GetAgeRatings(long? Id)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<AgeRating> RetVal = _GetAgeRatings(SearchUsing.id, Id);
                return RetVal.Result;
            }
        }

        public static AgeRating GetAgeRatings(string Slug)
        {
            Task<AgeRating> RetVal = _GetAgeRatings(SearchUsing.slug, Slug);
            return RetVal.Result;
        }

        private static async Task<AgeRating> _GetAgeRatings(SearchUsing searchUsing, object searchValue)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("AgeRating", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("AgeRating", (string)searchValue);
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

            AgeRating returnValue = new AgeRating();
            switch (cacheStatus)
            {
                case Storage.CacheStatus.NotPresent:
                    returnValue = await GetObjectFromServer(WhereClause);
                    Storage.NewCacheValue(returnValue);
                    UpdateSubClasses(returnValue);
                    break;  
                case Storage.CacheStatus.Expired:
                    returnValue = await GetObjectFromServer(WhereClause);
                    Storage.NewCacheValue(returnValue, true);
                    UpdateSubClasses(returnValue);
                    break;  
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<AgeRating>(returnValue, "id", (long)searchValue);
                    break;
                default:
                    throw new Exception("How did you get here?");
            }

            return returnValue;
        }

        private static void UpdateSubClasses(AgeRating ageRating)
        {
            if (ageRating.ContentDescriptions != null)
            {
                foreach (long AgeRatingContentDescriptionId in ageRating.ContentDescriptions.Ids)
                {
                    AgeRatingContentDescription ageRatingContentDescription = AgeRatingContentDescriptions.GetAgeRatingContentDescriptions(AgeRatingContentDescriptionId);
                }
            }
        }

        private enum SearchUsing
        {
            id,
            slug
        }

        private static async Task<AgeRating> GetObjectFromServer(string WhereClause)
        {
            // get AgeRatings metadata
            var results = await igdb.QueryAsync<AgeRating>(IGDBClient.Endpoints.AgeRating, query: fieldList + " " + WhereClause + ";");
            var result = results.First();

            return result;
        }
	}
}


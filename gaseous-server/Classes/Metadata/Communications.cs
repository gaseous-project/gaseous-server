using System;
using System.Data;
using System.Net;
using IGDB;
using IGDB.Models;
using RestEase;

namespace gaseous_server.Classes.Metadata
{
    public class Communications
    {
        private static IGDBClient igdb = new IGDBClient(
                    // Found in Twitch Developer portal for your app
                    Config.IGDB.ClientId,
                    Config.IGDB.Secret
                );

        private int RateLimitAvoidanceWait = 1000;
        private int RateLimitWaitTime = 10000;
        private DateTime RateLimitResumeTime = DateTime.UtcNow.AddMinutes(5 * -1);

        private int RetryAttempts = 0;
        private int RetryAttemptsMax = 3;

        public enum MetadataSources
        {
            None,
            IGDB
        }

        public async Task<T[]?> APIComm<T>(string Endpoint, string fieldList, string WhereClause)
        {
            switch (Config.MetadataConfiguration.Source)
            {
                case MetadataSources.None:
                    return null;
                case MetadataSources.IGDB:
                    return await IGDBAPI<T>(Endpoint, fieldList, WhereClause);
                default:
                    return null;
            }
        }

        public async Task<T[]> IGDBAPI<T>(string Endpoint, string fieldList, string WhereClause)
        {
            Logging.Log(Logging.LogType.Debug, "API Connection", "Accessing API for endpoint: " + Endpoint);

            if (RateLimitResumeTime > DateTime.UtcNow)
            {
                Logging.Log(Logging.LogType.Information, "API Connection", "IGDB rate limit hit. Pausing API communications until " + RateLimitResumeTime.ToString() + ". Attempt " + RetryAttempts + " of " + RetryAttemptsMax + " retries.");
                Thread.Sleep(RateLimitWaitTime);
            }

            try
            {   
                // sleep for a moment to help avoid hitting the rate limiter
                Thread.Sleep(RateLimitAvoidanceWait);
                var results = await igdb.QueryAsync<T>(Endpoint, query: fieldList + " " + WhereClause + ";");
                
                return results;
            }
            catch (ApiException apiEx)
            {
                switch (apiEx.StatusCode)
                {
                    case HttpStatusCode.TooManyRequests:
                        if (RetryAttempts >= RetryAttemptsMax)
                        {
                            Logging.Log(Logging.LogType.Warning, "API Connection", "IGDB rate limiter attempts expired. Aborting.", apiEx);
                            throw;
                        }
                        else
                        {
                            Logging.Log(Logging.LogType.Information, "API Connection", "IGDB API rate limit hit while accessing endpoint " + Endpoint, apiEx);
                            
                            RetryAttempts += 1;

                            return await IGDBAPI<T>(Endpoint, fieldList, WhereClause);
                        }
                    default:
                        Logging.Log(Logging.LogType.Warning, "API Connection", "Exception when accessing endpoint " + Endpoint, apiEx);
                        throw;
                }
            }
            catch(Exception ex)
            {
                Logging.Log(Logging.LogType.Warning, "API Connection", "Exception when accessing endpoint " + Endpoint, ex);
                throw;
            }
        }
    }
}
using System;
using System.Data;
using System.Net;
using IGDB;
using IGDB.Models;

namespace gaseous_server.Classes.Metadata
{
    public class Communications
    {
        private static IGDBClient igdb = new IGDBClient(
                    // Found in Twitch Developer portal for your app
                    Config.IGDB.ClientId,
                    Config.IGDB.Secret
                );

        public static async Task<T[]> APIComm<T>(string Endpoint, string fieldList, string WhereClause)
        {
            Logging.Log(Logging.LogType.Information, "API Connection", "Accessing API for end point " + Endpoint);

            return await igdb.QueryAsync<T>(Endpoint, query: fieldList + " " + WhereClause + ";");
        }
    }
}
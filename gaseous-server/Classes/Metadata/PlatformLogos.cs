using System;
using gaseous_tools;
using IGDB;
using IGDB.Models;
using MySqlX.XDevAPI.Common;
using static gaseous_tools.Config.ConfigFile;

namespace gaseous_server.Classes.Metadata
{
	public class PlatformLogos
    {
        public PlatformLogos()
        {
        }

        private static IGDBClient igdb = new IGDBClient(
                    // Found in Twitch Developer portal for your app
                    Config.IGDB.ClientId,
                    Config.IGDB.Secret
                );

        public async static void GetPlatformLogo(long Id, string LogoPath)
        {
            var logo_results = await igdb.QueryAsync<PlatformLogo>(IGDBClient.Endpoints.PlatformLogos, query: "fields alpha_channel,animated,checksum,height,image_id,url,width; where id = " + Id + ";");
            var logo_result = logo_results.First();

            using (var client = new HttpClient())
            {
                using (var s = client.GetStreamAsync("https:" + logo_result.Url))
                {
                    using (var fs = new FileStream(LogoPath, FileMode.OpenOrCreate))
                    {
                        s.Result.CopyTo(fs);
                    }
                }
            }
        }
	}
}


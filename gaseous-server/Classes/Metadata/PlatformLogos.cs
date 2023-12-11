using System;
using IGDB;
using IGDB.Models;


namespace gaseous_server.Classes.Metadata
{
	public class PlatformLogos
    {
        const string fieldList = "fields alpha_channel,animated,checksum,height,image_id,url,width;";

        public PlatformLogos()
        {
        }

        public static PlatformLogo? GetPlatformLogo(long? Id, string LogoPath)
        {
            if ((Id == 0) || (Id == null))
            {
                return null;
            }
            else
            {
                Task<PlatformLogo> RetVal = _GetPlatformLogo(SearchUsing.id, Id, LogoPath);
                return RetVal.Result;
            }
        }

        public static PlatformLogo GetPlatformLogo(string Slug, string LogoPath)
        {
            Task<PlatformLogo> RetVal = _GetPlatformLogo(SearchUsing.slug, Slug, LogoPath);
            return RetVal.Result;
        }

        private static async Task<PlatformLogo> _GetPlatformLogo(SearchUsing searchUsing, object searchValue, string LogoPath)
        {
            // check database first
            Storage.CacheStatus? cacheStatus = new Storage.CacheStatus();
            if (searchUsing == SearchUsing.id)
            {
                cacheStatus = Storage.GetCacheStatus("PlatformLogo", (long)searchValue);
            }
            else
            {
                cacheStatus = Storage.GetCacheStatus("PlatformLogo", (string)searchValue);
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

            PlatformLogo returnValue = new PlatformLogo();
            bool forceImageDownload = false;
            switch (cacheStatus)
            {
                case Storage.CacheStatus.NotPresent:
                    returnValue = await GetObjectFromServer(WhereClause, LogoPath);
                    if (returnValue != null)
                    {
                        Storage.NewCacheValue(returnValue);
                        forceImageDownload = true;
                    }
                    break;  
                case Storage.CacheStatus.Expired:
                    try
                    {
                        returnValue = await GetObjectFromServer(WhereClause, LogoPath);
                        Storage.NewCacheValue(returnValue, true);
                        forceImageDownload = true;
                    }
                    catch (Exception ex)
                    {
                        Logging.Log(Logging.LogType.Warning, "Metadata: " + returnValue.GetType().Name, "An error occurred while connecting to IGDB. WhereClause: " + WhereClause, ex);
                        returnValue = Storage.GetCacheValue<PlatformLogo>(returnValue, "id", (long)searchValue);
                    }
                    break;  
                case Storage.CacheStatus.Current:
                    returnValue = Storage.GetCacheValue<PlatformLogo>(returnValue, "id", (long)searchValue);
                    break;
                default:
                    throw new Exception("How did you get here?");
            }

            if (returnValue != null)
            {
                if ((!File.Exists(Path.Combine(LogoPath, "Logo.jpg"))) || forceImageDownload == true)
                {
                    GetImageFromServer(returnValue.Url, LogoPath, LogoSize.t_thumb);
                    GetImageFromServer(returnValue.Url, LogoPath, LogoSize.t_logo_med);
                }
            }

            return returnValue;
        }

        private enum SearchUsing
        {
            id,
            slug
        }

        private static async Task<PlatformLogo?> GetObjectFromServer(string WhereClause, string LogoPath)
        {
            // get PlatformLogo metadata
            var results = await Communications.APIComm<PlatformLogo>(IGDBClient.Endpoints.PlatformLogos, fieldList, WhereClause);
            if (results.Length > 0)
            {
                var result = results.First();

                GetImageFromServer(result.Url, LogoPath, LogoSize.t_thumb);
                GetImageFromServer(result.Url, LogoPath, LogoSize.t_logo_med);

                return result;
            }
            else
            {
                return null;
            }
        }

        private static void GetImageFromServer(string Url, string LogoPath, LogoSize logoSize)
        {
            using (var client = new HttpClient())
            {
                string fileName = "Logo.jpg";
                string extension = "jpg";
                switch (logoSize)
                {
                    case LogoSize.t_thumb:
                        fileName = "Logo_Thumb";
                        extension = "jpg";
                        break;
                    case LogoSize.t_logo_med:
                        fileName = "Logo_Medium";
                        extension = "png";
                        break;
                    default:
                        fileName = "Logo";
                        extension = "jpg";
                        break;
                }
                string imageUrl = Url.Replace(LogoSize.t_thumb.ToString(), logoSize.ToString()).Replace("jpg", extension);

                using (var s = client.GetStreamAsync("https:" + imageUrl))
                {
                    if (!Directory.Exists(LogoPath)) { Directory.CreateDirectory(LogoPath); }
                    using (var fs = new FileStream(Path.Combine(LogoPath, fileName + "." + extension), FileMode.OpenOrCreate))
                    {
                        s.Result.CopyTo(fs);
                    }
                }
            }
        }

        private enum LogoSize
        {
            t_thumb,
            t_logo_med
        }
	}
}


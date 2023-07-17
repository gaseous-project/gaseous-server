using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using gaseous_tools;

namespace gaseous_server.Classes
{
	public class Bios
	{
		public Bios()
		{
            
		}

        public static Models.PlatformMapping.PlatformMapItem? BiosHashSignatureLookup(string MD5)
        {
            foreach (Models.PlatformMapping.PlatformMapItem platformMapping in Models.PlatformMapping.PlatformMap)
            {
                if (platformMapping.WebEmulator != null)
                {
                    if (platformMapping.WebEmulator.Bios != null)
                    {
                        foreach (Models.PlatformMapping.PlatformMapItem.WebEmulatorItem.EmulatorBiosItem emulatorBiosItem in platformMapping.WebEmulator.Bios)
                        {
                            if (emulatorBiosItem.hash.ToLower() == MD5.ToLower())
                            {
                                return platformMapping;
                            }
                        }
                    }
                }
            }

            return null;
        }

        public static List<BiosItem> GetBios()
        {
            return BuildBiosList();
        }

        public static List<BiosItem> GetBios(long PlatformId, bool HideUnavailable)
        {
            List<BiosItem> biosItems = new List<BiosItem>();
            foreach (BiosItem biosItem in BuildBiosList())
            {
                if (biosItem.platformid == PlatformId)
                {
                    if (HideUnavailable == true)
                    {
                        if (biosItem.Available == true)
                        {
                            biosItems.Add(biosItem);
                        }
                    }
                    else
                    {
                        biosItems.Add(biosItem);
                    }
                }
            }

            return biosItems;
        }

        private static List<BiosItem> BuildBiosList()
        {
            List<BiosItem> biosItems = new List<BiosItem>();

            foreach (Models.PlatformMapping.PlatformMapItem platformMapping in Models.PlatformMapping.PlatformMap)
            {
                if (platformMapping.WebEmulator != null)
                    {
                    if (platformMapping.WebEmulator.Bios != null)
                    {
                        IGDB.Models.Platform platform = Metadata.Platforms.GetPlatform(platformMapping.IGDBId);

                        foreach (Models.PlatformMapping.PlatformMapItem.WebEmulatorItem.EmulatorBiosItem emulatorBios in platformMapping.WebEmulator.Bios)
                        {
                            BiosItem biosItem = new BiosItem
                            {
                                platformid = platformMapping.IGDBId,
                                platformslug = platform.Slug,
                                description = emulatorBios.description,
                                filename = emulatorBios.filename,
                                region = emulatorBios.region,
                                hash = emulatorBios.hash
                            };
                            biosItems.Add(biosItem);
                        }
                    }
                }
            }
            return biosItems;
        }

        public class BiosItem : Models.PlatformMapping.PlatformMapItem.WebEmulatorItem.EmulatorBiosItem
        {
            public long platformid { get; set; }
            public string platformslug { get; set; }
            public string biosPath
            {
                get
                {
                    return Path.Combine(Config.LibraryConfiguration.LibraryBIOSDirectory, platformslug, base.filename);
                }
            }
            public bool Available {
                get
                {
                    return File.Exists(biosPath);
                }
            }
        }
    }
}


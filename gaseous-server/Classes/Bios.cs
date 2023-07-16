using System;
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
    }
}


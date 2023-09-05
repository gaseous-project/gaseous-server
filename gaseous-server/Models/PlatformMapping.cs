using System;
using System.Reflection;
using System.Text.Json.Serialization;
using gaseous_server.Classes;

namespace gaseous_server.Models
{
	public class PlatformMapping
	{
        public PlatformMapping()
        {

        }

        //private static List<PlatformMapItem> _PlatformMaps = new List<PlatformMapItem>();
        public static List<PlatformMapItem> PlatformMap
        {
            get
            {
                // load platform maps from: gaseous_server.Support.PlatformMap.json
                List<PlatformMapItem> _PlatformMaps = new List<PlatformMapItem>();
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "gaseous_server.Support.PlatformMap.json";
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string rawJson = reader.ReadToEnd();
                    _PlatformMaps.Clear();
                    _PlatformMaps = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlatformMapItem>>(rawJson);
                }

                return _PlatformMaps;
            }
        }

        public static void GetIGDBPlatformMapping(ref Models.Signatures_Games Signature, FileInfo RomFileInfo, bool SetSystemName)
        {
            bool PlatformFound = false;
            foreach (Models.PlatformMapping.PlatformMapItem PlatformMapping in Models.PlatformMapping.PlatformMap)
            {
                if (PlatformMapping.KnownFileExtensions.Contains(RomFileInfo.Extension, StringComparer.OrdinalIgnoreCase))
                {
                    if (SetSystemName == true)
                    {
                        if (Signature.Game != null) { Signature.Game.System = PlatformMapping.IGDBName; }
                    }
                    Signature.Flags.IGDBPlatformId = PlatformMapping.IGDBId;
                    Signature.Flags.IGDBPlatformName = PlatformMapping.IGDBName;

                    PlatformFound = true;
                    break;
                }
            }

            if (PlatformFound == false)
            {
                foreach (Models.PlatformMapping.PlatformMapItem PlatformMapping in Models.PlatformMapping.PlatformMap)
                {
                    if (
                        PlatformMapping.IGDBName == Signature.Game.System ||
                        PlatformMapping.AlternateNames.Contains(Signature.Game.System, StringComparer.OrdinalIgnoreCase)
                        )
                    {
                        if (SetSystemName == true)
                        {
                            if (Signature.Game != null) { Signature.Game.System = PlatformMapping.IGDBName; }
                        }
                        Signature.Flags.IGDBPlatformId = PlatformMapping.IGDBId;
                        Signature.Flags.IGDBPlatformName = PlatformMapping.IGDBName;

                        PlatformFound = true;
                        break;
                    }
                }
            }
        }

        public class PlatformMapItem
        {
            public int IGDBId { get; set; }
            public string IGDBName { get; set; }
            public List<string> AlternateNames { get; set; } = new List<string>();
            public List<string> KnownFileExtensions { get; set; } = new List<string>();
            //public Dictionary<string, object>? WebEmulator { get; set; }
            public WebEmulatorItem? WebEmulator { get; set; }

            public class WebEmulatorItem
            {
                public string Type { get; set; }
                public string Core { get; set; }
                public List<EmulatorBiosItem> Bios { get; set; }

                public class EmulatorBiosItem
                {
                    public string hash { get; set; }
                    public string description { get; set; }
                    public string filename { get; set; }
                    public string region { get; set; }
                }
            }
        }
	}
}


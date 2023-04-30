using System;
using System.Reflection;

namespace gaseous_server.Models
{
	public class PlatformMapping
	{
        public PlatformMapping()
        {

        }

        private static List<PlatformMapItem> _PlatformMaps = new List<PlatformMapItem>();
        public static List<PlatformMapItem> PlatformMap
        {
            get
            {
                if (_PlatformMaps.Count == 0)
                {
                    // load platform maps from: gaseous_server.Support.PlatformMap.json
                    var assembly = Assembly.GetExecutingAssembly();
                    var resourceName = "gaseous_server.Support.PlatformMap.json";
                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string rawJson = reader.ReadToEnd();
                        _PlatformMaps.Clear();
                        _PlatformMaps = Newtonsoft.Json.JsonConvert.DeserializeObject<List<PlatformMapItem>>(rawJson);
                    }
                }

                return _PlatformMaps;
            }
        }

        public static void GetIGDBPlatformMapping(ref Models.Signatures_Games Signature, FileInfo RomFileInfo, bool SetSystemName)
        {
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
                }
            }
        }

        public class PlatformMapItem
		{
            public int IGDBId { get; set; }
            public string IGDBName { get; set; }
            public List<string> AlternateNames { get; set; } = new List<string>();
            public List<string> KnownFileExtensions { get; set; } = new List<string>();
        }
	}
}


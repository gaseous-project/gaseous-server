using System;
using System.Collections.Generic;

namespace gaseous_identifier.classes
{
	public class tosecXML
	{
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Version { get; set; }
        public string Author { get; set; }
        public string Email { get; set; }
        public string Homepage { get; set; }
        public Uri? Url { get; set; }

        public List<Game> Games { get; set; }

        public class Game
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Year { get; set; }
            public string Publisher { get; set; }
            public List<Rom> Roms { get; set; }
            
            public class Rom
            {
                public string Name { get; set; }
                public UInt64 Size { get; set; }
                public string Crc { get; set; }
                public string Md5 { get; set; }
                public string Sha1 { get; set; }

                public string flags { get; set; }

                public RomTypes RomType { get; set; }
                public UInt16 DiskNumber { get; set; }
                public string DiskSide { get; set; }

                public enum RomTypes
                {
                    Cartridge = 0,
                    Cassette = 1,
                    Floppy = 2,
                    CD = 3,
                    DVD = 4,
                    Unknown = 100
                }
            }

        }
    }
}


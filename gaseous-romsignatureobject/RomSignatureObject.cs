using System;
using System.Collections.Generic;

namespace Gaseous_ROMSignatureObject
{
    /// <summary>
    /// Object returned by all signature engines containing metadata about the ROM's in the data files
    ///
    /// This class was based on the TOSEC dataset, so may need to be expanded as new signature engines are added
    /// </summary>
	public class RomSignatureObject
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public string? Version { get; set; }
        public string? Author { get; set; }
        public string? Email { get; set; }
        public string? Homepage { get; set; }
        public Uri? Url { get; set; }
        public string? SourceType { get; set; }
        public string SourceMd5 { get; set; } = "";
        public string SourceSHA1 { get; set; } = "";

        public List<Game> Games { get; set; } = new List<Game>();

        public class Game
        {
            public string? Name { get; set; }
            public string? Description { get; set; }
            public string? Year { get; set; }
            public string? Publisher { get; set; }
            public DemoTypes Demo { get; set; }
            public string? System { get; set; }
            public string? SystemVariant { get; set; }
            public string? Video { get; set; }
            public string? Country { get; set; }
            public string? Language { get; set; }
            public string? Copyright { get; set; }
            public List<Rom> Roms { get; set; } = new List<Rom>();
            public int RomCount
            {
                get
                {
                    return Roms.Count();
                }
            }

            public enum DemoTypes
            {
                NotDemo = 0,
                demo = 1,
                demo_kiosk = 2,
                demo_playable = 3,
                demo_rolling = 4,
                demo_slideshow = 5
            }

            public class Rom
            {
                public string? Name { get; set; }
                public UInt64? Size { get; set; }
                public string? Crc { get; set; }
                public string? Md5 { get; set; }
                public string? Sha1 { get; set; }

                public string? DevelopmentStatus { get; set; }

                public List<string> flags { get; set; } = new List<string>();

                public RomTypes RomType { get; set; }
                public string? RomTypeMedia { get; set; }
                public string? MediaLabel { get; set; }

                public enum RomTypes
                {
                    /// <summary>
                    /// Media type is unknown
                    /// </summary>
                    Unknown = 0,

                    /// <summary>
                    /// Optical media
                    /// </summary>
                    Disc = 1,

                    /// <summary>
                    /// Magnetic media
                    /// </summary>
                    Disk = 2,

                    /// <summary>
                    /// Individual files
                    /// </summary>
                    File = 3,

                    /// <summary>
                    /// Individual pars
                    /// </summary>
                    Part = 4,

                    /// <summary>
                    /// Tape base media
                    /// </summary>
                    Tape = 5,

                    /// <summary>
                    /// Side of the media
                    /// </summary>
                    Side = 6
                }
            }

        }
    }
}


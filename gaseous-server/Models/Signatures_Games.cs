﻿using System;
using static gaseous_romsignatureobject.RomSignatureObject.Game;

namespace gaseous_server.Models
{
	public class Signatures_Games
	{
		public Signatures_Games()
		{
		}

        public GameItem? Game { get; set; }
        public RomItem? Rom { get; set; }

		public class GameItem
		{
            public Int32? Id { get; set; }
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
            
            public enum DemoTypes
            {
                NotDemo = 0,
                demo = 1,
                demo_kiosk = 2,
                demo_playable = 3,
                demo_rolling = 4,
                demo_slideshow = 5
            }
        }

        public class RomItem
        {
            public Int32? Id { get; set; }
            public string? Name { get; set; }
            public Int64? Size { get; set; }
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

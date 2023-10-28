﻿using System;
using System.Data;
using gaseous_server.Classes;

namespace gaseous_server.Models
{
	public class Signatures_Status
	{

        private Int64 _SourceCount = 0;
        private Int64 _PlatformCount = 0;
        private Int64 _GameCount = 0;
        private Int64 _RomCount = 0;

		public Signatures_Status()
		{
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "select (select count(*) from Signatures_Sources) as SourceCount, (select count(*) from Signatures_Platforms) as PlatformCount, (select count(*) from Signatures_Games) as GameCount, (select count(*) from Signatures_Roms) as RomCount;";
            
            DataTable sigDb = db.ExecuteCMD(sql);
            if (sigDb.Rows.Count > 0)
            {
                _SourceCount = (Int64)sigDb.Rows[0]["SourceCount"];
                _PlatformCount = (Int64)sigDb.Rows[0]["PlatformCount"];
                _GameCount = (Int64)sigDb.Rows[0]["GameCount"];
                _RomCount = (Int64)sigDb.Rows[0]["RomCount"];
            }
        }

        public Int64 Sources
        {
            get
            {
                return _SourceCount;
            }
        }

        public Int64 Platforms
        {
            get
            {
                return _PlatformCount;
            }
        }

        public Int64 Games
        {
            get
            {
                return _GameCount;
            }
        }

        public Int64 Roms
        {
            get
            {
                return _RomCount;
            }
        }
    }
}


using System;
using System.Data;
using System.Threading.Tasks;
using gaseous_tools;

namespace gaseous_server.Classes
{
	public class ImportGames
	{
		public ImportGames(string ImportPath)
		{
			if (Directory.Exists(ImportPath))
			{
				string[] importContents_Files = Directory.GetFiles(ImportPath);
                string[] importContents_Directories = Directory.GetDirectories(ImportPath);

				// import files first
				foreach (string importContent in importContents_Files) {
					ImportGame importGame = new ImportGame();
					importGame.ImportGameFile(importContent);
				}
            }
			else
			{
				Logging.Log(Logging.LogType.Critical, "Import Games", "The import directory " + ImportPath + " does not exist.");
				throw new DirectoryNotFoundException("Invalid path: " + ImportPath);
			}
		}


	}

	public class ImportGame
	{
        private Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

		public void ImportGameFile(string GameFileImportPath, bool IsDirectory = false)
		{
			Logging.Log(Logging.LogType.Information, "Import Game", "Processing item " + GameFileImportPath);
			if (IsDirectory == false)
			{
                FileInfo fi = new FileInfo(GameFileImportPath);

                // process as a single file
                // check 1: do we have a signature for it?
                Common.hashObject hash = new Common.hashObject(GameFileImportPath);
				gaseous_server.Controllers.SignaturesController sc = new Controllers.SignaturesController();
                List<Models.Signatures_Games> signatures = sc.GetSignature(hash.md5hash);
				if (signatures.Count == 0)
				{
					// no md5 signature found - try sha1
					signatures = sc.GetSignature("", hash.sha1hash);
				}

				Models.Signatures_Games discoveredSignature = new Models.Signatures_Games();
				if (signatures.Count == 1)
				{
					// only 1 signature found!
					discoveredSignature = signatures.ElementAt(0);
                    gaseous_server.Models.PlatformMapping.GetIGDBPlatformMapping(ref discoveredSignature, fi, false);
                }
				else if (signatures.Count > 1)
				{
					// more than one signature found - find one with highest score
					foreach(Models.Signatures_Games Sig in signatures)
					{
						if (Sig.Score > discoveredSignature.Score)
						{
							discoveredSignature = Sig;
                            gaseous_server.Models.PlatformMapping.GetIGDBPlatformMapping(ref discoveredSignature, fi, false);
                        }
					}
				}
				else
				{
					// no signature match found - try alternate methods
					Models.Signatures_Games.GameItem gi = new Models.Signatures_Games.GameItem();
					Models.Signatures_Games.RomItem ri = new Models.Signatures_Games.RomItem();

					// game title is the file name without the extension or path
					gi.Name = Path.GetFileNameWithoutExtension(GameFileImportPath);

                    // guess platform
                    gaseous_server.Models.PlatformMapping.GetIGDBPlatformMapping(ref discoveredSignature, fi, true);

                    // get rom data
                    ri.Name = Path.GetFileName(GameFileImportPath);
					ri.Md5 = hash.md5hash;
					ri.Sha1 = hash.sha1hash;

					discoveredSignature.Game = gi;
					discoveredSignature.Rom = ri;
				}

				//Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(discoveredSignature));

				IGDB.Models.Platform determinedPlatform = Platforms.GetPlatform(discoveredSignature.Flags.IGDBPlatformId);
            }
		}
	}
}


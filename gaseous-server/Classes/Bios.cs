using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using gaseous_server.Classes.Metadata;
using HasheousClient.Models.Metadata.IGDB;

namespace gaseous_server.Classes
{
    public class Bios
    {
        public Bios()
        {

        }

        public static void ImportBiosFile(string FilePath, HashObject Hash, ref Dictionary<string, object> BiosFileInfo)
        {
            BiosFileInfo.Add("type", "bios");
            BiosFileInfo.Add("status", "notimported");

            foreach (Classes.Bios.BiosItem biosItem in Classes.Bios.GetBios().Result)
            {
                if (biosItem.Available == false)
                {
                    if (biosItem.hash == Hash.md5hash)
                    {
                        string biosPath = Path.Combine(Config.LibraryConfiguration.LibraryFirmwareDirectory, biosItem.hash + ".bios");
                        Logging.Log(Logging.LogType.Information, "Import BIOS File", "  " + FilePath + " is a BIOS file - moving to " + biosPath);

                        File.Move(FilePath, biosItem.biosPath, true);

                        BiosFileInfo.Add("name", biosItem.filename);
                        BiosFileInfo.Add("platform", Platforms.GetPlatform(biosItem.platformid));
                        BiosFileInfo["status"] = "imported";
                    }
                }
                else
                {
                    if (biosItem.hash == Hash.md5hash)
                    {
                        BiosFileInfo["status"] = "duplicate";
                    }
                }
            }
        }

        public static void MigrateToNewFolderStructure()
        {
            // migrate from old BIOS file structure which had each bios file inside a folder named for the platform to the new structure which has each file in a subdirectory named after the MD5 hash
            if (Directory.Exists(Config.LibraryConfiguration.LibraryBIOSDirectory))
            {
                foreach (Models.PlatformMapping.PlatformMapItem platformMapping in Models.PlatformMapping.PlatformMap)
                {
                    if (platformMapping.Bios != null)
                    {
                        foreach (Models.PlatformMapping.PlatformMapItem.EmulatorBiosItem emulatorBiosItem in platformMapping.Bios)
                        {
                            string oldBiosPath = Path.Combine(Config.LibraryConfiguration.LibraryBIOSDirectory, platformMapping.IGDBSlug.ToString(), emulatorBiosItem.filename);
                            string newBiosPath = Path.Combine(Config.LibraryConfiguration.LibraryFirmwareDirectory, emulatorBiosItem.hash + ".bios");

                            if (File.Exists(oldBiosPath))
                            {
                                File.Copy(oldBiosPath, newBiosPath, true);
                            }
                        }
                    }
                }

                // remove old BIOS folder structure
                Directory.Delete(Config.LibraryConfiguration.LibraryBIOSDirectory, true);
            }
        }

        public static Models.PlatformMapping.PlatformMapItem? BiosHashSignatureLookup(string MD5)
        {
            foreach (Models.PlatformMapping.PlatformMapItem platformMapping in Models.PlatformMapping.PlatformMap)
            {
                if (platformMapping.Bios != null)
                {
                    foreach (Models.PlatformMapping.PlatformMapItem.EmulatorBiosItem emulatorBiosItem in platformMapping.Bios)
                    {
                        if (emulatorBiosItem.hash.ToLower() == MD5.ToLower())
                        {
                            return platformMapping;
                        }
                    }
                }
            }

            return null;
        }

        public static async Task<List<BiosItem>> GetBios()
        {
            return await BuildBiosList();
        }

        public static async Task<List<BiosItem>> GetBios(long PlatformId, bool HideUnavailable)
        {
            List<BiosItem> biosItems = new List<BiosItem>();
            foreach (BiosItem biosItem in await BuildBiosList())
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

        private static async Task<List<BiosItem>> BuildBiosList()
        {
            List<BiosItem> biosItems = new List<BiosItem>();

            foreach (Models.PlatformMapping.PlatformMapItem platformMapping in Models.PlatformMapping.PlatformMap)
            {
                if (platformMapping.Bios != null)
                {
                    Platform platform = await Metadata.Platforms.GetPlatform(platformMapping.IGDBId);

                    foreach (Models.PlatformMapping.PlatformMapItem.EmulatorBiosItem emulatorBios in platformMapping.Bios)
                    {
                        BiosItem biosItem = new BiosItem
                        {
                            platformid = platformMapping.IGDBId,
                            platformslug = platform.Slug,
                            platformname = platform.Name,
                            description = emulatorBios.description,
                            filename = emulatorBios.filename,
                            hash = emulatorBios.hash.ToLower()
                        };
                        biosItems.Add(biosItem);
                    }
                }
            }
            return biosItems;
        }

        public class BiosItem : Models.PlatformMapping.PlatformMapItem.EmulatorBiosItem
        {
            public long platformid { get; set; }
            public string platformslug { get; set; }
            public string platformname { get; set; }
            public string biosPath
            {
                get
                {
                    return Path.Combine(Config.LibraryConfiguration.LibraryFirmwareDirectory, hash + ".bios");
                }
            }
            public bool Available
            {
                get
                {
                    bool fileExists = File.Exists(biosPath);
                    return fileExists;
                }
            }
        }
    }
}


using gaseous_server.Models;

namespace gaseous_server.Classes
{
    public class SystemInfo
    {
        public static SystemInfoModel.PathItem GetDisk(string Path)
        {
            SystemInfoModel.PathItem pathItem = new SystemInfoModel.PathItem
            {
                LibraryPath = Path,
                SpaceUsed = Common.DirSize(new DirectoryInfo(Path)),
                SpaceAvailable = new DriveInfo(Path).AvailableFreeSpace,
                TotalSpace = new DriveInfo(Path).TotalSize
            };

            return pathItem;
        }
    }
}
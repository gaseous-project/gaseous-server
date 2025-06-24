using System;
using System.IO;
using System.Security.Cryptography;

namespace gaseous_server.Classes
{
    public class HashObject
    {
        public string md5hash { get; set; } = string.Empty;
        public string sha1hash { get; set; } = string.Empty;
        public string sha256hash { get; set; } = string.Empty;
        public string crc32hash { get; set; } = string.Empty;

        public HashObject() { }

        public HashObject(string fileName)
        {
            using var fileStream = File.OpenRead(fileName);

            Logging.Log(Logging.LogType.Information, "Hash File", $"Generating MD5 hash for file: {fileName}");
            using (var md5 = MD5.Create())
            {
                md5hash = BitConverter.ToString(md5.ComputeHash(fileStream)).Replace("-", "").ToLowerInvariant();
            }

            Logging.Log(Logging.LogType.Information, "Hash File", $"Generating SHA1 hash for file: {fileName}");
            fileStream.Position = 0;
            using (var sha1 = SHA1.Create())
            {
                sha1hash = BitConverter.ToString(sha1.ComputeHash(fileStream)).Replace("-", "").ToLowerInvariant();
            }

            Logging.Log(Logging.LogType.Information, "Hash File", $"Generating SHA256 hash for file: {fileName}");
            fileStream.Position = 0;
            using (var sha256 = SHA256.Create())
            {
                sha256hash = BitConverter.ToString(sha256.ComputeHash(fileStream)).Replace("-", "").ToLowerInvariant();
            }

            Logging.Log(Logging.LogType.Information, "Hash File", $"Generating CRC32 hash for file: {fileName}");
            uint crc32HashCalc = CRC32.ComputeFile(fileName);
            crc32hash = crc32HashCalc.ToString("x8");
        }
    }
}

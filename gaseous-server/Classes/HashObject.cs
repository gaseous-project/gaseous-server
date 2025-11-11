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

            Logging.LogKey(Logging.LogType.Information, "process.hash_file", "hashfile.generating_md5", null, new string[] { fileName });
            using (var md5 = MD5.Create())
            {
                md5hash = BitConverter.ToString(md5.ComputeHash(fileStream)).Replace("-", "").ToLowerInvariant();
            }

            Logging.LogKey(Logging.LogType.Information, "process.hash_file", "hashfile.generating_sha1", null, new string[] { fileName });
            fileStream.Position = 0;
            using (var sha1 = SHA1.Create())
            {
                sha1hash = BitConverter.ToString(sha1.ComputeHash(fileStream)).Replace("-", "").ToLowerInvariant();
            }

            Logging.LogKey(Logging.LogType.Information, "process.hash_file", "hashfile.generating_sha256", null, new string[] { fileName });
            fileStream.Position = 0;
            using (var sha256 = SHA256.Create())
            {
                sha256hash = BitConverter.ToString(sha256.ComputeHash(fileStream)).Replace("-", "").ToLowerInvariant();
            }

            Logging.LogKey(Logging.LogType.Information, "process.hash_file", "hashfile.generating_crc32", null, new string[] { fileName });
            uint crc32HashCalc = CRC32.ComputeFile(fileName);
            crc32hash = crc32HashCalc.ToString("x8");
        }
    }
}

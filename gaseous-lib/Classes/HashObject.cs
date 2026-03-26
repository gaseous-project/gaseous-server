using System;
using System.IO;
using System.Security.Cryptography;

namespace gaseous_server.Classes
{
    /// <summary>
    /// Represents a collection of hash values for a file, including MD5, SHA1, SHA256, and CRC32.
    /// </summary>
    public class HashObject
    {
        /// <summary>
        /// The MD5 hash of the file, represented as a lowercase hexadecimal string.
        /// </summary>
        public string md5hash { get; set; } = string.Empty;

        /// <summary>
        /// The SHA1 hash of the file, represented as a lowercase hexadecimal string.
        /// </summary>
        public string sha1hash { get; set; } = string.Empty;

        /// <summary>
        /// The SHA256 hash of the file, represented as a lowercase hexadecimal string.
        /// </summary>
        public string sha256hash { get; set; } = string.Empty;

        /// <summary>
        /// The CRC32 hash of the file, represented as a lowercase hexadecimal string.
        /// </summary>
        public string crc32hash { get; set; } = string.Empty;

        /// <summary>
        /// Initializes a new instance of the HashObject class with empty hash values.
        /// </summary>
        public HashObject() { }

        /// <summary>
        /// Initializes a new instance of the HashObject class with specified hash values.
        /// </summary>
        /// <param name="md5">The MD5 hash value.</param>
        /// <param name="sha1">The SHA1 hash value.</param>
        /// <param name="sha256">The SHA256 hash value.</param>
        /// <param name="crc32">The CRC32 hash value.</param>
        public HashObject(string md5, string sha1, string sha256, string crc32)
        {
            md5hash = md5;
            sha1hash = sha1;
            sha256hash = sha256;
            crc32hash = crc32;
        }

        /// <summary>
        /// Initializes a new instance of the HashObject class by computing the hash values for the specified file.
        /// </summary>
        /// <param name="fileName">The path to the file for which to compute hash values.</param>
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

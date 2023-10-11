using System;
using System.Security.Cryptography;

namespace gaseous_tools
{
	public class Common
	{
		/// <summary>
		/// Returns IfNullValue if the ObjectToCheck is null
		/// </summary>
		/// <param name="ObjectToCheck">Any nullable object to check for null</param>
		/// <param name="IfNullValue">Any object to return if ObjectToCheck is null</param>
		/// <returns></returns>
		static public object ReturnValueIfNull(object? ObjectToCheck, object IfNullValue)
		{
			if (ObjectToCheck == null || ObjectToCheck == System.DBNull.Value)
			{
				return IfNullValue;
			} else
			{
				return ObjectToCheck;
			}
		}

		static public DateTime ConvertUnixToDateTime(double UnixTimeStamp)
		{
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(UnixTimeStamp).ToLocalTime();
            return dateTime;
        }

		public class hashObject
		{
			public hashObject()
			{

			}

			public hashObject(string FileName)
			{
                var xmlStream = File.OpenRead(FileName);

                var md5 = MD5.Create();
                byte[] md5HashByte = md5.ComputeHash(xmlStream);
                string md5Hash = BitConverter.ToString(md5HashByte).Replace("-", "").ToLowerInvariant();
				_md5hash = md5Hash;

                var sha1 = SHA1.Create();
				xmlStream.Position = 0;
                byte[] sha1HashByte = sha1.ComputeHash(xmlStream);
                string sha1Hash = BitConverter.ToString(sha1HashByte).Replace("-", "").ToLowerInvariant();
				_sha1hash = sha1Hash;

				xmlStream.Close();
            }

			string _md5hash = "";
			string _sha1hash = "";

			public string md5hash
			{
				get
				{
					return _md5hash.ToLower();
				}
				set
				{
					_md5hash = value;
				}
			}

			public string sha1hash
			{
				get
				{
					return _sha1hash.ToLower();
				}
				set
				{
					_sha1hash = value;
				}
			}
		}

        public static long DirSize(DirectoryInfo d)
        {
            long size = 0;
            // Add file sizes.
            FileInfo[] fis = d.GetFiles();
            foreach (FileInfo fi in fis)
            {
                size += fi.Length;
            }
            // Add subdirectory sizes.
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis)
            {
                size += DirSize(di);
            }
            return size;
        }

		public static string[] SkippableFiles = {
				".DS_STORE",
				"desktop.ini"
			};
    }
}


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
			if (ObjectToCheck == null)
			{
				return IfNullValue;
			} else
			{
				return ObjectToCheck;
			}
		}

		public class hashObject
		{
			public hashObject(string FileName)
			{
                var xmlStream = File.OpenRead(FileName);

                var md5 = MD5.Create();
                byte[] md5HashByte = md5.ComputeHash(xmlStream);
                string md5Hash = BitConverter.ToString(md5HashByte).Replace("-", "").ToLowerInvariant();
				_md5hash = md5hash;

                var sha1 = SHA1.Create();
                byte[] sha1HashByte = sha1.ComputeHash(xmlStream);
                string sha1Hash = BitConverter.ToString(sha1HashByte).Replace("-", "").ToLowerInvariant();
				_sha1hash = sha1hash;
            }

			string _md5hash = "";
			string _sha1hash = "";

			public string md5hash
			{
				get
				{
					return _md5hash;
				}
			}

			public string sha1hash
			{
				get
				{
					return _sha1hash;
				}
			}
		}
	}
}


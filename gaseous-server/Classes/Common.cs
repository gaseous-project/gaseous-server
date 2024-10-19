using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using static gaseous_server.Classes.Metadata.Communications;

namespace gaseous_server.Classes
{
	public static class Common
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
			}
			else
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

				Logging.Log(Logging.LogType.Information, "Hash File", "Generating MD5 hash for file: " + FileName);
				var md5 = MD5.Create();
				byte[] md5HashByte = md5.ComputeHash(xmlStream);
				string md5Hash = BitConverter.ToString(md5HashByte).Replace("-", "").ToLowerInvariant();
				_md5hash = md5Hash;

				Logging.Log(Logging.LogType.Information, "Hash File", "Generating SHA1 hash for file: " + FileName);
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

		public static string NormalizePath(string path)
		{
			return Path.GetFullPath(new Uri(path).LocalPath)
					.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}

		public static string GetDescription(this Enum value)
		{
			return ((DescriptionAttribute)Attribute.GetCustomAttribute(
				value.GetType().GetFields(BindingFlags.Public | BindingFlags.Static)
					.Single(x => x.GetValue(null).Equals(value)),
				typeof(DescriptionAttribute)))?.Description ?? value.ToString();
		}

		public static Point GetResolution(this Enum value)
		{
			string width = ((ResolutionAttribute)Attribute.GetCustomAttribute(
				value.GetType().GetFields(BindingFlags.Public | BindingFlags.Static)
					.Single(x => x.GetValue(null).Equals(value)),
				typeof(ResolutionAttribute)))?.width.ToString() ?? value.ToString();

			string height = ((ResolutionAttribute)Attribute.GetCustomAttribute(
			value.GetType().GetFields(BindingFlags.Public | BindingFlags.Static)
				.Single(x => x.GetValue(null).Equals(value)),
			typeof(ResolutionAttribute)))?.height.ToString() ?? value.ToString();

			return new Point(int.Parse(width), int.Parse(height));
		}

		public static bool IsNullableEnum(this Type t)
		{
			Type u = Nullable.GetUnderlyingType(t);
			return u != null && u.IsEnum;
		}

		// compression
		public static byte[] Compress(byte[] data)
		{
			MemoryStream output = new MemoryStream();
			using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
			{
				dstream.Write(data, 0, data.Length);
			}
			return output.ToArray();
		}

		public static byte[] Decompress(byte[] data)
		{
			MemoryStream input = new MemoryStream(data);
			MemoryStream output = new MemoryStream();
			using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
			{
				dstream.CopyTo(output);
			}
			return output.ToArray();
		}

		public static object GetEnvVar(string envName, string defaultValue)
		{
			if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable(envName)))
			{
				return Environment.GetEnvironmentVariable(envName);
			}
			else
			{
				return defaultValue;
			}
		}

		public static int GetLookupByCode(LookupTypes LookupType, string Code)
		{
			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "SELECT Id FROM " + LookupType.ToString() + " WHERE Code = @code";
			Dictionary<string, object> dbDict = new Dictionary<string, object>{
				{ "code", Code }
			};

			DataTable data = db.ExecuteCMD(sql, dbDict);
			if (data.Rows.Count == 0)
			{
				return -1;
			}
			else
			{
				return (int)data.Rows[0]["Id"];
			}
		}

		public static int GetLookupByValue(LookupTypes LookupType, string Value)
		{
			Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
			string sql = "SELECT Id FROM " + LookupType.ToString() + " WHERE Value = @value";
			Dictionary<string, object> dbDict = new Dictionary<string, object>{
				{ "value", Value }
			};

			DataTable data = db.ExecuteCMD(sql, dbDict);
			if (data.Rows.Count == 0)
			{
				return -1;
			}
			else
			{
				return (int)data.Rows[0]["Id"];
			}
		}

		public enum LookupTypes
		{
			Country,
			Language
		}
	}

	/// <summary>
	/// Provides a way to set contextual data that flows with the call and 
	/// async context of a test or invocation.
	/// </summary>
	public static class CallContext
	{
		static ConcurrentDictionary<string, AsyncLocal<object>> state = new ConcurrentDictionary<string, AsyncLocal<object>>();

		/// <summary>
		/// Stores a given object and associates it with the specified name.
		/// </summary>
		/// <param name="name">The name with which to associate the new item in the call context.</param>
		/// <param name="data">The object to store in the call context.</param>
		public static void SetData(string name, object data) =>
			state.GetOrAdd(name, _ => new AsyncLocal<object>()).Value = data;

		/// <summary>
		/// Retrieves an object with the specified name from the <see cref="CallContext"/>.
		/// </summary>
		/// <param name="name">The name of the item in the call context.</param>
		/// <returns>The object in the call context associated with the specified name, or <see langword="null"/> if not found.</returns>
		public static object GetData(string name) =>
			state.TryGetValue(name, out AsyncLocal<object> data) ? data.Value : null;
	}
}
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using static gaseous_server.Classes.Plugins.PluginManagement.ImageResize;

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

		public static string StripVersionsFromFileName(string fileName)
		{
			// strip anything in brackets
			fileName = Regex.Replace(fileName, @"\[.*?\]", "").Trim();
			fileName = Regex.Replace(fileName, @"\{.*?\}", "").Trim();
			fileName = Regex.Replace(fileName, @"\(.*?\)", "").Trim();

			// strip versions
			fileName = Regex.Replace(fileName, @"v(\d+\.)?(\d+\.)?(\*|\d+)$", "").Trim();
			fileName = Regex.Replace(fileName, @"Rev (\d+\.)?(\d+\.)?(\*|\d+)$", "").Trim();
			fileName = Regex.Replace(fileName, @"Revision (\d+\.)?(\d+\.)?(\*|\d+)$", "").Trim();
			fileName = Regex.Replace(fileName, @"Release (\d+\.)?(\d+\.)?(\*|\d+)$", "").Trim();
			fileName = Regex.Replace(fileName, @"Build (\d+\.)?(\d+\.)?(\*|\d+)$", "").Trim();
			fileName = Regex.Replace(fileName, @"Beta (\d+\.)?(\d+\.)?(\*|\d+)$", "").Trim();
			fileName = Regex.Replace(fileName, @"Alpha (\d+\.)?(\d+\.)?(\*|\d+)$", "").Trim();
			fileName = Regex.Replace(fileName, @"RC (\d+\.)?(\d+\.)?(\*|\d+)$", "").Trim();
			fileName = Regex.Replace(fileName, @"SP (\d+\.)?(\d+\.)?(\*|\d+)$", "").Trim();
			fileName = Regex.Replace(fileName, @"Service Pack (\d+\.)?(\d+\.)?(\*|\d+)$", "").Trim();
			fileName = Regex.Replace(fileName, @"Set (\d+\.)?(\d+\.)?(\*|\d+)$", "").Trim();

			return fileName;
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

		public static string[] NormalizeRelativePathSegments(string? input)
		{
			if (string.IsNullOrWhiteSpace(input))
			{
				return Array.Empty<string>();
			}

			return input
				.Replace('\\', '/')
				.Split('/', StringSplitOptions.RemoveEmptyEntries)
				.Where(segment => segment != "." && segment != ".." && !Path.IsPathRooted(segment) && !segment.Contains(':'))
				.ToArray();
		}

		public static char[] GetInvalidFileNameChars() => new char[]
		{
			'\"', '<', '>', '|', '\0',
			(char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
			(char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
			(char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
			(char)31, ':', '*', '?', '\\', '/'
		};

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
			string sql = "SELECT Id FROM Lookup" + LookupType.ToString() + " WHERE Code = @code";
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
			string sql = "SELECT Id FROM Lookup" + LookupType.ToString() + " WHERE Value = @value";
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

		public class RomanNumerals
		{
			/// <summary>
			/// Converts an integer to its Roman numeral representation.
			/// </summary>
			/// <param name="number">The integer to convert (1-3999).</param>
			/// <returns>A string containing the Roman numeral.</returns>
			public static string IntToRoman(int number)
			{
				if (number < 1 || number > 3999)
					throw new ArgumentOutOfRangeException(nameof(number), "Value must be in the range 1-3999.");

				var numerals = new[]
				{
				new { Value = 1000, Numeral = "M" },
				new { Value = 900, Numeral = "CM" },
				new { Value = 500, Numeral = "D" },
				new { Value = 400, Numeral = "CD" },
				new { Value = 100, Numeral = "C" },
				new { Value = 90, Numeral = "XC" },
				new { Value = 50, Numeral = "L" },
				new { Value = 40, Numeral = "XL" },
				new { Value = 10, Numeral = "X" },
				new { Value = 9, Numeral = "IX" },
				new { Value = 5, Numeral = "V" },
				new { Value = 4, Numeral = "IV" },
				new { Value = 1, Numeral = "I" }
			};

				var result = string.Empty;
				foreach (var item in numerals)
				{
					while (number >= item.Value)
					{
						result += item.Numeral;
						number -= item.Value;
					}
				}
				return result;
			}

			/// <summary>
			/// Finds the first Roman numeral in a string.
			/// </summary>
			/// <param name="input">The input string to search.</param>
			/// <returns>The first Roman numeral found, or null if none found.</returns>
			public static string? FindFirstRomanNumeral(string input)
			{
				if (string.IsNullOrEmpty(input))
					return null;

				// Regex for Roman numerals (1-3999, case-insensitive)
				var matches = Regex.Matches(input, @"\bM{0,3}(CM|CD|D?C{0,3})(XC|XL|L?X{0,3})(IX|IV|V?I{0,3})\b", RegexOptions.IgnoreCase);
				foreach (Match match in matches)
				{
					if (match.Success && !string.IsNullOrEmpty(match.Value))
						return match.Value.ToUpper();
				}

				return null;
			}

			/// <summary>
			/// Converts a Roman numeral string to its integer representation.
			/// </summary>
			/// <param name="roman">The Roman numeral string to convert.</param>
			/// <returns>The integer representation of the Roman numeral.</returns>
			public static int RomanToInt(string roman)
			{
				if (string.IsNullOrEmpty(roman))
					throw new ArgumentException("Input cannot be null or empty.", nameof(roman));

				var romanMap = new Dictionary<char, int>
			{
				{ 'I', 1 },
				{ 'V', 5 },
				{ 'X', 10 },
				{ 'L', 50 },
				{ 'C', 100 },
				{ 'D', 500 },
				{ 'M', 1000 }
			};

				int total = 0;
				int prevValue = 0;

				foreach (char c in roman.ToUpper())
				{
					if (!romanMap.ContainsKey(c))
						throw new ArgumentException($"Invalid Roman numeral character: {c}", nameof(roman));

					int currentValue = romanMap[c];

					// If the current value is greater than the previous value, subtract twice the previous value
					// (to account for the addition in the previous iteration).
					if (currentValue > prevValue)
					{
						total += currentValue - 2 * prevValue;
					}
					else
					{
						total += currentValue;
					}

					prevValue = currentValue;
				}

				return total;
			}
		}

		public class Numbers
		{
			private static readonly Dictionary<int, string> NumberWords = new Dictionary<int, string>
			{
				{ 0, "Zero" },
				{ 1, "One" },
				{ 2, "Two" },
				{ 3, "Three" },
				{ 4, "Four" },
				{ 5, "Five" },
				{ 6, "Six" },
				{ 7, "Seven" },
				{ 8, "Eight" },
				{ 9, "Nine" },
				{ 10, "Ten" },
				{ 11, "Eleven" },
				{ 12, "Twelve" },
				{ 13, "Thirteen" },
				{ 14, "Fourteen" },
				{ 15, "Fifteen" },
				{ 16, "Sixteen" },
				{ 17, "Seventeen" },
				{ 18, "Eighteen" },
				{ 19, "Nineteen" },
				{ 20, "Twenty" },
				{ 30, "Thirty" },
				{ 40, "Forty" },
				{ 50, "Fifty" },
				{ 60, "Sixty" },
				{ 70, "Seventy" },
				{ 80, "Eighty" },
				{ 90, "Ninety" },
				{ 100, "Hundred" },
				{ 1000, "Thousand" },
				{ 1000000, "Million" },
				{ 1000000000, "Billion" }
			};

			private static readonly Dictionary<string, int> WordsToNumber = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
			{
				{ "Zero", 0 },
				{ "One", 1 },
				{ "Two", 2 },
				{ "Three", 3 },
				{ "Four", 4 },
				{ "Five", 5 },
				{ "Six", 6 },
				{ "Seven", 7 },
				{ "Eight", 8 },
				{ "Nine", 9 },
				{ "Ten", 10 },
				{ "Eleven", 11 },
				{ "Twelve", 12 },
				{ "Thirteen", 13 },
				{ "Fourteen", 14 },
				{ "Fifteen", 15 },
				{ "Sixteen", 16 },
				{ "Seventeen", 17 },
				{ "Eighteen", 18 },
				{ "Nineteen", 19 },
				{ "Twenty", 20 },
				{ "Thirty", 30 },
				{ "Forty", 40 },
				{ "Fifty", 50 },
				{ "Sixty", 60 },
				{ "Seventy", 70 },
				{ "Eighty", 80 },
				{ "Ninety", 90 },
				{ "Hundred", 100 },
				{ "Thousand", 1000 },
				{ "Million", 1000000 },
				{ "Billion", 1000000000 }
			};

			/// <summary>
			/// Converts a number to its English word representation.
			/// </summary>
			/// <param name="number">The number to convert (0 to 999,999,999).</param>
			/// <returns>The English word representation of the number.</returns>
			public static string NumberToWords(int number)
			{
				if (number < 0 || number > 999999999)
					throw new ArgumentOutOfRangeException(nameof(number), "Value must be in the range 0-999,999,999.");

				if (number == 0)
					return "Zero";

				if (NumberWords.TryGetValue(number, out var word))
					return word;

				List<string> parts = new List<string>();

				// Billions
				int billions = number / 1000000000;
				if (billions > 0)
				{
					parts.Add(NumberToWords(billions) + " Billion");
					number %= 1000000000;
				}

				// Millions
				int millions = number / 1000000;
				if (millions > 0)
				{
					parts.Add(NumberToWords(millions) + " Million");
					number %= 1000000;
				}

				// Thousands
				int thousands = number / 1000;
				if (thousands > 0)
				{
					parts.Add(NumberToWords(thousands) + " Thousand");
					number %= 1000;
				}

				// Hundreds
				int hundreds = number / 100;
				if (hundreds > 0)
				{
					parts.Add(NumberWords[hundreds] + " Hundred");
					number %= 100;
				}

				// Ones and Tens
				if (number > 0)
				{
					if (number < 20)
					{
						parts.Add(NumberWords[number]);
					}
					else
					{
						int tens = number / 10;
						int ones = number % 10;
						string tensWord = NumberWords[tens * 10];
						if (ones > 0)
						{
							parts.Add(tensWord + " " + NumberWords[ones]);
						}
						else
						{
							parts.Add(tensWord);
						}
					}
				}

				return string.Join(" ", parts);
			}

			/// <summary>
			/// Converts English number words to an integer.
			/// Handles written forms like "Twenty One", "One Hundred Thirty Four", etc.
			/// </summary>
			/// <param name="words">The English words representing a number.</param>
			/// <returns>The integer representation, or null if conversion fails.</returns>
			public static int? WordsToNumbers(string words)
			{
				if (string.IsNullOrWhiteSpace(words))
					return null;

				// Normalize spacing and remove extra whitespace
				words = Regex.Replace(words.Trim(), @"\s+", " ");
				string[] tokens = words.Split(' ', StringSplitOptions.RemoveEmptyEntries);

				int result = 0;
				int current = 0;

				foreach (string token in tokens)
				{
					if (!WordsToNumber.TryGetValue(token, out int value))
						return null; // Invalid token

					if (value >= 1000)
					{
						current += result;
						result = current * value;
						current = 0;
					}
					else if (value == 100)
					{
						current *= value;
					}
					else
					{
						current += value;
					}
				}

				result += current;
				return result >= 0 ? result : null;
			}
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

using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using MySqlConnector;

namespace gaseous_server.Classes
{
	public class Database
	{
		private static int _schema_version { get; set; } = 0;
		public static int schema_version
		{
			get
			{
				//Logging.Log(Logging.LogType.Information, "Database Schema", "Schema version is " + _schema_version);
				return _schema_version;
			}
			set
			{
				//Logging.Log(Logging.LogType.Information, "Database Schema", "Setting schema version " + _schema_version);
				_schema_version = value;
			}
		}

		public Database()
		{

		}

		public Database(databaseType Type, string ConnectionString)
		{
			_ConnectorType = Type;
			_ConnectionString = ConnectionString;
		}

		public enum databaseType
		{
			MySql
		}

		string _ConnectionString = "";

		public string ConnectionString
		{
			get
			{
				return _ConnectionString;
			}
			set
			{
				_ConnectionString = value;
			}
		}

		databaseType? _ConnectorType = null;

		public databaseType? ConnectorType
		{
			get
			{
				return _ConnectorType;
			}
			set
			{
				_ConnectorType = value;
			}
		}

		private static MemoryCache DatabaseMemoryCache = new MemoryCache();

		public void InitDB()
		{
			// load resources
			var assembly = Assembly.GetExecutingAssembly();

			DatabaseMemoryCacheOptions? CacheOptions = new DatabaseMemoryCacheOptions(false);

			Config.DatabaseConfiguration.UpgradeInProgress = true;

			switch (_ConnectorType)
			{
				case databaseType.MySql:
					// check if the database exists first - first run must have permissions to create a database
					string sql = "CREATE DATABASE IF NOT EXISTS `" + Config.DatabaseConfiguration.DatabaseName + "`;";
					Dictionary<string, object> dbDict = new Dictionary<string, object>();
					Logging.LogKey(Logging.LogType.Information, "process.database", "database.creating_database_if_not_exists");
					ExecuteCMD(sql, dbDict, CacheOptions, 30, "server=" + Config.DatabaseConfiguration.HostName + ";port=" + Config.DatabaseConfiguration.Port + ";userid=" + Config.DatabaseConfiguration.UserName + ";password=" + Config.DatabaseConfiguration.Password);

					// check if schema version table is in place - if not, create the schema version table
					sql = "SELECT TABLE_SCHEMA, TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = '" + Config.DatabaseConfiguration.DatabaseName + "' AND TABLE_NAME = 'schema_version';";
					DataTable SchemaVersionPresent = ExecuteCMD(sql, dbDict, CacheOptions);
					if (SchemaVersionPresent.Rows.Count == 0)
					{
						// no schema table present - create it
						Logging.LogKey(Logging.LogType.Information, "process.database", "database.schema_version_table_missing_creating");
						sql = "CREATE TABLE `schema_version` (`schema_version` INT NOT NULL, PRIMARY KEY (`schema_version`)); INSERT INTO `schema_version` (`schema_version`) VALUES (0);";
						ExecuteCMD(sql, dbDict, CacheOptions);
					}

					sql = "SELECT schema_version FROM schema_version;";
					dbDict = new Dictionary<string, object>();
					DataTable SchemaVersion = ExecuteCMD(sql, dbDict, CacheOptions);
					int OuterSchemaVer = (int)SchemaVersion.Rows[0][0];
					if (OuterSchemaVer == 0)
					{
						OuterSchemaVer = 1000;
					}

					for (int i = OuterSchemaVer; i < 10000; i++)
					{
						string resourceName = "gaseous_server.Support.Database.MySQL.gaseous-" + i + ".sql";
						string dbScript = "";

						string[] resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
						if (resources.Contains(resourceName))
						{
							using (Stream stream = assembly.GetManifestResourceStream(resourceName))
							using (StreamReader reader = new StreamReader(stream))
							{
								dbScript = reader.ReadToEnd();

								// apply script
								sql = "SELECT schema_version FROM schema_version;";
								dbDict = new Dictionary<string, object>();
								SchemaVersion = ExecuteCMD(sql, dbDict, CacheOptions);
								if (SchemaVersion.Rows.Count == 0)
								{
									// something is broken here... where's the table?
									Logging.LogKey(Logging.LogType.Critical, "process.database", "database.schema_table_missing_should_not_happen");
									throw new Exception("schema_version table is missing!");
								}
								else
								{
									int SchemaVer = (int)SchemaVersion.Rows[0][0];
									Logging.LogKey(Logging.LogType.Information, "process.database", "database.schema_version_is", null, new[] { SchemaVer.ToString() });
									// update schema version variable
									Database.schema_version = SchemaVer;
									if (SchemaVer < i)
									{
										try
										{
											// run pre-upgrade code
											DatabaseMigration.PreUpgradeScript(i, _ConnectorType);

											// apply schema!
											Logging.LogKey(Logging.LogType.Information, "process.database", "database.updating_schema_to_version", null, new[] { i.ToString() });
											ExecuteCMD(dbScript, dbDict, 100);

											// increment schema version
											sql = "UPDATE schema_version SET schema_version=@schemaver";
											dbDict = new Dictionary<string, object>();
											dbDict.Add("schemaver", i);
											ExecuteCMD(sql, dbDict, CacheOptions);

											// run post-upgrade code
											DatabaseMigration.PostUpgradeScript(i, _ConnectorType);

											// update schema version variable
											Database.schema_version = i;
										}
										catch (Exception ex)
										{
											Logging.LogKey(Logging.LogType.Critical, "process.database", "database.schema_upgrade_failed_unable_to_continue", null, null, ex);
											System.Environment.Exit(1);
										}
									}
								}
							}
						}
					}
					Logging.LogKey(Logging.LogType.Information, "process.database", "database.setup_complete");
					break;
			}
		}

		/// <summary>
		/// Splits a SQL script into individual statements, ignoring semicolons inside strings and comments.
		/// </summary>
		private static List<string> SplitSqlStatements(string sqlScript)
		{
			var statements = new List<string>();
			var sb = new System.Text.StringBuilder();
			bool inSingleQuote = false;
			bool inDoubleQuote = false;
			bool inLineComment = false;
			bool inBlockComment = false;

			for (int i = 0; i < sqlScript.Length; i++)
			{
				char c = sqlScript[i];
				char next = i < sqlScript.Length - 1 ? sqlScript[i + 1] : '\0';

				// Handle entering/exiting comments
				if (!inSingleQuote && !inDoubleQuote)
				{
					if (!inBlockComment && c == '-' && next == '-')
					{
						inLineComment = true;
					}
					else if (!inBlockComment && c == '/' && next == '*')
					{
						inBlockComment = true;
						i++; // skip '*'
						continue;
					}
					else if (inBlockComment && c == '*' && next == '/')
					{
						inBlockComment = false;
						i++; // skip '/'
						continue;
					}
				}

				if (inLineComment)
				{
					if (c == '\n')
					{
						inLineComment = false;
						sb.Append(c);
					}
					continue;
				}
				if (inBlockComment)
				{
					continue;
				}

				// Handle entering/exiting string literals
				if (c == '\'' && !inDoubleQuote)
				{
					inSingleQuote = !inSingleQuote;
				}
				else if (c == '"' && !inSingleQuote)
				{
					inDoubleQuote = !inDoubleQuote;
				}

				// Split on semicolon if not inside a string
				if (c == ';' && !inSingleQuote && !inDoubleQuote)
				{
					var statement = sb.ToString().Trim();
					if (!string.IsNullOrEmpty(statement))
						statements.Add(statement);
					sb.Clear();
				}
				else
				{
					sb.Append(c);
				}
			}

			// Add any remaining statement
			var last = sb.ToString().Trim();
			if (!string.IsNullOrEmpty(last))
				statements.Add(last);

			return statements;
		}

		#region Synchronous Database Access
		public DataTable ExecuteCMD(string Command)
		{
			DatabaseMemoryCacheOptions? CacheOptions = null;

			Dictionary<string, object> dbDict = new Dictionary<string, object>();
			return _ExecuteCMD(Command, dbDict, CacheOptions, 30, "");
		}

		public DataTable ExecuteCMD(string Command, DatabaseMemoryCacheOptions? CacheOptions)
		{
			Dictionary<string, object> dbDict = new Dictionary<string, object>();
			return _ExecuteCMD(Command, dbDict, CacheOptions, 30, "");
		}

		public DataTable ExecuteCMD(string Command, Dictionary<string, object> Parameters)
		{
			DatabaseMemoryCacheOptions? CacheOptions = null;

			return _ExecuteCMD(Command, Parameters, CacheOptions, 30, "");
		}

		public DataTable ExecuteCMD(string Command, Dictionary<string, object> Parameters, DatabaseMemoryCacheOptions? CacheOptions)
		{
			return _ExecuteCMD(Command, Parameters, CacheOptions, 30, "");
		}

		public DataTable ExecuteCMD(string Command, Dictionary<string, object> Parameters, int Timeout = 30, string ConnectionString = "")
		{
			DatabaseMemoryCacheOptions? CacheOptions = null;

			return _ExecuteCMD(Command, Parameters, CacheOptions, Timeout, ConnectionString);
		}

		public DataTable ExecuteCMD(string Command, Dictionary<string, object> Parameters, DatabaseMemoryCacheOptions? CacheOptions, int Timeout = 30, string ConnectionString = "")
		{
			return _ExecuteCMD(Command, Parameters, CacheOptions, Timeout, ConnectionString);
		}

		public List<Dictionary<string, object>> ExecuteCMDDict(string Command)
		{
			DatabaseMemoryCacheOptions? CacheOptions = null;

			Dictionary<string, object> dbDict = new Dictionary<string, object>();
			return _ExecuteCMDDict(Command, dbDict, CacheOptions, 30, "");
		}

		public List<Dictionary<string, object>> ExecuteCMDDict(string Command, DatabaseMemoryCacheOptions? CacheOptions)
		{
			Dictionary<string, object> dbDict = new Dictionary<string, object>();
			return _ExecuteCMDDict(Command, dbDict, CacheOptions, 30, "");
		}

		public List<Dictionary<string, object>> ExecuteCMDDict(string Command, Dictionary<string, object> Parameters)
		{
			DatabaseMemoryCacheOptions? CacheOptions = null;

			return _ExecuteCMDDict(Command, Parameters, CacheOptions, 30, "");
		}

		public List<Dictionary<string, object>> ExecuteCMDDict(string Command, Dictionary<string, object> Parameters, DatabaseMemoryCacheOptions? CacheOptions)
		{
			return _ExecuteCMDDict(Command, Parameters, CacheOptions, 30, "");
		}

		public List<Dictionary<string, object>> ExecuteCMDDict(string Command, Dictionary<string, object> Parameters, int Timeout = 30, string ConnectionString = "")
		{
			DatabaseMemoryCacheOptions? CacheOptions = null;

			return _ExecuteCMDDict(Command, Parameters, CacheOptions, Timeout, ConnectionString);
		}

		public List<Dictionary<string, object>> ExecuteCMDDict(string Command, Dictionary<string, object> Parameters, DatabaseMemoryCacheOptions? CacheOptions, int Timeout = 30, string ConnectionString = "")
		{
			return _ExecuteCMDDict(Command, Parameters, CacheOptions, Timeout, ConnectionString);
		}
		#endregion Synchronous Database Access

		#region Asynchronous Database Access
		public async Task<DataTable> ExecuteCMDAsync(string Command)
		{
			DatabaseMemoryCacheOptions? CacheOptions = null;

			Dictionary<string, object> dbDict = new Dictionary<string, object>();
			return _ExecuteCMD(Command, dbDict, CacheOptions, 30, "");
		}

		public async Task<DataTable> ExecuteCMDAsync(string Command, DatabaseMemoryCacheOptions? CacheOptions)
		{
			Dictionary<string, object> dbDict = new Dictionary<string, object>();
			return _ExecuteCMD(Command, dbDict, CacheOptions, 30, "");
		}

		public async Task<DataTable> ExecuteCMDAsync(string Command, Dictionary<string, object> Parameters)
		{
			DatabaseMemoryCacheOptions? CacheOptions = null;

			return _ExecuteCMD(Command, Parameters, CacheOptions, 30, "");
		}

		public async Task<DataTable> ExecuteCMDAsync(string Command, Dictionary<string, object> Parameters, DatabaseMemoryCacheOptions? CacheOptions)
		{
			return _ExecuteCMD(Command, Parameters, CacheOptions, 30, "");
		}

		public async Task<DataTable> ExecuteCMDAsync(string Command, Dictionary<string, object> Parameters, int Timeout = 30, string ConnectionString = "")
		{
			DatabaseMemoryCacheOptions? CacheOptions = null;

			return _ExecuteCMD(Command, Parameters, CacheOptions, Timeout, ConnectionString);
		}

		public async Task<DataTable> ExecuteCMDAsync(string Command, Dictionary<string, object> Parameters, DatabaseMemoryCacheOptions? CacheOptions, int Timeout = 30, string ConnectionString = "")
		{
			return _ExecuteCMD(Command, Parameters, CacheOptions, Timeout, ConnectionString);
		}

		public async Task<List<Dictionary<string, object>>> ExecuteCMDDictAsync(string Command)
		{
			DatabaseMemoryCacheOptions? CacheOptions = null;

			Dictionary<string, object> dbDict = new Dictionary<string, object>();
			return _ExecuteCMDDict(Command, dbDict, CacheOptions, 30, "");
		}

		public async Task<List<Dictionary<string, object>>> ExecuteCMDDictAsync(string Command, DatabaseMemoryCacheOptions? CacheOptions)
		{
			Dictionary<string, object> dbDict = new Dictionary<string, object>();
			return _ExecuteCMDDict(Command, dbDict, CacheOptions, 30, "");
		}

		public async Task<List<Dictionary<string, object>>> ExecuteCMDDictAsync(string Command, Dictionary<string, object> Parameters)
		{
			DatabaseMemoryCacheOptions? CacheOptions = null;

			return _ExecuteCMDDict(Command, Parameters, CacheOptions, 30, "");
		}

		public async Task<List<Dictionary<string, object>>> ExecuteCMDDictAsync(string Command, Dictionary<string, object> Parameters, DatabaseMemoryCacheOptions? CacheOptions)
		{
			return _ExecuteCMDDict(Command, Parameters, CacheOptions, 30, "");
		}

		public async Task<List<Dictionary<string, object>>> ExecuteCMDDictAsync(string Command, Dictionary<string, object> Parameters, int Timeout = 30, string ConnectionString = "")
		{
			DatabaseMemoryCacheOptions? CacheOptions = null;

			return _ExecuteCMDDict(Command, Parameters, CacheOptions, Timeout, ConnectionString);
		}

		public async Task<List<Dictionary<string, object>>> ExecuteCMDDictAsync(string Command, Dictionary<string, object> Parameters, DatabaseMemoryCacheOptions? CacheOptions, int Timeout = 30, string ConnectionString = "")
		{
			return _ExecuteCMDDict(Command, Parameters, CacheOptions, Timeout, ConnectionString);
		}
		#endregion Asynchronous Database Access


		private List<Dictionary<string, object>> _ExecuteCMDDict(string Command, Dictionary<string, object> Parameters, DatabaseMemoryCacheOptions? CacheOptions, int Timeout = 30, string ConnectionString = "")
		{
			DataTable dataTable = _ExecuteCMD(Command, Parameters, CacheOptions, Timeout, ConnectionString);

			// convert datatable to dictionary
			List<Dictionary<string, object?>> rows = new List<Dictionary<string, object?>>();

			foreach (DataRow dataRow in dataTable.Rows)
			{
				Dictionary<string, object?> row = new Dictionary<string, object?>();
				for (int i = 0; i < dataRow.Table.Columns.Count; i++)
				{
					string columnName = dataRow.Table.Columns[i].ColumnName;
					if (dataRow[i] == System.DBNull.Value)
					{
						row.Add(columnName, null);
					}
					else
					{
						row.Add(columnName, dataRow[i].ToString());
					}
				}
				rows.Add(row);
			}

			return rows;
		}

		private DataTable _ExecuteCMD(string Command, Dictionary<string, object> Parameters, DatabaseMemoryCacheOptions? CacheOptions, int Timeout = 30, string ConnectionString = "")
		{
			string CacheKey = Command + string.Join(";", Parameters.Select(x => string.Join("=", x.Key, x.Value)));
			if (CacheOptions?.CacheKey != null)
			{
				CacheKey = CacheOptions.CacheKey;
			}

			if (CacheOptions is object && CacheOptions.CacheEnabled)
			{
				object? CachedData = DatabaseMemoryCache.GetCacheObject(CacheKey);
				if (CachedData is object)
				{
					return (DataTable)CachedData;
				}
			}

			// purge cache if command contains "INSERT", "UPDATE", "DELETE", or "ALTER"
			if (
				Command.Contains("INSERT", StringComparison.InvariantCultureIgnoreCase) ||
				Command.Contains("UPDATE", StringComparison.InvariantCultureIgnoreCase) ||
				Command.Contains("DELETE", StringComparison.InvariantCultureIgnoreCase) ||
				Command.Contains("ALTER", StringComparison.InvariantCultureIgnoreCase)
				)
			{
				// exclude logging events from purging the cache
				if (!Command.StartsWith("INSERT INTO SERVERLOGS", StringComparison.InvariantCultureIgnoreCase))
				{
					DatabaseMemoryCache.ClearCache();
				}
			}

			if (ConnectionString == "") { ConnectionString = _ConnectionString; }
			switch (_ConnectorType)
			{
				case databaseType.MySql:
					MySQLServerConnector conn = new MySQLServerConnector(ConnectionString);
					DataTable RetTable = conn.ExecCMD(Command, Parameters, Timeout);
					if (CacheOptions is object && CacheOptions.CacheEnabled)
					{
						DatabaseMemoryCache.SetCacheObject(CacheKey, RetTable, CacheOptions.ExpirationSeconds);
					}
					return RetTable;
				default:
					return new DataTable();
			}
		}

		public int ExecuteNonQuery(string Command)
		{
			Dictionary<string, object> dbDict = new Dictionary<string, object>();
			return _ExecuteNonQuery(Command, dbDict, 30, "");
		}

		public int ExecuteNonQuery(string Command, Dictionary<string, object> Parameters)
		{
			return _ExecuteNonQuery(Command, Parameters, 30, "");
		}

		public int ExecuteNonQuery(string Command, Dictionary<string, object> Parameters, int Timeout = 30, string ConnectionString = "")
		{
			return _ExecuteNonQuery(Command, Parameters, Timeout, ConnectionString);
		}

		private int _ExecuteNonQuery(string Command, Dictionary<string, object> Parameters, int Timeout = 30, string ConnectionString = "")
		{
			if (ConnectionString == "") { ConnectionString = _ConnectionString; }
			switch (_ConnectorType)
			{
				case databaseType.MySql:
					MySQLServerConnector conn = new MySQLServerConnector(ConnectionString);
					int retVal = conn.ExecNonQuery(Command, Parameters, Timeout);
					return retVal;
				default:
					return 0;
			}
		}

		public void ExecuteTransactionCMD(List<SQLTransactionItem> CommandList, int Timeout = 60)
		{
			object conn;
			switch (_ConnectorType)
			{
				case databaseType.MySql:
					{
						var commands = new List<Dictionary<string, object>>();
						foreach (SQLTransactionItem CommandItem in CommandList)
						{
							var nCmd = new Dictionary<string, object>();
							nCmd.Add("sql", CommandItem.SQLCommand);
							nCmd.Add("values", CommandItem.Parameters);
							commands.Add(nCmd);
						}

						conn = new MySQLServerConnector(_ConnectionString);
						((MySQLServerConnector)conn).TransactionExecCMD(commands, Timeout);
						break;
					}
			}
		}

		public int GetDatabaseSchemaVersion()
		{
			switch (_ConnectorType)
			{
				case databaseType.MySql:
					string sql = "SELECT schema_version FROM schema_version;";
					DataTable SchemaVersion = ExecuteCMD(sql);
					if (SchemaVersion.Rows.Count == 0)
					{
						return 0;
					}
					else
					{
						return (int)SchemaVersion.Rows[0][0];
					}

				default:
					return 0;

			}
		}

		public bool TestConnection()
		{
			switch (_ConnectorType)
			{
				case databaseType.MySql:
					MySQLServerConnector conn = new MySQLServerConnector(_ConnectionString);
					return conn.TestConnection();
				default:
					return false;
			}
		}

		public class SQLTransactionItem
		{
			public SQLTransactionItem()
			{

			}

			public SQLTransactionItem(string SQLCommand, Dictionary<string, object> Parameters)
			{
				this.SQLCommand = SQLCommand;
				this.Parameters = Parameters;
			}

			public string? SQLCommand;
			public Dictionary<string, object>? Parameters = new Dictionary<string, object>();
		}

		private partial class MySQLServerConnector
		{
			private string DBConn = "";

			public MySQLServerConnector(string ConnectionString)
			{
				DBConn = ConnectionString;
			}

			public DataTable ExecCMD(string SQL, Dictionary<string, object> Parameters, int Timeout)
			{
				DataTable RetTable = new DataTable();

				Logging.LogKey(Logging.LogType.Debug, "process.database", "database.connecting_to_database", null, null, null, true);
				using (MySqlConnection conn = new MySqlConnection(DBConn))
				{
					conn.Open();

					MySqlCommand cmd = new MySqlCommand
					{
						Connection = conn,
						CommandText = SQL,
						CommandTimeout = Timeout
					};

					foreach (string Parameter in Parameters.Keys)
					{
						cmd.Parameters.AddWithValue(Parameter, Parameters[Parameter]);
					}

					try
					{
						Logging.LogKey(Logging.LogType.Debug, "process.database", "database.executing_sql", null, new[] { SQL }, null, true);
						if (Parameters.Count > 0)
						{
							string dictValues = string.Join(";", Parameters.Select(x => string.Join("=", x.Key, x.Value)));
							Logging.LogKey(Logging.LogType.Debug, "process.database", "database.parameters", null, new[] { dictValues }, null, true);
						}
						RetTable.Load(cmd.ExecuteReader());
					}
					catch (Exception ex)
					{
						Logging.LogKey(Logging.LogType.Critical, "process.database", "database.error_executing_sql", null, new[] { SQL }, ex);
						Trace.WriteLine("Error executing " + SQL);
						Trace.WriteLine("Full exception: " + ex.ToString());
					}

					Logging.LogKey(Logging.LogType.Debug, "process.database", "database.closing_database_connection", null, null, null, true);
					conn.Close();
				}

				return RetTable;
			}

			public int ExecNonQuery(string SQL, Dictionary<string, object> Parameters, int Timeout)
			{
				int result = 0;

				Logging.LogKey(Logging.LogType.Debug, "process.database", "database.connecting_to_database", null, null, null, true);
				using (MySqlConnection conn = new MySqlConnection(DBConn))
				{
					conn.Open();

					MySqlCommand cmd = new MySqlCommand
					{
						Connection = conn,
						CommandText = SQL,
						CommandTimeout = Timeout
					};

					foreach (string Parameter in Parameters.Keys)
					{
						cmd.Parameters.AddWithValue(Parameter, Parameters[Parameter]);
					}

					try
					{
						Logging.LogKey(Logging.LogType.Debug, "process.database", "database.executing_sql", null, new[] { SQL }, null, true);
						if (Parameters.Count > 0)
						{
							string dictValues = string.Join(";", Parameters.Select(x => string.Join("=", x.Key, x.Value)));
							Logging.LogKey(Logging.LogType.Debug, "process.database", "database.parameters", null, new[] { dictValues }, null, true);
						}
						result = cmd.ExecuteNonQuery();
					}
					catch (Exception ex)
					{
						Logging.LogKey(Logging.LogType.Critical, "process.database", "database.error_executing_sql", null, new[] { SQL }, ex);
						Trace.WriteLine("Error executing " + SQL);
						Trace.WriteLine("Full exception: " + ex.ToString());
					}

					Logging.LogKey(Logging.LogType.Debug, "process.database", "database.closing_database_connection", null, null, null, true);
					conn.Close();
				}

				return result;
			}

			public void TransactionExecCMD(List<Dictionary<string, object>> Parameters, int Timeout)
			{
				using (MySqlConnection conn = new MySqlConnection(DBConn))
				{
					conn.Open();
					var command = conn.CreateCommand();
					MySqlTransaction transaction;
					transaction = conn.BeginTransaction();
					command.Connection = conn;
					command.Transaction = transaction;
					foreach (Dictionary<string, object> Parameter in Parameters)
					{
						var cmd = buildcommand(conn, Parameter["sql"].ToString(), (Dictionary<string, object>)Parameter["values"], Timeout);
						cmd.Transaction = transaction;
						cmd.ExecuteNonQuery();
					}

					transaction.Commit();
					conn.Close();
				}
			}

			private MySqlCommand buildcommand(MySqlConnection Conn, string SQL, Dictionary<string, object> Parameters, int Timeout)
			{
				var cmd = new MySqlCommand();
				cmd.Connection = Conn;
				cmd.CommandText = SQL;
				cmd.CommandTimeout = Timeout;
				{
					var withBlock = cmd.Parameters;
					if (Parameters is object)
					{
						if (Parameters.Count > 0)
						{
							foreach (string param in Parameters.Keys)
								withBlock.AddWithValue(param, Parameters[param]);
						}
					}
				}

				return cmd;
			}

			public bool TestConnection()
			{
				using (MySqlConnection conn = new MySqlConnection(DBConn))
				{
					try
					{
						conn.Open();
						conn.Close();
						return true;
					}
					catch
					{
						return false;
					}
				}
			}
		}
	}
}


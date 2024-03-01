using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
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

		public void InitDB()
		{
            // load resources
            var assembly = Assembly.GetExecutingAssembly();

            switch (_ConnectorType)
			{
				case databaseType.MySql:
					// check if the database exists first - first run must have permissions to create a database
					string sql = "CREATE DATABASE IF NOT EXISTS `" + Config.DatabaseConfiguration.DatabaseName + "`;";
					Dictionary<string, object> dbDict = new Dictionary<string, object>();
					Logging.Log(Logging.LogType.Information, "Database", "Creating database if it doesn't exist");
					ExecuteCMD(sql, dbDict, 30, "server=" + Config.DatabaseConfiguration.HostName + ";port=" + Config.DatabaseConfiguration.Port + ";userid=" + Config.DatabaseConfiguration.UserName + ";password=" + Config.DatabaseConfiguration.Password);

					// check if schema version table is in place - if not, create the schema version table
					sql = "SELECT TABLE_SCHEMA, TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = '" + Config.DatabaseConfiguration.DatabaseName + "' AND TABLE_NAME = 'schema_version';";
					DataTable SchemaVersionPresent = ExecuteCMD(sql, dbDict);
					if (SchemaVersionPresent.Rows.Count == 0)
					{
                        // no schema table present - create it
                        Logging.Log(Logging.LogType.Information, "Database", "Schema version table doesn't exist. Creating it.");
                        sql = "CREATE TABLE `schema_version` (`schema_version` INT NOT NULL, PRIMARY KEY (`schema_version`)); INSERT INTO `schema_version` (`schema_version`) VALUES (0);";
						ExecuteCMD(sql, dbDict);
					}

					sql = "SELECT schema_version FROM schema_version;";
					dbDict = new Dictionary<string, object>();
					DataTable SchemaVersion = ExecuteCMD(sql, dbDict);
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
								SchemaVersion = ExecuteCMD(sql, dbDict);
								if (SchemaVersion.Rows.Count == 0)
								{
                                    // something is broken here... where's the table?
                                    Logging.Log(Logging.LogType.Critical, "Database", "Schema table missing! This shouldn't happen!");
                                    throw new Exception("schema_version table is missing!");
								}
								else
								{
									int SchemaVer = (int)SchemaVersion.Rows[0][0];
                                    Logging.Log(Logging.LogType.Information, "Database", "Schema version is " + SchemaVer);
									// update schema version variable
									Database.schema_version = SchemaVer;
                                    if (SchemaVer < i)
									{
										try
										{
											// run pre-upgrade code
											DatabaseMigration.PreUpgradeScript(i, _ConnectorType);
											
											// apply schema!
											Logging.Log(Logging.LogType.Information, "Database", "Updating schema to version " + i);
											ExecuteCMD(dbScript, dbDict, 180);

											sql = "UPDATE schema_version SET schema_version=@schemaver";
											dbDict = new Dictionary<string, object>();
											dbDict.Add("schemaver", i);
											ExecuteCMD(sql, dbDict);

											// run post-upgrade code
											DatabaseMigration.PostUpgradeScript(i, _ConnectorType);
										}
										catch (Exception ex)
										{
											Logging.Log(Logging.LogType.Critical, "Database", "Schema upgrade failed! Unable to continue.", ex);
											System.Environment.Exit(1);
										}
									}
								}
							}
						}
                    }
                    Logging.Log(Logging.LogType.Information, "Database", "Database setup complete");
                    break;
			}
		}

        public DataTable ExecuteCMD(string Command)
		{
			Dictionary<string, object> dbDict = new Dictionary<string, object>();
			return _ExecuteCMD(Command, dbDict, 30, "");
		}

        public DataTable ExecuteCMD(string Command, Dictionary<string, object> Parameters)
        {
            return _ExecuteCMD(Command, Parameters, 30, "");
        }

        public DataTable ExecuteCMD(string Command, Dictionary<string, object> Parameters, int Timeout = 30, string ConnectionString = "")
        {
			return _ExecuteCMD(Command, Parameters, Timeout, ConnectionString);
        }

		public List<Dictionary<string, object>> ExecuteCMDDict(string Command)
		{
			Dictionary<string, object> dbDict = new Dictionary<string, object>();
			return _ExecuteCMDDict(Command, dbDict, 30, "");
		}

        public List<Dictionary<string, object>> ExecuteCMDDict(string Command, Dictionary<string, object> Parameters)
        {
            return _ExecuteCMDDict(Command, Parameters, 30, "");
        }

        public List<Dictionary<string, object>> ExecuteCMDDict(string Command, Dictionary<string, object> Parameters, int Timeout = 30, string ConnectionString = "")
        {
			return _ExecuteCMDDict(Command, Parameters, Timeout, ConnectionString);
        }

		private List<Dictionary<string, object>> _ExecuteCMDDict(string Command, Dictionary<string, object> Parameters, int Timeout = 30, string ConnectionString = "")
		{
			DataTable dataTable = _ExecuteCMD(Command, Parameters, Timeout, ConnectionString);

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

        private DataTable _ExecuteCMD(string Command, Dictionary<string, object> Parameters, int Timeout = 30, string ConnectionString = "")
        {
            if (ConnectionString == "") { ConnectionString = _ConnectionString; }
            switch (_ConnectorType)
            {
                case databaseType.MySql:
                    MySQLServerConnector conn = new MySQLServerConnector(ConnectionString);
                    return (DataTable)conn.ExecCMD(Command, Parameters, Timeout);
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

                Logging.Log(Logging.LogType.Debug, "Database", "Connecting to database", null, true);
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
						Logging.Log(Logging.LogType.Debug, "Database", "Executing sql: '" + SQL + "'", null, true);
						if (Parameters.Count > 0)
						{
							string dictValues = string.Join(";", Parameters.Select(x => string.Join("=", x.Key, x.Value)));
							Logging.Log(Logging.LogType.Debug, "Database", "Parameters: " + dictValues, null, true);
						}
						RetTable.Load(cmd.ExecuteReader());
					} catch (Exception ex) {
						Logging.Log(Logging.LogType.Critical, "Database", "Error while executing '" + SQL + "'", ex);
						Trace.WriteLine("Error executing " + SQL);
						Trace.WriteLine("Full exception: " + ex.ToString());
					}

					Logging.Log(Logging.LogType.Debug, "Database", "Closing database connection", null, true);
					conn.Close();
				}

				return RetTable;
			}

			public int ExecNonQuery(string SQL, Dictionary< string, object> Parameters, int Timeout)
			{
				int result = 0;

                Logging.Log(Logging.LogType.Debug, "Database", "Connecting to database", null, true);
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
						Logging.Log(Logging.LogType.Debug, "Database", "Executing sql: '" + SQL + "'", null, true);
						if (Parameters.Count > 0)
						{
							string dictValues = string.Join(";", Parameters.Select(x => string.Join("=", x.Key, x.Value)));
							Logging.Log(Logging.LogType.Debug, "Database", "Parameters: " + dictValues, null, true);
						}
						result = cmd.ExecuteNonQuery();
					} catch (Exception ex) {
						Logging.Log(Logging.LogType.Critical, "Database", "Error while executing '" + SQL + "'", ex);
						Trace.WriteLine("Error executing " + SQL);
						Trace.WriteLine("Full exception: " + ex.ToString());
					}

					Logging.Log(Logging.LogType.Debug, "Database", "Closing database connection", null, true);
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


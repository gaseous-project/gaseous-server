using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;
using MySql.Data.MySqlClient;

namespace gaseous_tools
{
	public class Database
	{
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
					sql = "SELECT TABLE_SCHEMA, TABLE_NAME FROM information_schema.TABLES WHERE TABLE_SCHEMA = 'gaseous' AND TABLE_NAME = 'schema_version';";
					DataTable SchemaVersionPresent = ExecuteCMD(sql, dbDict);
					if (SchemaVersionPresent.Rows.Count == 0)
					{
                        // no schema table present - create it
                        Logging.Log(Logging.LogType.Information, "Database", "Schema version table doesn't exist. Creating it.");
                        sql = "CREATE TABLE `schema_version` (`schema_version` INT NOT NULL, PRIMARY KEY (`schema_version`)); INSERT INTO `schema_version` (`schema_version`) VALUES (0);";
						ExecuteCMD(sql, dbDict);
					}

                    for (int i = 1000; i < 10000; i++)
					{
						string resourceName = "gaseous_tools.Database.MySQL.gaseous-" + i + ".sql";
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
								DataTable SchemaVersion = ExecuteCMD(sql, dbDict);
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
                                    if (SchemaVer < i)
									{
                                        // apply schema!
                                        Logging.Log(Logging.LogType.Information, "Database", "Schema update available - applying");
                                        ExecuteCMD(dbScript, dbDict);

										sql = "UPDATE schema_version SET schema_version=@schemaver";
										dbDict = new Dictionary<string, object>();
										dbDict.Add("schemaver", i);
										ExecuteCMD(sql, dbDict);
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

                Logging.Log(Logging.LogType.Debug, "Database", "Connecting to database");
                MySqlConnection conn = new MySqlConnection(DBConn);
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
                    Logging.Log(Logging.LogType.Debug, "Database", "Executing sql: '" + SQL + "'");
					if (Parameters.Count > 0)
					{
						string dictValues = string.Join(";", Parameters.Select(x => string.Join("=", x.Key, x.Value)));
						Logging.Log(Logging.LogType.Debug, "Database", "Parameters: " + dictValues);
					}
                    RetTable.Load(cmd.ExecuteReader());
				} catch (Exception ex) {
					Logging.Log(Logging.LogType.Critical, "Database", "Error while executing '" + SQL + "'", ex);
					Trace.WriteLine("Error executing " + SQL);
					Trace.WriteLine("Full exception: " + ex.ToString());
				}

				Logging.Log(Logging.LogType.Debug, "Database", "Closing database connection");
				conn.Close();

				return RetTable;
			}
		}
	}
}


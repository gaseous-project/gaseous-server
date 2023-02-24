using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
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


        public DataTable ExecuteCMD(string Command, Dictionary<string, object> Parameters, int Timeout = 30)
        {
            switch (_ConnectorType)
			{
				case databaseType.MySql:
                    MySQLServerConnector conn = new MySQLServerConnector(_ConnectionString);
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
					RetTable.Load(cmd.ExecuteReader());
				} catch (Exception ex) {
					Trace.WriteLine("Error executing " + SQL);
					Trace.WriteLine("Full exception: " + ex.ToString());
				}

				conn.Close();

				return RetTable;
			}
		}
	}
}


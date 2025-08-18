using gaseous_server.Classes;
using System.Collections.Generic;
using System.Data;

namespace Authentication
{
    /// <summary>
    /// Access to UserAuthenticatorKeys table (MariaDB/MySQL)
    /// </summary>
    public class UserAuthenticatorKeysTable
    {
        private readonly Database _database;

        public UserAuthenticatorKeysTable(Database database)
        {
            _database = database;
        }

        /// <summary>
        /// Get the authenticator key for a user (null if none).
        /// </summary>
        public string? GetKey(string userId)
        {
            const string sql = "SELECT AuthenticatorKey FROM UserAuthenticatorKeys WHERE UserId=@uid";
            var dict = new Dictionary<string, object> { { "uid", userId } };
            DataTable dt = _database.ExecuteCMD(sql, dict);
            if (dt.Rows.Count == 0) return null;
            return (string)dt.Rows[0][0];
        }

        /// <summary>
        /// Upsert the authenticator key for a user.
        /// </summary>
        public void SetKey(string userId, string key)
        {
            const string sql = "REPLACE INTO UserAuthenticatorKeys (UserId, AuthenticatorKey) VALUES (@uid, @key)";
            var dict = new Dictionary<string, object> { { "uid", userId }, { "key", key } };
            _database.ExecuteNonQuery(sql, dict);
        }

        /// <summary>
        /// Remove the authenticator key for a user.
        /// </summary>
        public void DeleteKey(string userId)
        {
            const string sql = "DELETE FROM UserAuthenticatorKeys WHERE UserId=@uid";
            var dict = new Dictionary<string, object> { { "uid", userId } };
            _database.ExecuteNonQuery(sql, dict);
        }
    }
}

using gaseous_server.Classes;
using System.Collections.Generic;
using System.Data;

namespace Authentication
{
    /// <summary>
    /// Access to UserRecoveryCodes table (MariaDB/MySQL)
    /// </summary>
    public class UserRecoveryCodesTable
    {
        private readonly Database _database;

        /// <summary>
        /// Ctor with database dependency.
        /// </summary>
        public UserRecoveryCodesTable(Database database)
        {
            _database = database;
        }

        /// <summary>
        /// Count codes for a user.
        /// </summary>
        public int CountCodes(string userId)
        {
            const string sql = "SELECT COUNT(*) FROM UserRecoveryCodes WHERE UserId=@uid";
            var dict = new Dictionary<string, object> { { "uid", userId } };
            DataTable dt = _database.ExecuteCMD(sql, dict);
            if (dt.Rows.Count == 0) return 0;
            var val = dt.Rows[0][0]?.ToString();
            return int.TryParse(val, out var n) ? n : 0;
        }

        /// <summary>
        /// Delete a matching code and return true if removed.
        /// </summary>
        public bool RedeemCode(string userId, string codeHash)
        {
            const string delSql = "DELETE FROM UserRecoveryCodes WHERE UserId=@uid AND CodeHash=@code";
            var dict = new Dictionary<string, object> { { "uid", userId }, { "code", codeHash } };
            var affected = _database.ExecuteNonQuery(delSql, dict);
            return affected > 0;
        }

        /// <summary>
        /// Replace all codes for a user with a new set (hashed).
        /// </summary>
        public void ReplaceCodes(string userId, IEnumerable<string> codeHashes)
        {
            // Execute delete + inserts atomically to avoid partial state.
            var txItems = new List<Database.SQLTransactionItem>();

            // clear existing
            txItems.Add(new Database.SQLTransactionItem(
                "DELETE FROM UserRecoveryCodes WHERE UserId=@uid",
                new Dictionary<string, object> { { "uid", userId } }
            ));

            foreach (var code in codeHashes)
            {
                txItems.Add(new Database.SQLTransactionItem(
                    "INSERT INTO UserRecoveryCodes (UserId, CodeHash) VALUES (@uid, @code)",
                    new Dictionary<string, object> { { "uid", userId }, { "code", code } }
                ));
            }

            _database.ExecuteTransactionCMD(txItems);
        }
    }
}

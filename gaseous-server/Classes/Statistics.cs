using System.Data;
using System.Text;
using gaseous_server.Models;

namespace gaseous_server.Classes
{
    public class Statistics
    {
        public StatisticsModel RecordSession(Guid SessionId, long GameId, long PlatformId, long RomId, bool IsMediaGroup, string UserId)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql;
            Dictionary<string, object> dbDict = new Dictionary<string, object>{
                { "userid", UserId },
                { "gameid", GameId },
                { "platformid", PlatformId },
                { "romid", RomId },
                { "ismediagroup", IsMediaGroup }
            };

            // update last played rom id
            sql = "INSERT INTO User_RecentPlayedRoms (UserId, GameId, PlatformId, RomId, IsMediaGroup) VALUES (@userid, @gameid, @platformid, @romid, @ismediagroup) ON DUPLICATE KEY UPDATE RomId = @romid, IsMediaGroup = @ismediagroup;";
            db.ExecuteNonQuery(sql, dbDict);

            // update sessions

            if (SessionId == Guid.Empty)
            {
                // new session required
                SessionId = Guid.NewGuid();

                sql = "INSERT INTO UserTimeTracking (GameId, UserId, SessionId, SessionTime, SessionLength, PlatformId, IsMediaGroup, RomId) VALUES (@gameid, @userid, @sessionid, @sessiontime, @sessionlength, @platformid, @ismediagroup, @romid);";
                dbDict = new Dictionary<string, object>{
                    { "gameid", GameId },
                    { "userid", UserId },
                    { "sessionid", SessionId },
                    { "sessiontime", DateTime.UtcNow },
                    { "sessionlength", 1 },
                    { "platformid", PlatformId },
                    { "ismediagroup", IsMediaGroup },
                    { "romid", RomId }
                };

                db.ExecuteNonQuery(sql, dbDict);

                return new StatisticsModel
                {
                    GameId = GameId,
                    SessionId = SessionId,
                    SessionStart = (DateTime)dbDict["sessiontime"],
                    SessionLength = (int)dbDict["sessionlength"]
                };
            }
            else
            {
                // update existing session
                sql = "UPDATE UserTimeTracking SET SessionLength = SessionLength + @sessionlength WHERE GameId = @gameid AND UserId = @userid AND SessionId = @sessionid;";
                dbDict = new Dictionary<string, object>{
                    { "gameid", GameId },
                    { "userid", UserId },
                    { "sessionid", SessionId },
                    { "sessionlength", 1 }
                };

                db.ExecuteNonQuery(sql, dbDict);

                sql = "SELECT * FROM UserTimeTracking WHERE GameId = @gameid AND UserId = @userid AND SessionId = @sessionid;";
                DataTable data = db.ExecuteCMD(sql, dbDict);

                return new StatisticsModel
                {
                    GameId = (long)data.Rows[0]["GameId"],
                    SessionId = Guid.Parse(data.Rows[0]["SessionId"].ToString()),
                    SessionStart = (DateTime)data.Rows[0]["SessionTime"],
                    SessionLength = (int)data.Rows[0]["SessionLength"]
                };
            }
        }

        public StatisticsModel? GetSession(long GameId, string UserId)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "";
            Dictionary<string, object> dbDict;

            // get all the metadatamapids with the same game mapping
            sql = "SELECT MetadataSourceId FROM view_MetadataMap WHERE Id = @gameid;";
            DataTable dtGameIds = db.ExecuteCMD(sql, new Dictionary<string, object> { { "gameid", GameId } });

            sql = "SELECT Id FROM view_MetadataMap WHERE MetadataSourceId = @metadatasourceid;";
            dtGameIds = db.ExecuteCMD(sql, new Dictionary<string, object> { { "metadatasourceid", dtGameIds.Rows[0]["MetadataSourceId"] } });

            dbDict = new Dictionary<string, object>{
                { "gameid", GameId },
                { "userid", UserId }
            };

            int inCounter = 0;
            StringBuilder inStatement = new StringBuilder();
            foreach (DataRow row in dtGameIds.Rows)
            {
                if (inCounter == 0)
                {
                    inStatement.Append("@metadatasourceid" + inCounter);
                }
                else
                {
                    inStatement.Append(", @metadatasourceid" + inCounter);
                }
                dbDict.Add("metadatasourceid" + inCounter, row["Id"]);
                inCounter++;
            }

            sql = "SELECT SUM(SessionLength) AS TotalLength FROM UserTimeTracking WHERE GameId IN (" + inStatement + ") AND UserId = @userid;";

            DataTable data = db.ExecuteCMD(sql, dbDict);

            if (data.Rows.Count == 0)
            {
                return null;
            }
            else
            {
                if (data.Rows[0]["TotalLength"] == DBNull.Value)
                {
                    return null;
                }
                else
                {
                    int TotalTime = int.Parse(data.Rows[0]["TotalLength"].ToString());

                    sql = "SELECT * FROM UserTimeTracking WHERE GameId IN (" + inStatement + ") AND UserId = @userid ORDER BY SessionTime DESC LIMIT 1;";
                    data = db.ExecuteCMD(sql, dbDict);

                    return new StatisticsModel
                    {
                        GameId = GameId,
                        SessionLength = TotalTime,
                        SessionStart = (DateTime)data.Rows[0]["SessionTime"]
                    };
                }
            }
        }
    }
}
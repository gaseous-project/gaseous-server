using IGDB.Models;

namespace gaseous_server.Classes
{
    public class Favourites
    {
        public bool GetFavourite(string userid, long GameId)
        {
            Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            string sql = "SELECT * FROM Favourites WHERE UserId=@userid AND GameId=@gameid";
            Dictionary<string, object> dbDict = new Dictionary<string, object>{
                { "userid", userid },
                { "gameid", GameId}
            };

            if (db.ExecuteCMD(sql, dbDict).Rows.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool SetFavourite(string userid, long GameId, bool Favourite)
        {
            bool CurrentFavourite = GetFavourite(userid, GameId);
            if (CurrentFavourite == Favourite)
            {
                return Favourite;
            }
            else
            {
                Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
                string sql;
                Dictionary<string, object> dbDict = new Dictionary<string, object>{
                    { "userid", userid },
                    { "gameid", GameId}
                };

                if (CurrentFavourite == true)
                {
                    // delete existing value
                    sql = "DELETE FROM Favourites WHERE UserId=@userid AND GameId=@gameid";
                }
                else
                {
                    // insert new value
                    sql = "INSERT INTO Favourites (UserId, GameId) VALUES (@userid, @gameid)";
                }
                db.ExecuteNonQuery(sql, dbDict);

                return Favourite;
            }
        }
    }
}
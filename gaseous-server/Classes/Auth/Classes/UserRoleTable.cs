using System;
using System.Collections.Generic;
using System.Data;
using gaseous_server.Classes;
using Microsoft.AspNetCore.Identity;

namespace Authentication
{
    /// <summary>
    /// Class that represents the UserRoles table in the MySQL Database
    /// </summary>
    public class UserRolesTable
    {
        private Database _database;

        /// <summary>
        /// Constructor that takes a MySQLDatabase instance 
        /// </summary>
        /// <param name="database"></param>
        public UserRolesTable(Database database)
        {
            _database = database;
        }

        /// <summary>
        /// Returns a list of user's roles
        /// </summary>
        /// <param name="userId">The user's id</param>
        /// <returns></returns>
        public List<string> FindByUserId(string userId)
        {
            List<string> roles = new List<string>();
            string commandText = "Select Roles.Name from UserRoles, Roles where UserRoles.UserId = @userId and UserRoles.RoleId = Roles.Id";
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@userId", userId);

            var rows = _database.ExecuteCMD(commandText, parameters).Rows;
            foreach (DataRow row in rows)
            {
                roles.Add((string)row["Name"]);
            }

            return roles;
        }

        /// <summary>
        /// Deletes all roles from a user in the UserRoles table
        /// </summary>
        /// <param name="userId">The user's id</param>
        /// <returns></returns>
        public int Delete(string userId)
        {
            string commandText = "Delete from UserRoles where UserId = @userId";
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("UserId", userId);

            return (int)_database.ExecuteNonQuery(commandText, parameters);
        }

        public int DeleteUserFromRole(string userId, string roleId)
        {
            string commandText = "Delete from UserRoles where UserId = @userId and RoleId = @roleId";
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("userId", userId);
            parameters.Add("roleId", roleId);

            return (int)_database.ExecuteNonQuery(commandText, parameters);
        }

        /// <summary>
        /// Inserts a new role for a user in the UserRoles table
        /// </summary>
        /// <param name="user">The User</param>
        /// <param name="roleId">The Role's id</param>
        /// <returns></returns>
        public int Insert(IdentityUser user, string roleId)
        {
            string commandText = "Insert into UserRoles (UserId, RoleId) values (@userId, @roleId)";
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("userId", user.Id);
            parameters.Add("roleId", roleId);

            return (int)_database.ExecuteNonQuery(commandText, parameters);
        }
    }
}

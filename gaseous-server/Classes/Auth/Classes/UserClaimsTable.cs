﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Security.Claims;
using gaseous_server.Classes;
using Microsoft.AspNetCore.Identity;

namespace Authentication
{
    /// <summary>
    /// Class that represents the UserClaims table in the MySQL Database
    /// </summary>
    public class UserClaimsTable
    {
        private Database _database;

        /// <summary>
        /// Constructor that takes a MySQLDatabase instance 
        /// </summary>
        /// <param name="database"></param>
        public UserClaimsTable(Database database)
        {
            _database = database;
        }

        /// <summary>
        /// Returns a ClaimsIdentity instance given a userId
        /// </summary>
        /// <param name="userId">The user's id</param>
        /// <returns></returns>
        public ClaimsIdentity FindByUserId(string userId)
        {
            ClaimsIdentity claims = new ClaimsIdentity();
            string commandText = "Select * from UserClaims where UserId = @userId";
            Dictionary<string, object> parameters = new Dictionary<string, object>() { { "@UserId", userId } };

            var rows = _database.ExecuteCMD(commandText, parameters).Rows;
            foreach (DataRow row in rows)
            {
                Claim claim = new Claim((string)row["ClaimType"], (string)row["ClaimValue"]);
                claims.AddClaim(claim);
            }

            return claims;
        }

        /// <summary>
        /// Deletes all claims from a user given a userId
        /// </summary>
        /// <param name="userId">The user's id</param>
        /// <returns></returns>
        public int Delete(string userId)
        {
            string commandText = "Delete from UserClaims where UserId = @userId";
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("userId", userId);

            return (int)_database.ExecuteNonQuery(commandText, parameters);
        }

        /// <summary>
        /// Inserts a new claim in UserClaims table
        /// </summary>
        /// <param name="userClaim">User's claim to be added</param>
        /// <param name="userId">User's id</param>
        /// <returns></returns>
        public int Insert(Claim userClaim, string userId)
        {
            string commandText = "Insert into UserClaims (ClaimValue, ClaimType, UserId) values (@value, @type, @userId)";
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("value", userClaim.Value);
            parameters.Add("type", userClaim.Type);
            parameters.Add("userId", userId);

            return (int)_database.ExecuteNonQuery(commandText, parameters);
        }

        /// <summary>
        /// Deletes a claim from a user 
        /// </summary>
        /// <param name="user">The user to have a claim deleted</param>
        /// <param name="claim">A claim to be deleted from user</param>
        /// <returns></returns>
        public int Delete(IdentityUser user, Claim claim)
        {
            string commandText = "Delete from UserClaims where UserId = @userId and @ClaimValue = @value and ClaimType = @type";
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("userId", user.Id);
            parameters.Add("value", claim.Value);
            parameters.Add("type", claim.Type);

            return (int)_database.ExecuteNonQuery(commandText, parameters);
        }
    }
}

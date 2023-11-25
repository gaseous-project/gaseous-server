using System;
using System.Collections.Generic;
using System.Data;
using gaseous_server.Classes;
using Microsoft.AspNetCore.Identity;

namespace Authentication
{
    /// <summary>
    /// Class that represents the Users table in the MySQL Database
    /// </summary>
    public class UserTable<TUser>
        where TUser :ApplicationUser
    {
        private Database _database;

        /// <summary>
        /// Constructor that takes a MySQLDatabase instance 
        /// </summary>
        /// <param name="database"></param>
        public UserTable(Database database)
        {
            _database = database;
        }

        /// <summary>
        /// Returns the user's name given a user id
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public string? GetUserName(string userId)
        {
            string commandText = "Select NormalizedUserName from Users where Id = @id";
            Dictionary<string, object> parameters = new Dictionary<string, object>() { { "@id", userId } };

            DataTable table = _database.ExecuteCMD(commandText, parameters);
            
            if (table.Rows.Count == 0)
            {
                return null;
            }
            else
            {
                return (string)table.Rows[0][0];
            }
        }

        /// <summary>
        /// Returns a User ID given a user name
        /// </summary>
        /// <param name="userName">The user's name</param>
        /// <returns></returns>
        public string? GetUserId(string normalizedUserName)
        {
            string commandText = "Select Id from Users where NormalizedUserName = @name";
            Dictionary<string, object> parameters = new Dictionary<string, object>() { { "@name", normalizedUserName } };

            DataTable table = _database.ExecuteCMD(commandText, parameters);

            if (table.Rows.Count == 0)
            {
                return null;
            }
            else
            {
                return (string)table.Rows[0][0];
            }
        }

        /// <summary>
        /// Returns an TUser given the user's id
        /// </summary>
        /// <param name="userId">The user's id</param>
        /// <returns></returns>
        public TUser GetUserById(string userId)
        {
            TUser user = null;
            string commandText = "Select * from Users where Id = @id";
            Dictionary<string, object> parameters = new Dictionary<string, object>() { { "@id", userId } };

            var rows = _database.ExecuteCMDDict(commandText, parameters);
            if (rows != null && rows.Count == 1)
            {
                Dictionary<string, object> row = rows[0];
                user = (TUser)Activator.CreateInstance(typeof(TUser));
                user.Id = (string)row["Id"];
                user.UserName = (string?)row["UserName"];
                user.PasswordHash = (string?)(string.IsNullOrEmpty((string?)row["PasswordHash"]) ? null : row["PasswordHash"]);
                user.SecurityStamp = (string?)(string.IsNullOrEmpty((string?)row["SecurityStamp"]) ? null : row["SecurityStamp"]);
                user.ConcurrencyStamp = (string?)(string.IsNullOrEmpty((string?)row["ConcurrencyStamp"]) ? null : row["ConcurrencyStamp"]);
                user.Email = (string?)(string.IsNullOrEmpty((string?)row["Email"]) ? null : row["Email"]);
                user.EmailConfirmed = row["EmailConfirmed"] == "1" ? true:false;
                user.PhoneNumber = (string?)(string.IsNullOrEmpty((string?)row["PhoneNumber"]) ? null : row["PhoneNumber"]);
                user.PhoneNumberConfirmed = row["PhoneNumberConfirmed"] == "1" ? true : false;
                user.NormalizedEmail = (string?)(string.IsNullOrEmpty((string?)row["NormalizedEmail"]) ? null : row["NormalizedEmail"]);
                user.NormalizedUserName = (string?)(string.IsNullOrEmpty((string?)row["NormalizedUserName"]) ? null : row["NormalizedUserName"]);
                user.LockoutEnabled = row["LockoutEnabled"] == "1" ? true : false;
                user.LockoutEnd = string.IsNullOrEmpty((string?)row["LockoutEnd"]) ? DateTime.Now : DateTime.Parse((string?)row["LockoutEnd"]);
                user.AccessFailedCount = string.IsNullOrEmpty((string?)row["AccessFailedCount"]) ? 0 : int.Parse((string?)row["AccessFailedCount"]);
                user.TwoFactorEnabled = row["TwoFactorEnabled"] == "1" ? true:false;
                user.SecurityProfile = GetSecurityProfile(user);
            }

            return user;
        }

        /// <summary>
        /// Returns a list of TUser instances given a user name
        /// </summary>
        /// <param name="normalizedUserName">User's name</param>
        /// <returns></returns>
        public List<TUser> GetUserByName(string normalizedUserName)
        {
            List<TUser> users = new List<TUser>();
            string commandText = "Select * from Users where NormalizedEmail = @name";
            Dictionary<string, object> parameters = new Dictionary<string, object>() { { "@name", normalizedUserName } };

            var rows = _database.ExecuteCMDDict(commandText, parameters);
            foreach(Dictionary<string, object> row in rows)
            {
                TUser user = (TUser)Activator.CreateInstance(typeof(TUser));
                user.Id = (string)row["Id"];
                user.UserName = (string?)row["UserName"];
                user.PasswordHash = (string?)(string.IsNullOrEmpty((string?)row["PasswordHash"]) ? null : row["PasswordHash"]);
                user.SecurityStamp = (string?)(string.IsNullOrEmpty((string?)row["SecurityStamp"]) ? null : row["SecurityStamp"]);
                user.ConcurrencyStamp = (string?)(string.IsNullOrEmpty((string?)row["ConcurrencyStamp"]) ? null : row["ConcurrencyStamp"]);
                user.Email = (string?)(string.IsNullOrEmpty((string?)row["Email"]) ? null : row["Email"]);
                user.EmailConfirmed = row["EmailConfirmed"] == "1" ? true:false;
                user.PhoneNumber = (string?)(string.IsNullOrEmpty((string?)row["PhoneNumber"]) ? null : row["PhoneNumber"]);
                user.PhoneNumberConfirmed = row["PhoneNumberConfirmed"] == "1" ? true : false;
                user.NormalizedEmail = (string?)(string.IsNullOrEmpty((string?)row["NormalizedEmail"]) ? null : row["NormalizedEmail"]);
                user.NormalizedUserName = (string?)(string.IsNullOrEmpty((string?)row["NormalizedUserName"]) ? null : row["NormalizedUserName"]);
                user.LockoutEnabled = row["LockoutEnabled"] == "1" ? true : false;
                user.LockoutEnd = string.IsNullOrEmpty((string?)row["LockoutEnd"]) ? DateTime.Now : DateTime.Parse((string?)row["LockoutEnd"]);
                user.AccessFailedCount = string.IsNullOrEmpty((string?)row["AccessFailedCount"]) ? 0 : int.Parse((string?)row["AccessFailedCount"]);
                user.TwoFactorEnabled = row["TwoFactorEnabled"] == "1" ? true:false;
                user.SecurityProfile = GetSecurityProfile(user);
                users.Add(user);
            }

            return users;
        }

        public List<TUser> GetUsers()
        {
            List<TUser> users = new List<TUser>();
            string commandText = "Select * from Users order by NormalizedUserName";
            
            var rows = _database.ExecuteCMDDict(commandText);
            foreach(Dictionary<string, object> row in rows)
            {
                TUser user = (TUser)Activator.CreateInstance(typeof(TUser));
                user.Id = (string)row["Id"];
                user.UserName = (string?)row["UserName"];
                user.PasswordHash = (string?)(string.IsNullOrEmpty((string?)row["PasswordHash"]) ? null : row["PasswordHash"]);
                user.SecurityStamp = (string?)(string.IsNullOrEmpty((string?)row["SecurityStamp"]) ? null : row["SecurityStamp"]);
                user.ConcurrencyStamp = (string?)(string.IsNullOrEmpty((string?)row["ConcurrencyStamp"]) ? null : row["ConcurrencyStamp"]);
                user.Email = (string?)(string.IsNullOrEmpty((string?)row["Email"]) ? null : row["Email"]);
                user.EmailConfirmed = row["EmailConfirmed"] == "1" ? true:false;
                user.PhoneNumber = (string?)(string.IsNullOrEmpty((string?)row["PhoneNumber"]) ? null : row["PhoneNumber"]);
                user.PhoneNumberConfirmed = row["PhoneNumberConfirmed"] == "1" ? true : false;
                user.NormalizedEmail = (string?)(string.IsNullOrEmpty((string?)row["NormalizedEmail"]) ? null : row["NormalizedEmail"]);
                user.NormalizedUserName = (string?)(string.IsNullOrEmpty((string?)row["NormalizedUserName"]) ? null : row["NormalizedUserName"]);
                user.LockoutEnabled = row["LockoutEnabled"] == "1" ? true : false;
                user.LockoutEnd = string.IsNullOrEmpty((string?)row["LockoutEnd"]) ? DateTime.Now : DateTime.Parse((string?)row["LockoutEnd"]);
                user.AccessFailedCount = string.IsNullOrEmpty((string?)row["AccessFailedCount"]) ? 0 : int.Parse((string?)row["AccessFailedCount"]);
                user.TwoFactorEnabled = row["TwoFactorEnabled"] == "1" ? true:false;
                user.SecurityProfile = GetSecurityProfile(user);
                users.Add(user);
            }

            return users;
        }

        public TUser GetUserByEmail(string email)
        {
            List<TUser> users = GetUserByName(email);
            if (users.Count == 0)
            {
                return null;
            }
            else
            {
                return users[0];
            }
        }

        /// <summary>
        /// Return the user's password hash
        /// </summary>
        /// <param name="userId">The user's id</param>
        /// <returns></returns>
        public string GetPasswordHash(string userId)
        {
            string commandText = "Select PasswordHash from Users where Id = @id";
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@id", userId);

            DataTable table = _database.ExecuteCMD(commandText, parameters);

            if (table.Rows.Count == 0)
            {
                return null;
            }
            else
            {
                return (string)table.Rows[0][0];
            }
        }

        /// <summary>
        /// Sets the user's password hash
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="passwordHash"></param>
        /// <returns></returns>
        public int SetPasswordHash(string userId, string passwordHash)
        {
            string commandText = "Update Users set PasswordHash = @pwdHash where Id = @id";
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@pwdHash", passwordHash);
            parameters.Add("@id", userId);

            return _database.ExecuteCMD(commandText, parameters).Rows.Count;
        }

        /// <summary>
        /// Returns the user's security stamp
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public string GetSecurityStamp(string userId)
        {
            string commandText = "Select SecurityStamp from Users where Id = @id";
            Dictionary<string, object> parameters = new Dictionary<string, object>() { { "@id", userId } };
            DataTable table = _database.ExecuteCMD(commandText, parameters);

            if (table.Rows.Count == 0)
            {
                return null;
            }
            else
            {
                return (string)table.Rows[0][0];
            }
        }

        /// <summary>
        /// Inserts a new user in the Users table
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public int Insert(TUser user)
        {
            string commandText = @"Insert into Users (UserName, Id, PasswordHash, SecurityStamp, ConcurrencyStamp, Email, EmailConfirmed, PhoneNumber, PhoneNumberConfirmed, NormalizedEmail, NormalizedUserName, AccessFailedCount, LockoutEnabled, LockoutEnd, TwoFactorEnabled) values (@name, @id, @pwdHash, @SecStamp, @concurrencystamp, @email ,@emailconfirmed ,@phonenumber, @phonenumberconfirmed, @normalizedemail, @normalizedusername, @accesscount, @lockoutenabled, @lockoutenddate, @twofactorenabled);";
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@name", user.UserName);
            parameters.Add("@id", user.Id);
            parameters.Add("@pwdHash", user.PasswordHash);
            parameters.Add("@SecStamp", user.SecurityStamp);
            parameters.Add("@concurrencystamp", user.ConcurrencyStamp);
            parameters.Add("@email", user.Email);
            parameters.Add("@emailconfirmed", user.EmailConfirmed);
            parameters.Add("@phonenumber", user.PhoneNumber);
            parameters.Add("@phonenumberconfirmed", user.PhoneNumberConfirmed);
            parameters.Add("@normalizedemail", user.NormalizedEmail);
            parameters.Add("@normalizedusername", user.NormalizedUserName);
            parameters.Add("@accesscount", user.AccessFailedCount);
            parameters.Add("@lockoutenabled", user.LockoutEnabled);
            parameters.Add("@lockoutenddate", user.LockoutEnd);
            parameters.Add("@twofactorenabled", user.TwoFactorEnabled);

            // set default security profile
            SetSecurityProfile(user, new SecurityProfileViewModel());

            return _database.ExecuteCMD(commandText, parameters).Rows.Count;
        }

        /// <summary>
        /// Deletes a user from the Users table
        /// </summary>
        /// <param name="userId">The user's id</param>
        /// <returns></returns>
        private int Delete(string userId)
        {
            string commandText = "Delete from Users where Id = @userId";
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@userId", userId);

            return _database.ExecuteCMD(commandText, parameters).Rows.Count;
        }

        /// <summary>
        /// Deletes a user from the Users table
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public int Delete(TUser user)
        {
            return Delete(user.Id);
        }

        /// <summary>
        /// Updates a user in the Users table
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public int Update(TUser user)
        {
            string commandText = @"Update Users set UserName = @userName, PasswordHash = @pwdHash, SecurityStamp = @secStamp, ConcurrencyStamp = @concurrencystamp, Email = @email, EmailConfirmed = @emailconfirmed, PhoneNumber = @phonenumber, PhoneNumberConfirmed = @phonenumberconfirmed, NormalizedEmail = @normalizedemail, NormalizedUserName = @normalizedusername, AccessFailedCount = @accesscount, LockoutEnabled = @lockoutenabled, LockoutEnd = @lockoutenddate, TwoFactorEnabled=@twofactorenabled WHERE Id = @userId;";
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@userId", user.Id);
            parameters.Add("@userName", user.UserName);
            parameters.Add("@pwdHash", user.PasswordHash);
            parameters.Add("@SecStamp", user.SecurityStamp);
            parameters.Add("@concurrencystamp", user.ConcurrencyStamp);
            parameters.Add("@email", user.Email);
            parameters.Add("@emailconfirmed", user.EmailConfirmed);
            parameters.Add("@phonenumber", user.PhoneNumber);
            parameters.Add("@phonenumberconfirmed", user.PhoneNumberConfirmed);
            parameters.Add("@normalizedemail", user.NormalizedEmail);
            parameters.Add("@normalizedusername", user.NormalizedUserName);
            parameters.Add("@accesscount", user.AccessFailedCount);
            parameters.Add("@lockoutenabled", user.LockoutEnabled);
            parameters.Add("@lockoutenddate", user.LockoutEnd);
            parameters.Add("@twofactorenabled", user.TwoFactorEnabled);

            // set the security profile
            SetSecurityProfile(user, user.SecurityProfile);

            return _database.ExecuteCMD(commandText, parameters).Rows.Count;
        }

        private SecurityProfileViewModel GetSecurityProfile(TUser user)
        {
            string sql = "SELECT SecurityProfile FROM Users WHERE Id=@Id;";
            Dictionary<string, object> dbDict = new Dictionary<string, object>();
            dbDict.Add("Id", user.Id);

            List<Dictionary<string, object>> data = _database.ExecuteCMDDict(sql, dbDict);
            if (data.Count == 0)
            {
                // no saved profile - return the default one
                return new SecurityProfileViewModel();
            }
            else
            {
                string? securityProfileString = (string?)data[0]["SecurityProfile"];
                if (securityProfileString != null && securityProfileString != "null")
                {
                    SecurityProfileViewModel securityProfile = Newtonsoft.Json.JsonConvert.DeserializeObject<SecurityProfileViewModel>(securityProfileString);
                    return securityProfile;
                }
                else
                {
                    return new SecurityProfileViewModel();
                }
            }
        }

        private int SetSecurityProfile(TUser user, SecurityProfileViewModel securityProfile)
        {
            string commandText = "UPDATE Users SET SecurityProfile=@SecurityProfile WHERE Id=@Id;";
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("Id", user.Id);
            parameters.Add("SecurityProfile", Newtonsoft.Json.JsonConvert.SerializeObject(securityProfile));
            
            return _database.ExecuteCMD(commandText, parameters).Rows.Count;
        }
    }
}

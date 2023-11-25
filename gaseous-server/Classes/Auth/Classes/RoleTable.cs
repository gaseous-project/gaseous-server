using System;
using System.Collections.Generic;
using System.Data;
using gaseous_server.Classes;
using Microsoft.AspNetCore.Identity;

namespace Authentication
{
    /// <summary>
    /// Class that represents the Role table in the MySQL Database
    /// </summary>
    public class RoleTable 
    {
        private Database _database;

        /// <summary>
        /// Constructor that takes a MySQLDatabase instance 
        /// </summary>
        /// <param name="database"></param>
        public RoleTable(Database database)
        {
            _database = database;
        }

        /// <summary>
        /// Deltes a role from the Roles table
        /// </summary>
        /// <param name="roleId">The role Id</param>
        /// <returns></returns>
        public int Delete(string roleId)
        {
            string commandText = "Delete from Roles where Id = @id";
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@id", roleId);

            return (int)_database.ExecuteNonQuery(commandText, parameters);
        }

        /// <summary>
        /// Inserts a new Role in the Roles table
        /// </summary>
        /// <param name="roleName">The role's name</param>
        /// <returns></returns>
        public int Insert(ApplicationRole role)
        {
            string commandText = "Insert into Roles (Id, Name) values (@id, @name)";
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@name", role.Name);
            parameters.Add("@id", role.Id);

            return (int)_database.ExecuteNonQuery(commandText, parameters);
        }

        /// <summary>
        /// Returns a role name given the roleId
        /// </summary>
        /// <param name="roleId">The role Id</param>
        /// <returns>Role name</returns>
        public string? GetRoleName(string roleId)
        {
            string commandText = "Select Name from Roles where Id = @id";
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@id", roleId);

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
        /// Returns the role Id given a role name
        /// </summary>
        /// <param name="roleName">Role's name</param>
        /// <returns>Role's Id</returns>
        public string? GetRoleId(string roleName)
        {
            string? roleId = null;
            string commandText = "Select Id from Roles where Name = @name";
            Dictionary<string, object> parameters = new Dictionary<string, object>() { { "@name", roleName } };

            DataTable result = _database.ExecuteCMD(commandText, parameters);
            if (result.Rows.Count > 0)
            {
                return Convert.ToString(result.Rows[0][0]);
            }

            return roleId;
        }

        /// <summary>
        /// Gets the ApplicationRole given the role Id
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public ApplicationRole? GetRoleById(string roleId)
        {
            var roleName = GetRoleName(roleId);
            ApplicationRole? role = null;

            if(roleName != null)
            {
                role = new ApplicationRole();
                role.Id = roleId;
                role.Name = roleName;
                role.NormalizedName = roleName.ToUpper();
            }

            return role;

        }

        /// <summary>
        /// Gets the ApplicationRole given the role name
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public ApplicationRole? GetRoleByName(string roleName)
        {
            var roleId = GetRoleId(roleName);
            ApplicationRole role = null;

            if (roleId != null)
            {
                role = new ApplicationRole();
                role.Id = roleId;
                role.Name = roleName;
                role.NormalizedName = roleName.ToUpper();
            }

            return role;
        }

        public int Update(ApplicationRole role)
        {
            string commandText = "Update Roles set Name = @name where Id = @id";
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            parameters.Add("@id", role.Id);

            return (int)_database.ExecuteNonQuery(commandText, parameters);
        }

        public List<ApplicationRole> GetRoles()
        {
            List<ApplicationRole> roles = new List<ApplicationRole>();

            string commandText = "Select Name from Roles";

            var rows = _database.ExecuteCMDDict(commandText);
            foreach(Dictionary<string, object> row in rows)
            {
                ApplicationRole role = (ApplicationRole)Activator.CreateInstance(typeof(ApplicationRole));
                role.Id = (string)row["Id"];
                role.Name = (string)row["Name"];
                role.NormalizedName = ((string)row["Name"]).ToUpper();
                roles.Add(role);
            }

            return roles;
        }
    }
}

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using gaseous_server.Classes;
using Microsoft.AspNetCore.Identity;
using MySqlConnector;

namespace Authentication
{
    /// <summary>
    /// Class that implements the key ASP.NET Identity role store iterfaces
    /// </summary>
    public class RoleStore : IQueryableRoleStore<ApplicationRole>
    {
        private RoleTable roleTable;
        public Database Database { get; private set; }

        public IQueryable<ApplicationRole> Roles
        {
            get
            {
                List<ApplicationRole> roles = roleTable.GetRoles();
                return roles.AsQueryable();
            }
        }

        public RoleStore()
        {
            Database = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            roleTable = new RoleTable(Database);
        }

        /// <summary>
        /// Constructor that takes a MySQLDatabase as argument 
        /// </summary>
        /// <param name="database"></param>
        public RoleStore(Database database)
        {
            Database = database;
            roleTable = new RoleTable(database);
        }

        public Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }

            roleTable.Insert(role);

            return Task.FromResult<IdentityResult>(IdentityResult.Success);
        }

        public Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            if (role == null)
            {
                throw new ArgumentNullException("user");
            }

            roleTable.Delete(role.Id);

            return Task.FromResult<IdentityResult>(IdentityResult.Success);
        }

        public Task<ApplicationRole> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        {
            ApplicationRole result = roleTable.GetRoleById(roleId) as ApplicationRole;

            return Task.FromResult<ApplicationRole>(result);
        }

        public Task<bool> RoleExistsAsync(string roleId, CancellationToken cancellationToken)
        {
            ApplicationRole? result = roleTable.GetRoleById(roleId) as ApplicationRole;

            if (result == null)
            {
                return Task.FromResult<bool>(false);
            }
            else
            {
                return Task.FromResult<bool>(true);
            }
        }

        public Task<ApplicationRole?> FindByNameAsync(string roleName, CancellationToken cancellationToken)
        {
            ApplicationRole? result = roleTable.GetRoleByName(roleName) as ApplicationRole;

            return Task.FromResult<ApplicationRole?>(result);
        }

        public Task<IdentityResult> UpdateAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            if (role == null)
            {
                throw new ArgumentNullException("user");
            }

            roleTable.Update(role);

            return Task.FromResult<IdentityResult>(IdentityResult.Success);
        }

        public void Dispose()
        {
            if (Database != null)
            {
                Database = null;
            }
        }

        public Task<string> GetRoleIdAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            if (role != null)
            {
                return Task.FromResult<string>(roleTable.GetRoleId(role.Name));
            }

            return Task.FromResult<string>(null);
        }

        public Task<string?> GetRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            if (role != null)
            {
                return Task.FromResult<string?>(roleTable.GetRoleName(role.Id));
            }

            return Task.FromResult<string?>(null);
        }

        public Task SetRoleNameAsync(ApplicationRole role, string? roleName, CancellationToken cancellationToken)
        {
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }

            role.Name = roleName;
            roleTable.Update(role);

            return Task.FromResult<IdentityResult>(IdentityResult.Success);
        }

        public Task<string?> GetNormalizedRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            if (role != null)
            {
                return Task.FromResult<string?>(roleTable.GetRoleName(role.Id));
            }

            return Task.FromResult<string?>(null);
        }

        public Task SetNormalizedRoleNameAsync(ApplicationRole role, string? normalizedName, CancellationToken cancellationToken)
        {
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }

            role.Name = normalizedName;
            roleTable.Update(role);

            return Task.FromResult<IdentityResult>(IdentityResult.Success);
        }
    }
}

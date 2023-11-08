using System;
using System.Security.Claims;
using System.Threading.Tasks;
using gaseous_server.Classes;
using Microsoft.AspNetCore.Identity;
using MySqlConnector;

namespace Classes.Auth
{
    /// <summary>
    /// Class that implements the key ASP.NET Identity role store iterfaces
    /// </summary>
    public class RoleStore<TRole> : IQueryableRoleStore<TRole>
        where TRole : IdentityRole
    {
        private RoleTable roleTable;
        public Database Database { get; private set; }

        public IQueryable<TRole> Roles
        {
            get
            {
                throw new NotImplementedException();
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

        public Task<IdentityResult> CreateAsync(TRole role, CancellationToken cancellationToken)
        {
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }

            roleTable.Insert(role);

            return Task.FromResult<IdentityResult>(IdentityResult.Success);
        }

        public Task<IdentityResult> DeleteAsync(TRole role, CancellationToken cancellationToken)
        {
            if (role == null)
            {
                throw new ArgumentNullException("user");
            }

            roleTable.Delete(role.Id);

            return Task.FromResult<IdentityResult>(IdentityResult.Success);
        }

        public Task<TRole> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        {
            TRole result = roleTable.GetRoleById(roleId) as TRole;

            return Task.FromResult<TRole>(result);
        }

        public Task<bool> RoleExistsAsync(string roleId, CancellationToken cancellationToken)
        {
            TRole? result = roleTable.GetRoleById(roleId) as TRole;

            if (result == null)
            {
                return Task.FromResult<bool>(false);
            }
            else
            {
                return Task.FromResult<bool>(true);
            }
        }

        public Task<TRole?> FindByNameAsync(string roleName, CancellationToken cancellationToken)
        {
            TRole? result = roleTable.GetRoleByName(roleName) as TRole;

            return Task.FromResult<TRole?>(result);
        }

        public Task<IdentityResult> UpdateAsync(TRole role, CancellationToken cancellationToken)
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

        public Task<string> GetRoleIdAsync(TRole role, CancellationToken cancellationToken)
        {
            if (role != null)
            {
                return Task.FromResult<string>(roleTable.GetRoleId(role.Name));
            }

            return Task.FromResult<string>(null);
        }

        public Task<string?> GetRoleNameAsync(TRole role, CancellationToken cancellationToken)
        {
            if (role != null)
            {
                return Task.FromResult<string?>(roleTable.GetRoleName(role.Id));
            }

            return Task.FromResult<string?>(null);
        }

        public Task SetRoleNameAsync(TRole role, string? roleName, CancellationToken cancellationToken)
        {
            if (role == null)
            {
                throw new ArgumentNullException("role");
            }

            role.Name = roleName;
            roleTable.Update(role);

            return Task.FromResult<IdentityResult>(IdentityResult.Success);
        }

        public Task<string?> GetNormalizedRoleNameAsync(TRole role, CancellationToken cancellationToken)
        {
            if (role != null)
            {
                return Task.FromResult<string?>(roleTable.GetRoleName(role.Id));
            }

            return Task.FromResult<string?>(null);
        }

        public Task SetNormalizedRoleNameAsync(TRole role, string? normalizedName, CancellationToken cancellationToken)
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

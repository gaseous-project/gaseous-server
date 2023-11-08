using System;
using System.Security.Claims;
using System.Threading.Tasks;
using gaseous_server.Classes;
using Microsoft.AspNetCore.Identity;
using MySqlConnector;

namespace Classes.Auth
{
    public class UserStore<TUser> : 
        IUserStore<TUser>,
        IUserRoleStore<TUser>,
        IUserLoginStore<TUser>,
        IUserClaimStore<TUser>,
        IUserPasswordStore<TUser>,
        IUserSecurityStampStore<TUser>,
        IQueryableUserStore<TUser>,
        IUserEmailStore<TUser>,
        IUserPhoneNumberStore<TUser>,
        IUserTwoFactorStore<TUser>,
        IUserLockoutStore<TUser>
        where TUser : IdentityUser
    {
        private Database database;

        private UserTable<TUser> userTable;
        private RoleTable roleTable;
        private UserRolesTable userRolesTable;
        private UserLoginsTable userLoginsTable;
        private UserClaimsTable userClaimsTable;

        public UserStore()
        {
            database = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            userTable = new UserTable<TUser>(database);
            roleTable = new RoleTable(database);
            userRolesTable = new UserRolesTable(database);
            userLoginsTable = new UserLoginsTable(database);
            userClaimsTable = new UserClaimsTable(database);
        }

        public UserStore(Database database)
        {
            this.database = database;
            userTable = new UserTable<TUser>(database);
            roleTable = new RoleTable(database);
            userRolesTable = new UserRolesTable(database);
            userLoginsTable = new UserLoginsTable(database);
            userClaimsTable = new UserClaimsTable(database);
        }

        public IQueryable<TUser> Users
        {
            get
            {
                return (IQueryable<TUser>)userTable.GetUsers();
            }
        }

        public Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (claims == null)
            {
                throw new ArgumentNullException("user");
            }

            foreach (Claim claim in claims)
            {
                userClaimsTable.Insert(claim, user.Id);
            }

            return Task.FromResult<object>(null);
        }

        public Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (login == null)
            {
                throw new ArgumentNullException("login");
            }

            userLoginsTable.Insert(user, login);

            return Task.FromResult<object>(null);
        }

        public Task AddToRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (string.IsNullOrEmpty(roleName))
            {
                throw new ArgumentException("Argument cannot be null or empty: roleName.");
            }

            string roleId = roleTable.GetRoleId(roleName);
            if (!string.IsNullOrEmpty(roleId))
            {
                userRolesTable.Insert(user, roleId);
            }

            return Task.FromResult<object>(null);
        }

        public Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            userTable.Insert(user);

            return Task.FromResult<IdentityResult>(IdentityResult.Success);
        }

        public Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user != null)
            {
                userTable.Delete(user);
            }

            return Task.FromResult<IdentityResult>(IdentityResult.Success);
        }

        public void Dispose()
        {
            if (database != null)
            {
                database = null;
            }
        }

        public Task<TUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(normalizedEmail))
            {
                throw new ArgumentNullException("email");
            }

            TUser result = userTable.GetUserByEmail(normalizedEmail) as TUser;
            if (result != null)
            {
                return Task.FromResult<TUser>(result);
            }

            return Task.FromResult<TUser>(null);
        }

        public Task<TUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("Null or empty argument: userId");
            }

            TUser result = userTable.GetUserById(userId) as TUser;
            if (result != null)
            {
                return Task.FromResult<TUser>(result);
            }

            return Task.FromResult<TUser>(null);
        }

        public Task<TUser?> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            if (loginProvider == null || providerKey == null)
            {
                throw new ArgumentNullException("login");
            }

            UserLoginInfo login = new UserLoginInfo(loginProvider, providerKey, loginProvider);

            var userId = userLoginsTable.FindUserIdByLogin(login);
            if (userId != null)
            {
                TUser user = userTable.GetUserById(userId) as TUser;
                if (user != null)
                {
                    return Task.FromResult<TUser>(user);
                }
            }

            return Task.FromResult<TUser>(null);
        }

        public Task<TUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(normalizedUserName))
            {
                throw new ArgumentException("Null or empty argument: normalizedUserName");
            }

            List<TUser> result = userTable.GetUserByName(normalizedUserName) as List<TUser>;

            // Should I throw if > 1 user?
            if (result != null && result.Count == 1)
            {
                return Task.FromResult<TUser>(result[0]);
            }

            return Task.FromResult<TUser>(null);
        }

        public Task<int> GetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.AccessFailedCount);
        }

        public Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken)
        {
            ClaimsIdentity identity = userClaimsTable.FindByUserId(user.Id);

            return Task.FromResult<IList<Claim>>(identity.Claims.ToList());
        }

        public Task<string?> GetEmailAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.EmailConfirmed);
        }

        public Task<bool> GetLockoutEnabledAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.LockoutEnabled);
        }

        public Task<DateTimeOffset?> GetLockoutEndDateAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user.LockoutEnd.HasValue)
            {
                return Task.FromResult((DateTimeOffset?)user.LockoutEnd.Value);
            }
            else
            {
                return Task.FromResult((DateTimeOffset?)new DateTimeOffset());
            }
        }

        public Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            List<UserLoginInfo> logins = userLoginsTable.FindByUserId(user.Id);
            if (logins != null)
            {
                return Task.FromResult<IList<UserLoginInfo>>(logins);
            }

            return Task.FromResult<IList<UserLoginInfo>>(null);
        }

        public Task<string?> GetNormalizedEmailAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.NormalizedEmail);
        }

        public Task<string?> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user != null)
            {
                return Task.FromResult<string?>(userTable.GetUserName(user.Id));
            }

            return Task.FromResult<string?>(null);
        }

        public Task<string?> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user != null)
            {
                return Task.FromResult<string?>(userTable.GetPasswordHash(user.Id));
            }

            return Task.FromResult<string?>(null);
        }

        public Task<string?> GetPhoneNumberAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PhoneNumber);
        }

        public Task<bool> GetPhoneNumberConfirmedAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PhoneNumberConfirmed);
        }

        public Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            List<string> roles = userRolesTable.FindByUserId(user.Id);
            {
                if (roles != null)
                {
                    return Task.FromResult<IList<string>>(roles);
                }
            }

            return Task.FromResult<IList<string>>(null);
        }

        public Task<string?> GetSecurityStampAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.SecurityStamp);
        }

        public Task<bool> GetTwoFactorEnabledAsync(TUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.TwoFactorEnabled);
        }

        public Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user != null)
            {
                return Task.FromResult<string>(userTable.GetUserId(user.NormalizedUserName));
            }

            return Task.FromResult<string>(null);
        }

        public Task<string?> GetUserNameAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user != null)
            {
                return Task.FromResult<string?>(userTable.GetUserName(user.Id));
            }

            return Task.FromResult<string?>(null);
        }

        public Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IList<TUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken)
        {
            var hasPassword = !string.IsNullOrEmpty(userTable.GetPasswordHash(user.Id));

            return Task.FromResult<bool>(Boolean.Parse(hasPassword.ToString()));
        }

        public Task<int> IncrementAccessFailedCountAsync(TUser user, CancellationToken cancellationToken)
        {
            user.AccessFailedCount++;
            userTable.Update(user);

            return Task.FromResult(user.AccessFailedCount);
        }

        public Task<bool> IsInRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (string.IsNullOrEmpty(roleName))
            {
                throw new ArgumentNullException("role");
            }

            List<string> roles = userRolesTable.FindByUserId(user.Id);
            {
                if (roles != null && roles.Contains(roleName))
                {
                    return Task.FromResult<bool>(true);
                }
            }

            return Task.FromResult<bool>(false);
        }

        public Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (claims == null)
            {
                throw new ArgumentNullException("claim");
            }

            foreach (Claim claim in claims)
            {
                userClaimsTable.Delete(user, claim);
            }

            return Task.FromResult<object>(null);
        }

        public Task RemoveFromRoleAsync(TUser user, string roleName, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (roleName == null)
            {
                throw new ArgumentNullException("role");
            }

            IdentityRole? role = roleTable.GetRoleByName(roleName);

            if (role != null)
            {
                userRolesTable.DeleteUserFromRole(user.Id, role.Id);
            }

            return Task.FromResult<Object>(null);
        }

        public Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (loginProvider == null || providerKey == null)
            {
                throw new ArgumentNullException("login");
            }

            UserLoginInfo login = new UserLoginInfo(loginProvider, providerKey, loginProvider);

            userLoginsTable.Delete(user, login);

            return Task.FromResult<Object>(null);
        }

        public Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (claim == null || newClaim == null)
            {
                throw new ArgumentNullException("claim");
            }

            userClaimsTable.Delete(user, claim);
            userClaimsTable.Insert(newClaim, user.Id);

            return Task.FromResult<Object>(null);
        }

        public Task ResetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken)
        {
            user.AccessFailedCount = 0;
            userTable.Update(user);

            return Task.FromResult(0);
        }

        public Task SetEmailAsync(TUser user, string? email, CancellationToken cancellationToken)
        {
            user.Email = email;
            userTable.Update(user);

            return Task.FromResult(0);
        }

        public Task SetEmailConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken)
        {
            user.EmailConfirmed = confirmed;
            userTable.Update(user);

            return Task.FromResult(0);
        }

        public Task SetLockoutEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken)
        {
            user.LockoutEnabled = enabled;
            userTable.Update(user);

            return Task.FromResult(0);
        }

        public Task SetLockoutEndDateAsync(TUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
        {
            user.LockoutEnd = lockoutEnd;
            userTable.Update(user);

            return Task.FromResult(0);
        }

        public Task SetNormalizedEmailAsync(TUser user, string? normalizedEmail, CancellationToken cancellationToken)
        {
            user.NormalizedEmail = normalizedEmail;
            userTable.Update(user);

            return Task.FromResult(0);
        }

        public Task SetNormalizedUserNameAsync(TUser user, string? normalizedName, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            user.NormalizedUserName = normalizedName;
            userTable.Update(user);

            return Task.FromResult<IdentityResult>(IdentityResult.Success);
        }

        public Task SetPasswordHashAsync(TUser user, string? passwordHash, CancellationToken cancellationToken)
        {
            user.PasswordHash = passwordHash;

            return Task.FromResult<Object>(null);
        }

        public Task SetPhoneNumberAsync(TUser user, string? phoneNumber, CancellationToken cancellationToken)
        {
            user.PhoneNumber = phoneNumber;
            userTable.Update(user);

            return Task.FromResult(0);
        }

        public Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken)
        {
            user.PhoneNumberConfirmed = confirmed;
            userTable.Update(user);

            return Task.FromResult(0);
        }

        public Task SetSecurityStampAsync(TUser user, string stamp, CancellationToken cancellationToken)
        {
            user.SecurityStamp = stamp;

            return Task.FromResult(0);
        }

        public Task SetTwoFactorEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken)
        {
            user.TwoFactorEnabled = enabled;
            userTable.Update(user);

            return Task.FromResult(0);
        }

        public Task SetUserNameAsync(TUser user, string? userName, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            user.UserName = userName;
            userTable.Update(user);

            return Task.FromResult<IdentityResult>(IdentityResult.Success);
        }

        public Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            userTable.Update(user);

            return Task.FromResult<IdentityResult>(IdentityResult.Success);
        }
    }
}
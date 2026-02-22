using System;
using System.Security.Claims;
using System.Threading.Tasks;
using gaseous_server.Classes;
using Microsoft.AspNetCore.Identity;
using MySqlConnector;

namespace Authentication
{
    public class UserStore :
        IUserStore<ApplicationUser>,
        IUserRoleStore<ApplicationUser>,
        IUserLoginStore<ApplicationUser>,
        IUserClaimStore<ApplicationUser>,
        IUserPasswordStore<ApplicationUser>,
        IUserSecurityStampStore<ApplicationUser>,
        IQueryableUserStore<ApplicationUser>,
        IUserEmailStore<ApplicationUser>,
        IUserPhoneNumberStore<ApplicationUser>,
        IUserTwoFactorStore<ApplicationUser>,
        IUserLockoutStore<ApplicationUser>,
        IUserAuthenticatorKeyStore<ApplicationUser>,
        IUserTwoFactorRecoveryCodeStore<ApplicationUser>
    {
        private Database database;

        private UserTable<ApplicationUser> userTable;
        private RoleTable roleTable;
        private UserRolesTable userRolesTable;
        private UserLoginsTable userLoginsTable;
        private UserClaimsTable userClaimsTable;
        private UserAuthenticatorKeysTable userAuthenticatorKeysTable;
        private UserRecoveryCodesTable userRecoveryCodesTable;

        public UserStore()
        {
            database = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
            userTable = new UserTable<ApplicationUser>(database);
            roleTable = new RoleTable(database);
            userRolesTable = new UserRolesTable(database);
            userLoginsTable = new UserLoginsTable(database);
            userClaimsTable = new UserClaimsTable(database);
            userAuthenticatorKeysTable = new UserAuthenticatorKeysTable(database);
            userRecoveryCodesTable = new UserRecoveryCodesTable(database);
        }

        public UserStore(Database database)
        {
            this.database = database;
            userTable = new UserTable<ApplicationUser>(database);
            roleTable = new RoleTable(database);
            userRolesTable = new UserRolesTable(database);
            userLoginsTable = new UserLoginsTable(database);
            userClaimsTable = new UserClaimsTable(database);
            userAuthenticatorKeysTable = new UserAuthenticatorKeysTable(database);
            userRecoveryCodesTable = new UserRecoveryCodesTable(database);
        }

        public IQueryable<ApplicationUser> Users
        {
            get
            {
                List<ApplicationUser> users = userTable.GetUsers();
                return users.AsQueryable();
            }
        }

        public Task AddClaimsAsync(ApplicationUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
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

        public Task AddLoginAsync(ApplicationUser user, UserLoginInfo login, CancellationToken cancellationToken)
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

        public Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
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

        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            userTable.Insert(user);

            return Task.FromResult<IdentityResult>(IdentityResult.Success);
        }

        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
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

        public Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(normalizedEmail))
            {
                throw new ArgumentNullException("email");
            }

            ApplicationUser result = userTable.GetUserByEmail(normalizedEmail) as ApplicationUser;
            if (result != null)
            {
                return Task.FromResult<ApplicationUser>(result);
            }

            return Task.FromResult<ApplicationUser>(null);
        }

        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("Null or empty argument: userId");
            }

            ApplicationUser result = userTable.GetUserById(userId) as ApplicationUser;
            if (result != null)
            {
                return Task.FromResult<ApplicationUser>(result);
            }

            return Task.FromResult<ApplicationUser>(null);
        }

        public Task<ApplicationUser?> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            if (loginProvider == null || providerKey == null)
            {
                throw new ArgumentNullException("login");
            }

            UserLoginInfo login = new UserLoginInfo(loginProvider, providerKey, loginProvider);

            var userId = userLoginsTable.FindUserIdByLogin(login);
            if (userId != null)
            {
                ApplicationUser user = userTable.GetUserById(userId) as ApplicationUser;
                if (user != null)
                {
                    return Task.FromResult<ApplicationUser>(user);
                }
            }

            return Task.FromResult<ApplicationUser>(null);
        }

        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(normalizedUserName))
            {
                throw new ArgumentException("Null or empty argument: normalizedUserName");
            }

            List<ApplicationUser> result = userTable.GetUserByName(normalizedUserName, false) as List<ApplicationUser>;

            // Should I throw if > 1 user?
            if (result != null && result.Count == 1)
            {
                return Task.FromResult<ApplicationUser>(result[0]);
            }

            return Task.FromResult<ApplicationUser>(null);
        }

        public Task<int> GetAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.AccessFailedCount);
        }

        public Task<IList<Claim>> GetClaimsAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            ClaimsIdentity identity = userClaimsTable.FindByUserId(user.Id);

            return Task.FromResult<IList<Claim>>(identity.Claims.ToList());
        }

        public Task<string?> GetEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.EmailConfirmed);
        }

        public Task<bool> GetLockoutEnabledAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.LockoutEnabled);
        }

        public Task<DateTimeOffset?> GetLockoutEndDateAsync(ApplicationUser user, CancellationToken cancellationToken)
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

        public Task<IList<UserLoginInfo>> GetLoginsAsync(ApplicationUser user, CancellationToken cancellationToken)
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

        public Task<string?> GetNormalizedEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.NormalizedEmail);
        }

        public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            if (user != null)
            {
                return Task.FromResult<string?>(userTable.GetUserName(user.Id));
            }

            return Task.FromResult<string?>(null);
        }

        public Task<string?> GetPasswordHashAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            if (user != null)
            {
                return Task.FromResult<string?>(userTable.GetPasswordHash(user.Id));
            }

            return Task.FromResult<string?>(null);
        }

        public Task<string?> GetPhoneNumberAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PhoneNumber);
        }

        public Task<bool> GetPhoneNumberConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PhoneNumberConfirmed);
        }

        public Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken)
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

        public Task<string?> GetSecurityStampAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.SecurityStamp);
        }

        public Task<bool> GetTwoFactorEnabledAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.TwoFactorEnabled);
        }

        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            if (user != null)
            {
                return Task.FromResult<string>(userTable.GetUserId(user.NormalizedUserName));
            }

            return Task.FromResult<string>(null);
        }

        public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            if (user != null)
            {
                //return Task.FromResult<string?>(userTable.GetUserName(user.Id));
                return Task.FromResult(user.UserName);
            }

            return Task.FromResult<string?>(null);
        }

        public Task<IList<ApplicationUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            var hasPassword = !string.IsNullOrEmpty(userTable.GetPasswordHash(user.Id));

            return Task.FromResult<bool>(Boolean.Parse(hasPassword.ToString()));
        }

        public Task<int> IncrementAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            user.AccessFailedCount++;
            userTable.Update(user);

            return Task.FromResult(user.AccessFailedCount);
        }

        public Task<bool> IsInRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
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
                if (roles != null)
                {
                    foreach (string role in roles)
                    {
                        if (role.ToUpper() == roleName.ToUpper())
                        {
                            return Task.FromResult<bool>(true);
                        }
                    }
                }
            }

            return Task.FromResult<bool>(false);
        }

        public Task RemoveClaimsAsync(ApplicationUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
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

        public Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
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

        public Task RemoveLoginAsync(ApplicationUser user, string loginProvider, string providerKey, CancellationToken cancellationToken)
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

        public Task ReplaceClaimAsync(ApplicationUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
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

        public Task ResetAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            user.AccessFailedCount = 0;
            userTable.Update(user);

            return Task.FromResult(0);
        }

        public Task SetEmailAsync(ApplicationUser user, string? email, CancellationToken cancellationToken)
        {
            user.Email = email;
            userTable.Update(user);

            return Task.FromResult(0);
        }

        public Task SetEmailConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken cancellationToken)
        {
            user.EmailConfirmed = confirmed;
            userTable.Update(user);

            return Task.FromResult(0);
        }

        public Task SetLockoutEnabledAsync(ApplicationUser user, bool enabled, CancellationToken cancellationToken)
        {
            user.LockoutEnabled = enabled;
            userTable.Update(user);

            return Task.FromResult(0);
        }

        public Task SetLockoutEndDateAsync(ApplicationUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
        {
            user.LockoutEnd = lockoutEnd;
            userTable.Update(user);

            return Task.FromResult(0);
        }

        public Task SetNormalizedEmailAsync(ApplicationUser user, string? normalizedEmail, CancellationToken cancellationToken)
        {
            user.NormalizedEmail = normalizedEmail;
            userTable.Update(user);

            return Task.FromResult(0);
        }

        public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            user.NormalizedUserName = normalizedName;
            userTable.Update(user);

            return Task.FromResult<IdentityResult>(IdentityResult.Success);
        }

        public Task SetPasswordHashAsync(ApplicationUser user, string? passwordHash, CancellationToken cancellationToken)
        {
            user.PasswordHash = passwordHash;

            return Task.FromResult<Object>(null);
        }

        public Task SetPhoneNumberAsync(ApplicationUser user, string? phoneNumber, CancellationToken cancellationToken)
        {
            user.PhoneNumber = phoneNumber;
            userTable.Update(user);

            return Task.FromResult(0);
        }

        public Task SetPhoneNumberConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken cancellationToken)
        {
            user.PhoneNumberConfirmed = confirmed;
            userTable.Update(user);

            return Task.FromResult(0);
        }

        public Task SetSecurityStampAsync(ApplicationUser user, string stamp, CancellationToken cancellationToken)
        {
            user.SecurityStamp = stamp;

            return Task.FromResult(0);
        }

        public Task SetTwoFactorEnabledAsync(ApplicationUser user, bool enabled, CancellationToken cancellationToken)
        {
            user.TwoFactorEnabled = enabled;
            userTable.Update(user);

            return Task.FromResult(0);
        }

        public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            user.UserName = userName;
            userTable.Update(user);

            return Task.FromResult<IdentityResult>(IdentityResult.Success);
        }

        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            userTable.Update(user);

            return Task.FromResult<IdentityResult>(IdentityResult.Success);
        }

        public Task<string?> GetAuthenticatorKeyAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            return Task.FromResult(userAuthenticatorKeysTable.GetKey(user.Id));
        }

        public Task SetAuthenticatorKeyAsync(ApplicationUser user, string key, CancellationToken cancellationToken)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            userAuthenticatorKeysTable.SetKey(user.Id, key);
            return Task.CompletedTask;
        }

        public Task ReplaceCodesAsync(ApplicationUser user, IEnumerable<string> recoveryCodes, CancellationToken cancellationToken)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (recoveryCodes == null) throw new ArgumentNullException(nameof(recoveryCodes));
            // Store hashed codes; Identity passes hashed strings here.
            userRecoveryCodesTable.ReplaceCodes(user.Id, recoveryCodes);
            return Task.CompletedTask;
        }

        public Task<bool> RedeemCodeAsync(ApplicationUser user, string code, CancellationToken cancellationToken)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            if (code == null) throw new ArgumentNullException(nameof(code));
            bool ok = userRecoveryCodesTable.RedeemCode(user.Id, code);
            return Task.FromResult(ok);
        }

        public Task<int> CountCodesAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));
            int count = userRecoveryCodesTable.CountCodes(user.Id);
            return Task.FromResult(count);
        }
    }
}
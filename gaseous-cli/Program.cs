using System;
using Authentication;
using gaseous_server.Classes;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/* ------------------------------------------------- */
/* This tool is a CLI tool that is used to manage    */
/* the Gaseous Server.                               */
/* Functions such as user management, and backups    */
/* are available.                                    */
/* ------------------------------------------------- */

// load app settings
Config.InitSettings();

// set up database connection
Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

// set up identity
IServiceCollection services = new ServiceCollection();
services.AddLogging();

services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 10;
            options.User.AllowedUserNameCharacters = null;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedPhoneNumber = false;
            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedAccount = false;
        })
    .AddUserStore<UserStore>()
    .AddRoleStore<RoleStore>()
    .AddDefaultTokenProviders()
    ;
services.AddScoped<UserStore>();
services.AddScoped<RoleStore>();

services.AddTransient<IUserStore<ApplicationUser>, UserStore>();
services.AddTransient<IRoleStore<ApplicationRole>, RoleStore>();
var userManager = services.BuildServiceProvider().GetService<UserManager<ApplicationUser>>();

// load the command line arguments
string[] cmdArgs = Environment.GetCommandLineArgs();

// check if the user has entered any arguments
if (cmdArgs.Length == 1)
{
    // no arguments were entered
    Console.WriteLine("Gaseous CLI - A tool for managing the Gaseous Server");
    Console.WriteLine("Usage: gaseous-cli [command] [options]");
    Console.WriteLine("Commands:");
    Console.WriteLine("  user [command] [options] - Manage users");
    Console.WriteLine("    2fa [subcommand] - 2FA operations: resetkey|getkey|enable|genrc|countrc|redeem");
    Console.WriteLine("  role [command] [options] - Manage roles");
    // Console.WriteLine("  backup [command] [options] - Manage backups");
    // Console.WriteLine("  restore [command] [options] - Restore backups");
    Console.WriteLine("  help - Display this help message");
    return;
}

// check if the user has entered the help command
if (cmdArgs[1] == "help")
{
    // display the help message
    Console.WriteLine("Gaseous CLI - A tool for managing the Gaseous Server");
    Console.WriteLine("Usage: gaseous-cli [command] [options]");
    Console.WriteLine("Commands:");
    Console.WriteLine("  user [command] [options] - Manage users");
    Console.WriteLine("    2fa [subcommand] - 2FA operations: resetkey|getkey|enable|genrc|countrc|redeem");
    Console.WriteLine("  role [command] [options] - Manage roles");
    // Console.WriteLine("  backup [command] [options] - Manage backups");
    // Console.WriteLine("  restore [command] [options] - Restore backups");
    Console.WriteLine("  help - Display this help message");
    return;
}

// check if the user has entered the user command
if (cmdArgs[1] == "user")
{
    // check if the user has entered any arguments
    if (cmdArgs.Length == 2)
    {
        // no arguments were entered
        Console.WriteLine("User Management");
        Console.WriteLine("Usage: gaseous-cli user [command] [options]");
        Console.WriteLine("Commands:");
        Console.WriteLine("  add [username] [password] - Add a new user");
        Console.WriteLine("  delete [username] - Delete a user");
        Console.WriteLine("  resetpassword [username] [password] - Reset a user's password");
        Console.WriteLine("  list - List all users");
        Console.WriteLine("  2fa [subcommand] - Manage 2FA (resetkey|getkey|enable|genrc|countrc|redeem)");
        return;
    }

    // check if the user has entered the add command
    if (cmdArgs[2] == "add")
    {
        // check if the user has entered the username and password
        if (cmdArgs.Length < 5)
        {
            // the username and password were not entered
            Console.WriteLine("Error: Please enter a username and password");
            return;
        }

        // add a new user
        UserTable<ApplicationUser> userTable = new UserTable<ApplicationUser>(db);
        if (userTable.GetUserByEmail(cmdArgs[3]) != null)
        {
            Console.WriteLine("Error: User already exists");
            return;
        }

        // create the user object
        ApplicationUser user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = cmdArgs[3],
            NormalizedEmail = cmdArgs[3].ToUpper(),
            EmailConfirmed = true,
            UserName = cmdArgs[3],
            NormalizedUserName = cmdArgs[3].ToUpper()
        };

        // create the password
        PasswordHasher<ApplicationUser> passwordHasher = new PasswordHasher<ApplicationUser>();
        user.PasswordHash = passwordHasher.HashPassword(user, cmdArgs[4]);

        await userManager.CreateAsync(user);
        await userManager.AddToRoleAsync(user, "Player");

        Console.WriteLine("User created successfully with default role: Player");

        return;
    }

    // 2FA subcommands
    if (cmdArgs[2] == "2fa")
    {
        // help
        if (cmdArgs.Length < 4)
        {
            Console.WriteLine("2FA Management");
            Console.WriteLine("Usage: gaseous-cli user 2fa [subcommand] [args]");
            Console.WriteLine("Subcommands:");
            Console.WriteLine("  enable [username] [true|false] - Enable/disable 2FA flag");
            Console.WriteLine("  resetkey [username] - Reset authenticator key and print it");
            Console.WriteLine("  getkey [username] - Display current authenticator key");
            Console.WriteLine("  genrc [username] [count] - Generate new recovery codes and print them");
            Console.WriteLine("  countrc [username] - Count remaining recovery codes");
            Console.WriteLine("  redeem [username] [code] - Redeem a recovery code");
            return;
        }

        var sub = cmdArgs[3].ToLowerInvariant();
        UserTable<ApplicationUser> userTable = new UserTable<ApplicationUser>(db);
        ApplicationUser user = userTable.GetUserByEmail(cmdArgs.Length > 4 ? cmdArgs[4] : "");
        if (user == null)
        {
            Console.WriteLine("Error: User not found");
            return;
        }

        if (sub == "enable")
        {
            if (cmdArgs.Length < 6)
            {
                Console.WriteLine("Error: Please provide true or false");
                return;
            }
            bool enabled = bool.Parse(cmdArgs[5]);
            await userManager.SetTwoFactorEnabledAsync(user, enabled);
            Console.WriteLine($"TwoFactorEnabled set to {enabled}");
            return;
        }
        else if (sub == "resetkey")
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            var key = await userManager.GetAuthenticatorKeyAsync(user);
            Console.WriteLine($"New Authenticator Key: {key}");
            return;
        }
        else if (sub == "getkey")
        {
            var key = await userManager.GetAuthenticatorKeyAsync(user);
            Console.WriteLine(key == null ? "(no key)" : key);
            return;
        }
        else if (sub == "genrc")
        {
            int count = 5;
            if (cmdArgs.Length >= 6) int.TryParse(cmdArgs[5], out count);
            var codes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, count);
            Console.WriteLine("Generated recovery codes:");
            foreach (var c in codes)
            {
                Console.WriteLine(c);
            }
            var remaining = await userManager.CountRecoveryCodesAsync(user);
            Console.WriteLine($"Remaining code count: {remaining}");
            return;
        }
        else if (sub == "countrc")
        {
            var remaining = await userManager.CountRecoveryCodesAsync(user);
            Console.WriteLine(remaining);
            return;
        }
        else if (sub == "redeem")
        {
            if (cmdArgs.Length < 6)
            {
                Console.WriteLine("Error: Please provide a recovery code to redeem");
                return;
            }
            var code = cmdArgs[5];
            var result = await userManager.RedeemTwoFactorRecoveryCodeAsync(user, code);
            Console.WriteLine(result.Succeeded ? "Redeemed" : "Failed to redeem");
            var remaining = await userManager.CountRecoveryCodesAsync(user);
            Console.WriteLine($"Remaining code count: {remaining}");
            return;
        }
        else
        {
            Console.WriteLine("Error: Unknown 2fa subcommand");
            return;
        }
    }

    // check if the user has entered the delete command
    if (cmdArgs[2] == "delete")
    {
        // check if the user has entered the username
        if (cmdArgs.Length < 4)
        {
            // the username was not entered
            Console.WriteLine("Error: Please enter a username");
            return;
        }

        // delete the user
        UserTable<ApplicationUser> userTable = new UserTable<ApplicationUser>(db);
        ApplicationUser user = userTable.GetUserByEmail(cmdArgs[3]);
        if (user == null)
        {
            Console.WriteLine("Error: User not found");
            return;
        }

        await userManager.DeleteAsync(user);

        Console.WriteLine("User deleted successfully");

        return;
    }

    // check if the user has entered the resetpassword command
    if (cmdArgs[2] == "resetpassword")
    {
        // check if the user has entered the username and password
        if (cmdArgs.Length < 5)
        {
            // the username and password were not entered
            Console.WriteLine("Error: Please enter a username and password");
            return;
        }

        // reset the user's password
        UserTable<ApplicationUser> userTable = new UserTable<ApplicationUser>(db);
        ApplicationUser user = userTable.GetUserByEmail(cmdArgs[3]);
        if (user == null)
        {
            Console.WriteLine("Error: User not found");
            return;
        }

        // create the password
        PasswordHasher<ApplicationUser> passwordHasher = new PasswordHasher<ApplicationUser>();
        user.PasswordHash = passwordHasher.HashPassword(user, cmdArgs[4]);

        await userManager.UpdateAsync(user);

        Console.WriteLine("Password reset successfully");

        return;
    }

    // check if the user has entered the list command
    if (cmdArgs[2] == "list")
    {
        // list all users
        UserTable<ApplicationUser> userTable = new UserTable<ApplicationUser>(db);
        var userList = userTable.GetUsers();
        foreach (var user in userList)
        {
            var roles = await userManager.GetRolesAsync(user);
            Console.WriteLine(user.Email + " - " + string.Join(", ", roles));
        }
        return;
    }
}

// check if the user has entered the role command
if (cmdArgs[1] == "role")
{
    // check if the user has entered any arguments
    if (cmdArgs.Length == 2)
    {
        // no arguments were entered
        Console.WriteLine("Role Management");
        Console.WriteLine("Usage: gaseous-cli role [command] [options]");
        Console.WriteLine("Commands:");
        Console.WriteLine("  set [username] [role] - Set the role of a user");
        Console.WriteLine("  list - List all roles");
        return;
    }

    // check if the user has entered the role command
    if (cmdArgs[2] == "set")
    {
        // check if the user has entered the username and role
        if (cmdArgs.Length < 5)
        {
            // the username and role were not entered
            Console.WriteLine("Error: Please enter a username and role");
            return;
        }

        // set the role of the user
        UserTable<ApplicationUser> userTable = new UserTable<ApplicationUser>(db);
        ApplicationUser user = userTable.GetUserByEmail(cmdArgs[3]);
        if (user == null)
        {
            Console.WriteLine("Error: User not found");
            return;
        }

        // remove all existing roles from user
        var roles = await userManager.GetRolesAsync(user);
        await userManager.RemoveFromRolesAsync(user, roles.ToArray());

        // add the new role to the user
        await userManager.AddToRoleAsync(user, cmdArgs[4]);

        Console.WriteLine("Role set successfully");

        return;
    }

    // check if the user has entered the list command
    if (cmdArgs[2] == "list")
    {
        // list all roles
        string[] roles = { "Player", "Gamer", "Admin" };
        foreach (var role in roles)
        {
            Console.WriteLine(role);
        }
        return;
    }
}

// // check if the user has entered the backup command
// if (cmdArgs[1] == "backup")
// {
//     // check if the user has entered any arguments
//     if (cmdArgs.Length == 2)
//     {
//         // no arguments were entered
//         Console.WriteLine("Backup Management");
//         Console.WriteLine("Usage: gaseous-cli backup [command] [options]");
//         Console.WriteLine("Commands:");
//         Console.WriteLine("  create - Create a backup");
//         Console.WriteLine("  list - List all backups");
//         Console.WriteLine("  remove [backup_id] - Remove a backup");
//         return;
//     }

//     // check if the user has entered the create command
//     if (cmdArgs[2] == "create")
//     {
//         // create a backup
//         Backup.CreateBackup();
//         return;
//     }

//     // check if the user has entered the list command
//     if (cmdArgs[2] == "list")
//     {
//         // list all backups
//         Backup.ListBackups();
//         return;
//     }

//     // check if the user has entered the remove command
//     if (cmdArgs[2] == "remove")
//     {
//         // check if the user has entered the backup id
//         if (cmdArgs.Length < 4)
//         {
//             // the backup id was not entered
//             Console.WriteLine("Error: Please enter a backup id");
//             return;
//         }

//         // remove the backup
//         Backup.RemoveBackup(cmdArgs[3]);
//         return;
//     }
// }

// // check if the user has entered the restore command
// if (cmdArgs[1] == "restore")
// {
//     // check if the user has entered any arguments
//     if (cmdArgs.Length == 2)
//     {
//         // no arguments were entered
//         Console.WriteLine("Restore Management");
//         Console.WriteLine("Usage: gaseous-cli restore [command] [options]");
//         Console.WriteLine("Commands:");
//         Console.WriteLine("  restore [backup_id] - Restore a backup");
//         return;
//     }

//     // check if the user has entered the restore command
//     if (cmdArgs[2] == "restore")
//     {
//         // check if the user has entered the backup id
//         if (cmdArgs.Length < 4)
//         {
//             // the backup id was not entered
//             Console.WriteLine("Error: Please enter a backup id");
//             return;
//         }

//         // restore the backup
//         Restore.RestoreBackup(cmdArgs[3]);
//         return;
//     }
// }

// the user entered an invalid command
Console.WriteLine("Error: Invalid command");
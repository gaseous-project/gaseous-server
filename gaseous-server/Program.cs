﻿using System.Reflection;
using System.Text.Json.Serialization;
using gaseous_server;
using gaseous_server.Classes;
using gaseous_server.Models;
using gaseous_server.SignatureIngestors.XML;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.OpenApi.Models;
using Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

Logging.WriteToDiskOnly = true;
Logging.Log(Logging.LogType.Information, "Startup", "Starting Gaseous Server " + Assembly.GetExecutingAssembly().GetName().Version);

Database db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionStringNoDatabase);

// check db availability
bool dbOnline = false;
do
{
    Logging.Log(Logging.LogType.Information, "Startup", "Waiting for database...");
    if (db.TestConnection() == true)
    {
        dbOnline = true;
    }
    else
    {
        Thread.Sleep(30000);
    }
} while (dbOnline == false);

db = new Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);

// set up db
db.InitDB();

// load app settings
Config.InitSettings();
// write updated settings back to the config file
Config.UpdateConfig();

// set initial values
Guid APIKey = Guid.NewGuid();
if (Config.ReadSetting("API Key", "Test API Key") == "Test API Key")
{
    // it's a new api key save it
    Logging.Log(Logging.LogType.Information, "Startup", "Setting initial API key");
    Config.SetSetting("API Key", APIKey.ToString());
}

// clean up storage
if (Directory.Exists(Config.LibraryConfiguration.LibraryTempDirectory))
{
    Directory.Delete(Config.LibraryConfiguration.LibraryTempDirectory, true);
}
if (Directory.Exists(Config.LibraryConfiguration.LibraryUploadDirectory))
{
    Directory.Delete(Config.LibraryConfiguration.LibraryUploadDirectory, true);
}

// kick off any delayed upgrade tasks
// run 1002 background updates in the background on every start
DatabaseMigration.BackgroundUpgradeTargetSchemaVersions.Add(1002);
// start the task
ProcessQueue.QueueItem queueItem = new ProcessQueue.QueueItem(
        ProcessQueue.QueueItemType.BackgroundDatabaseUpgrade,
        1,
        new List<ProcessQueue.QueueItemType>
        {
            ProcessQueue.QueueItemType.All
        },
        false,
        true
    );
queueItem.ForceExecute();
ProcessQueue.QueueItems.Add(queueItem);

// set up server
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(x =>
{
    // serialize enums as strings in api responses (e.g. Role)
    x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

    // suppress nulls
    x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

    // set max depth
    x.JsonSerializerOptions.MaxDepth = 64;
});
builder.Services.AddResponseCaching();
builder.Services.AddControllers(options =>
{
    options.CacheProfiles.Add("Default30",
        new CacheProfile()
        {
            Duration = 30
        });
    options.CacheProfiles.Add("5Minute",
        new CacheProfile()
        {
            Duration = 300,
            Location = ResponseCacheLocation.Any
        });
    options.CacheProfiles.Add("7Days",
        new CacheProfile()
        {
            Duration = 604800,
            Location = ResponseCacheLocation.Any
        });
});
builder.Services.AddApiVersioning(config =>
{
    config.DefaultApiVersion = new ApiVersion(1, 0);
    config.AssumeDefaultVersionWhenUnspecified = true;
    config.ReportApiVersions = true;
});
builder.Services.AddApiVersioning(setup =>
{
    setup.ApiVersionReader = new UrlSegmentApiVersionReader();
});
builder.Services.AddVersionedApiExplorer(setup =>
{
    setup.GroupNameFormat = "'v'VVV";
    setup.SubstituteApiVersionInUrl = true;
});

// set max upload size
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = int.MaxValue;
});
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = int.MaxValue;
});
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Version = "v1.0",
            Title = "Gaseous Server API",
            Description = "An API for managing the Gaseous Server",
            TermsOfService = new Uri("https://github.com/gaseous-project/gaseous-server"),
            Contact = new OpenApiContact
            {
                Name = "GitHub Repository",
                Url = new Uri("https://github.com/gaseous-project/gaseous-server")
            },
            License = new OpenApiLicense
            {
                Name = "Gaseous Server License",
                Url = new Uri("https://github.com/gaseous-project/gaseous-server/blob/main/LICENSE")
            }
        });

        // using System.Reflection;
        var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    }
);
builder.Services.AddHostedService<TimedHostedService>();

// identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
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
    .AddDefaultUI()
    ;
builder.Services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "Gaseous.Identity";
            options.ExpireTimeSpan = TimeSpan.FromDays(90);
            options.SlidingExpiration = true;
        });
// builder.Services.AddIdentityCore<ApplicationUser>(options => {
//         options.SignIn.RequireConfirmedAccount = false;
//         options.User.RequireUniqueEmail = true;
//         options.Password.RequireDigit = false;
//         options.Password.RequiredLength = 10;
//         options.Password.RequireNonAlphanumeric = false;
//         options.Password.RequireUppercase = false;
//         options.Password.RequireLowercase = false;
//     });
builder.Services.AddScoped<UserStore>();
builder.Services.AddScoped<RoleStore>();

builder.Services.AddTransient<IUserStore<ApplicationUser>, UserStore>();
builder.Services.AddTransient<IRoleStore<ApplicationRole>, RoleStore>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Manager", policy => policy.RequireRole("Manager"));
    options.AddPolicy("Member", policy => policy.RequireRole("Member"));
});

// builder.Services.AddControllersWithViews(options =>
// {
//     options.Filters.Add(new Microsoft.AspNetCore.Mvc.ValidateAntiForgeryTokenAttribute());
// });

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();

app.UseResponseCaching();

// set up system roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleStore>();
    var roles = new[] { "Admin", "Manager", "Member" };
 
    foreach (var role in roles)
    {
        if (await roleManager.FindByNameAsync(role, CancellationToken.None) == null)
        {
            ApplicationRole applicationRole = new ApplicationRole();
            applicationRole.Name = role;
            applicationRole.NormalizedName = role.ToUpper();
            await roleManager.CreateAsync(applicationRole, CancellationToken.None);
        }
    }

    // set up administrator account
    var userManager = scope.ServiceProvider.GetRequiredService<UserStore>();
    if (await userManager.FindByNameAsync("admin@localhost", CancellationToken.None) == null)
    {
        ApplicationUser adminUser = new ApplicationUser{
            Id = Guid.NewGuid().ToString(),
            Email = "admin@localhost",
            NormalizedEmail = "ADMIN@LOCALHOST",
            EmailConfirmed = true,
            UserName = "administrator",
            NormalizedUserName = "ADMINISTRATOR"
        };

        //set user password
        PasswordHasher<ApplicationUser> ph = new PasswordHasher<ApplicationUser>();
        adminUser.PasswordHash = ph.HashPassword(adminUser, "letmein");

        await userManager.CreateAsync(adminUser, CancellationToken.None);
        await userManager.AddToRoleAsync(adminUser, "Admin", CancellationToken.None);
    }
}

app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true, //allow unkown file types also to be served
    DefaultContentType = "plain/text" //content type to returned if fileType is not known.
});

app.MapControllers();

// setup library directories
Config.LibraryConfiguration.InitLibrary();

// insert unknown platform and game if not present
gaseous_server.Classes.Metadata.Games.GetGame(0, false, false, false);
gaseous_server.Classes.Metadata.Platforms.GetPlatform(0);

// extract platform map if not present
PlatformMapping.ExtractPlatformMap();

// add background tasks
ProcessQueue.QueueItems.Add(new ProcessQueue.QueueItem(
    ProcessQueue.QueueItemType.SignatureIngestor,
    60
    )
    );
ProcessQueue.QueueItems.Add(new ProcessQueue.QueueItem(
    ProcessQueue.QueueItemType.TitleIngestor,
    1,
    new List<ProcessQueue.QueueItemType>
    {
        ProcessQueue.QueueItemType.OrganiseLibrary,
        ProcessQueue.QueueItemType.LibraryScan
    })
    );
ProcessQueue.QueueItems.Add(new ProcessQueue.QueueItem(
    ProcessQueue.QueueItemType.MetadataRefresh,
    360
    )
    );
ProcessQueue.QueueItems.Add(new ProcessQueue.QueueItem(
    ProcessQueue.QueueItemType.OrganiseLibrary,
    1440,
    new List<ProcessQueue.QueueItemType>
    {
        ProcessQueue.QueueItemType.LibraryScan,
        ProcessQueue.QueueItemType.TitleIngestor,
        ProcessQueue.QueueItemType.Rematcher
    })
    );
ProcessQueue.QueueItems.Add(new ProcessQueue.QueueItem(
    ProcessQueue.QueueItemType.LibraryScan,
    60,
    new List<ProcessQueue.QueueItemType>
    {
        ProcessQueue.QueueItemType.OrganiseLibrary,
        ProcessQueue.QueueItemType.Rematcher
    })
    );
ProcessQueue.QueueItems.Add(new ProcessQueue.QueueItem(
    ProcessQueue.QueueItemType.Rematcher,
    1440,
    new List<ProcessQueue.QueueItemType>
    {
        ProcessQueue.QueueItemType.OrganiseLibrary,
        ProcessQueue.QueueItemType.LibraryScan
    })
    );
ProcessQueue.QueueItems.Add(new ProcessQueue.QueueItem(
    ProcessQueue.QueueItemType.Maintainer,
    10080,
    new List<ProcessQueue.QueueItemType>
    {
        ProcessQueue.QueueItemType.All
    })
    );

Logging.WriteToDiskOnly = false;

// start the app
app.Run();

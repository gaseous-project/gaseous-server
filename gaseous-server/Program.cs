using System.Reflection;
using System.Text.Json.Serialization;
using gaseous_server;
using gaseous_server.Classes;
using gaseous_server.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.OpenApi.Models;
using Authentication;
using Microsoft.AspNetCore.Identity;
using gaseous_server.Classes.Metadata;
using Asp.Versioning;
using HasheousClient.Models.Metadata.IGDB;

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
// create relation tables if they don't exist
await Storage.CreateRelationsTables<IGDB.Models.Game>();
await Storage.CreateRelationsTables<IGDB.Models.Platform>();

// populate db with static data for lookups
await AgeRatings.PopulateAgeMapAsync();

// load app settings
Config.InitSettings();
// write updated settings back to the config file
Config.UpdateConfig();

// update default library path
await GameLibrary.UpdateDefaultLibraryPathAsync();

// set api metadata source from config
Communications.MetadataSource = Config.MetadataConfiguration.DefaultMetadataSource;

// set up hasheous client
HasheousClient.WebApp.HttpHelper.BaseUri = Config.MetadataConfiguration.HasheousHost;

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
    options.CacheProfiles.Add("None",
        new CacheProfile()
        {
            Duration = 1
        });
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
    config.DefaultApiVersion = new ApiVersion(1, 1);
    config.AssumeDefaultVersionWhenUnspecified = true;
    config.ReportApiVersions = true;
    config.ApiVersionReader = ApiVersionReader.Combine(new UrlSegmentApiVersionReader(),
                                                    new HeaderApiVersionReader("x-api-version"),
                                                    new MediaTypeApiVersionReader("x-api-version"));
}).AddApiExplorer(setup =>
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
        // options.SwaggerDoc("v1", new OpenApiInfo
        // {
        //     Version = "v1.0",
        //     Title = "Gaseous Server API",
        //     Description = "An API for managing the Gaseous Server",
        //     TermsOfService = new Uri("https://github.com/gaseous-project/gaseous-server"),
        //     Contact = new OpenApiContact
        //     {
        //         Name = "GitHub Repository",
        //         Url = new Uri("https://github.com/gaseous-project/gaseous-server")
        //     },
        //     License = new OpenApiLicense
        //     {
        //         Name = "Gaseous Server License",
        //         Url = new Uri("https://github.com/gaseous-project/gaseous-server/blob/main/LICENSE")
        //     }
        // });

        options.SwaggerDoc("v1.1", new OpenApiInfo
        {
            Version = "v1.1",
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

        // sort the endpoints
        options.OrderActionsBy((apiDesc) => $"{apiDesc.RelativePath}_{apiDesc.HttpMethod}");
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
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });
builder.Services.AddScoped<UserStore>();
builder.Services.AddScoped<RoleStore>();

builder.Services.AddTransient<IUserStore<ApplicationUser>, UserStore>();
builder.Services.AddTransient<IRoleStore<ApplicationRole>, RoleStore>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Gamer", policy => policy.RequireRole("Gamer"));
    options.AddPolicy("Player", policy => policy.RequireRole("Player"));
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
app.UseSwaggerUI(options =>
    {
        // options.SwaggerEndpoint($"/swagger/v1/swagger.json", "v1.0");
        // options.SwaggerEndpoint($"/swagger/v1.1/swagger.json", "v1.1");

        var descriptions = app.DescribeApiVersions();
        foreach (var description in descriptions)
        {
            if (description.IsDeprecated == false)
            {
                var url = $"/swagger/{description.GroupName}/swagger.json";
                var name = description.GroupName.ToUpperInvariant();
                options.SwaggerEndpoint(url, name);
            }
        }
    }
);
//}

//app.UseHttpsRedirection();

app.UseResponseCaching();

// set up system roles
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleStore>();
    var roles = new[] { "Admin", "Gamer", "Player" };

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
}

app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true, //allow unkown file types also to be served
    DefaultContentType = "plain/text", //content type to returned if fileType is not known.
    OnPrepareResponse = ctx =>
    {
        if (ctx.Context.Request.QueryString.HasValue)
        {
            // get query items from query string
            var queryItems = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(ctx.Context.Request.QueryString.Value);
            if (queryItems.ContainsKey("page"))
            {
                if (queryItems["page"].ToString().ToLower() == "emulator")
                {
                    // set the CORS header
                    ctx.Context.Response.Headers.Add("Cross-Origin-Opener-Policy", "same-origin");
                    ctx.Context.Response.Headers.Add("Cross-Origin-Embedder-Policy", "require-corp");
                }
            }
        }
    }
});

app.MapControllers();

app.Use(async (context, next) =>
{
    // set the correlation id
    string correlationId = Guid.NewGuid().ToString();
    CallContext.SetData("CorrelationId", correlationId);
    CallContext.SetData("CallingProcess", context.Request.Method + ": " + context.Request.Path);

    string userIdentity;
    try
    {
        userIdentity = context.User.Claims.FirstOrDefault(x => x.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
    }
    catch
    {
        userIdentity = "";
    }
    CallContext.SetData("CallingUser", userIdentity);

    context.Response.Headers.Append("x-correlation-id", correlationId.ToString());
    await next();
});

// setup library directories
Config.LibraryConfiguration.InitLibrary();

// create unknown platform
await Platforms.GetPlatform(0, FileSignature.MetadataSources.None);
await Platforms.GetPlatform(0, FileSignature.MetadataSources.IGDB);
await Platforms.GetPlatform(0, FileSignature.MetadataSources.TheGamesDb);

// extract platform map if not present
await PlatformMapping.ExtractPlatformMap();

// migrate old firmware directory structure to new style
Bios.MigrateToNewFolderStructure();

// add background tasks
ProcessQueue.QueueItems.Add(new ProcessQueue.QueueItem(
ProcessQueue.QueueItemType.SignatureIngestor)
);
ProcessQueue.QueueItems.Add(new ProcessQueue.QueueItem(
    ProcessQueue.QueueItemType.TitleIngestor)
    );
ProcessQueue.QueueItems.Add(new ProcessQueue.QueueItem(
    ProcessQueue.QueueItemType.ImportQueueProcessor,
    1,
    false
));
ProcessQueue.QueueItems.Add(new ProcessQueue.QueueItem(
    ProcessQueue.QueueItemType.MetadataRefresh)
    );
ProcessQueue.QueueItems.Add(new ProcessQueue.QueueItem(
    ProcessQueue.QueueItemType.OrganiseLibrary)
    );
ProcessQueue.QueueItems.Add(new ProcessQueue.QueueItem(
    ProcessQueue.QueueItemType.LibraryScan)
    );

// maintenance tasks
ProcessQueue.QueueItem dailyMaintenance = new ProcessQueue.QueueItem(
    ProcessQueue.QueueItemType.DailyMaintainer
    );
ProcessQueue.QueueItems.Add(dailyMaintenance);

ProcessQueue.QueueItem weeklyMaintenance = new ProcessQueue.QueueItem(
    ProcessQueue.QueueItemType.WeeklyMaintainer
    );
ProcessQueue.QueueItems.Add(weeklyMaintenance);

ProcessQueue.QueueItem tempCleanup = new ProcessQueue.QueueItem(
    ProcessQueue.QueueItemType.TempCleanup
    );
ProcessQueue.QueueItems.Add(tempCleanup);

Logging.WriteToDiskOnly = false;

// start the app
await app.RunAsync();

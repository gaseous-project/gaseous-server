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
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using gaseous_server.Classes.Metadata;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.HttpOverrides;

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
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.RequireHeaderSymmetry = false;
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownProxies.Clear();
    options.KnownNetworks.Clear();
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    {
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
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
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
            options.Cookie.SameSite = SameSiteMode.Lax;
        });
builder.Services.ConfigureExternalCookie(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.None; // this one must be sent cross-site
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

// add social authentication
var authBuilder = builder.Services.AddAuthentication(o =>
    {
        o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie();

if (Config.SocialAuthConfiguration.GoogleAuthEnabled)
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = Config.SocialAuthConfiguration.GoogleClientId;
        options.ClientSecret = Config.SocialAuthConfiguration.GoogleClientSecret;
    });
}

if (Config.SocialAuthConfiguration.MicrosoftAuthEnabled)
{
    authBuilder.AddMicrosoftAccount(options =>
    {
        options.ClientId = Config.SocialAuthConfiguration.MicrosoftClientId;
        options.ClientSecret = Config.SocialAuthConfiguration.MicrosoftClientSecret;
        options.SaveTokens = true;
        options.AuthorizationEndpoint = "https://login.microsoftonline.com/consumers/oauth2/v2.0/authorize";
        options.TokenEndpoint = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";
    });
}

if (Config.SocialAuthConfiguration.OIDCAuthEnabled)
{
    // Normalize incoming claims (avoid legacy remapping)
    JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

    authBuilder.AddOpenIdConnect("OIDC", options =>
    {
        // remove the end path '/.well-known/openid-configuration' from the authority URL
        if (Config.SocialAuthConfiguration.OIDCAuthority.EndsWith("/.well-known/openid-configuration"))
        {
            options.Authority = Config.SocialAuthConfiguration.OIDCAuthority.Substring(0, Config.SocialAuthConfiguration.OIDCAuthority.Length - "/.well-known/openid-configuration".Length);
        }
        else
        {
            options.Authority = Config.SocialAuthConfiguration.OIDCAuthority;
        }

        options.ClientId = Config.SocialAuthConfiguration.OIDCClientId;
        options.ClientSecret = Config.SocialAuthConfiguration.OIDCClientSecret;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;
        options.SaveTokens = true;

        options.CallbackPath = "/signin-oidc";
        options.SignedOutCallbackPath = "/signout-oidc";

        // Standard scopes for all providers
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.GetClaimsFromUserInfoEndpoint = true;

        // Map raw provider claims, normalize in events
        options.ClaimActions.Clear();
        options.ClaimActions.MapUniqueJsonKey("sub", "sub");          // universal subject
        options.ClaimActions.MapUniqueJsonKey("oid", "oid");          // Entra ID object id
        options.ClaimActions.MapUniqueJsonKey(ClaimTypes.Email, "email");
        options.ClaimActions.MapUniqueJsonKey(ClaimTypes.Name, "name");
        options.ClaimActions.MapUniqueJsonKey("preferred_username", "preferred_username");
        options.ClaimActions.MapUniqueJsonKey("upn", "upn");          // Entra/legacy
        options.ClaimActions.MapJsonKey("groups", "groups");          // Authelia/Keycloak/Authentik
        options.ClaimActions.MapJsonKey("realm_access", "realm_access");      // Keycloak roles
        options.ClaimActions.MapJsonKey("resource_access", "resource_access"); // Keycloak client roles

        options.Events = new OpenIdConnectEvents
        {
            OnTokenValidated = ctx =>
            {
                var identity = ctx.Principal?.Identity as ClaimsIdentity;
                if (identity == null)
                {
                    ctx.Fail("No identity.");
                    return Task.CompletedTask;
                }

                // Stable external login key:
                // - Entra ID: oid
                // - Keycloak/Authelia/Authentik: sub
                var oid = identity.FindFirst("oid")?.Value;
                var sub = identity.FindFirst("sub")?.Value;
                var providerKey = !string.IsNullOrWhiteSpace(oid) ? oid : sub;

                foreach (var c in identity.FindAll(ClaimTypes.NameIdentifier).ToList())
                    identity.RemoveClaim(c);

                if (string.IsNullOrWhiteSpace(providerKey))
                {
                    ctx.Fail("Missing subject (oid/sub).");
                    return Task.CompletedTask;
                }
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, providerKey));

                // Ensure Name
                if (identity.FindFirst(ClaimTypes.Name) == null)
                {
                    var name = identity.FindFirst("name")?.Value
                               ?? identity.FindFirst("preferred_username")?.Value
                               ?? identity.FindFirst(ClaimTypes.Email)?.Value
                               ?? providerKey;
                    identity.AddClaim(new Claim(ClaimTypes.Name, name));
                }

                // Ensure Email (safe fallback only if it looks like an email)
                var email = identity.FindFirst(ClaimTypes.Email)?.Value ?? identity.FindFirst("email")?.Value;
                if (string.IsNullOrWhiteSpace(email))
                {
                    var candidate = identity.FindFirst("preferred_username")?.Value
                                    ?? identity.FindFirst("upn")?.Value;
                    if (!string.IsNullOrWhiteSpace(candidate) && candidate.Contains("@"))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Email, candidate));
                    }
                }

                return Task.CompletedTask;
            }
        };

        // Keep claim types predictable
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };
    });
}

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

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();

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

app.UseAuthentication();
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
                    ctx.Context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
                    ctx.Context.Response.Headers.Append("Cross-Origin-Embedder-Policy", "require-corp");
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
foreach (FileSignature.MetadataSources source in Enum.GetValues(typeof(FileSignature.MetadataSources)))
{
    await Platforms.GetPlatform(0, source);
}

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

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
using System.Net;
using System.Linq;
using Microsoft.Extensions.Hosting.WindowsServices;

// Defer heavy startup work so Windows Service can report RUNNING quickly

// set up server
var builder = WebApplication.CreateBuilder(args);

// Enable Windows Service support when running on Windows as a service
if (OperatingSystem.IsWindows())
{
    builder.Host.UseWindowsService(options =>
    {
        // Use the short service name without spaces; display name is set during service creation
        options.ServiceName = "GaseousServer";
    });

    // Bind Kestrel to the configured HTTP port
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(Config.ServerPort);
    });

    // When running as a Windows Service there is no console; route logs to Windows Event Log too.
    // Suppress CA1416 analyzer as this is guarded by a runtime OS check.
#pragma warning disable CA1416
    builder.Logging.AddEventLog();
#pragma warning restore CA1416
}

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
    options.RequireHeaderSymmetry = Config.ReverseProxyConfiguration.RequireHeaderSymmetry;
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;

    // Replace any defaults
    options.KnownProxies.Clear();
    options.KnownNetworks.Clear();

    // Known proxies (IP addresses)
    if (Config.ReverseProxyConfiguration.KnownProxies != null)
    {
        foreach (var proxy in Config.ReverseProxyConfiguration.KnownProxies)
        {
            if (IPAddress.TryParse(proxy, out var ip))
            {
                options.KnownProxies.Add(ip);
            }
            else if (!string.IsNullOrWhiteSpace(proxy))
            {
                Logging.LogKey(Logging.LogType.Warning, "process.forwardedheaders", "forwardedheaders.invalid_knownproxy_ip", null, new string[] { proxy });
            }
        }
    }

    // Known networks (CIDR)
    if (Config.ReverseProxyConfiguration.KnownNetworks != null)
    {
        foreach (var network in Config.ReverseProxyConfiguration.KnownNetworks)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(network)) continue;
                var parts = network.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length != 2)
                {
                    Logging.LogKey(Logging.LogType.Warning, "process.forwardedheaders", "forwardedheaders.invalid_knownnetwork", null, new string[] { network });
                    continue;
                }
                if (!IPAddress.TryParse(parts[0], out var prefix))
                {
                    Logging.LogKey(Logging.LogType.Warning, "process.forwardedheaders", "forwardedheaders.invalid_knownnetwork_address", null, new string[] { parts[0] });
                    continue;
                }
                if (!int.TryParse(parts[1], out var prefixLen))
                {
                    Logging.LogKey(Logging.LogType.Warning, "process.forwardedheaders", "forwardedheaders.invalid_knownnetwork_prefixlen", null, new string[] { parts[1] });
                    continue;
                }
                options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(prefix, prefixLen));
            }
            catch (Exception ex)
            {
                Logging.LogKey(Logging.LogType.Warning, "process.forwardedheaders", "forwardedheaders.failed_add_knownnetwork", null, new string[] { network }, ex);
            }
        }
    }
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
builder.Services.AddHostedService<StartupInitializer>();

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

// Use the configured ForwardedHeadersOptions from DI (includes KnownProxies/Networks)
app.UseForwardedHeaders();

app.UseHttpsRedirection();

app.UseResponseCaching();

// Heavy initialization moved to StartupInitializer (BackgroundService)

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

// Heavy initialization moved to StartupInitializer (BackgroundService)

// Start the web server explicitly so we only report RUNNING after Kestrel is accepting connections
await app.StartAsync();
try
{
    Logging.LogKey(Logging.LogType.Information, "process.startup", "startup.web_server_ready");
}
catch { /* logging should not block startup */ }
await app.WaitForShutdownAsync();

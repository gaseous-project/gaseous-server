using System.Text.Json.Serialization;
using gaseous_server;
using gaseous_tools;
using Microsoft.AspNetCore.Mvc;

Logging.Log(Logging.LogType.Information, "Startup", "Starting Gaseous Server");

// set up db
Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
db.InitDB();

// load app settings
Config.InitSettings();

// set initial values
Guid APIKey = Guid.NewGuid();
if (Config.ReadSetting("API Key", "Test API Key") == "Test API Key")
{
    // it's a new api key save it
    Logging.Log(Logging.LogType.Information, "Startup", "Setting initial API key");
    Config.SetSetting("API Key", APIKey.ToString());
}

// set up server
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers().AddJsonOptions(x =>
{
    // serialize enums as strings in api responses (e.g. Role)
    x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

    // suppress nulls
    x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
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

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<TimedHostedService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();

app.UseResponseCaching();

app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

// setup library directories
Config.LibraryConfiguration.InitLibrary();

// insert unknown platform and game if not present
gaseous_server.Classes.Metadata.Games.GetGame(0, false, false);
gaseous_server.Classes.Metadata.Platforms.GetPlatform(0);

// organise library
//gaseous_server.Classes.ImportGame.OrganiseLibrary();

// add background tasks
ProcessQueue.QueueItems.Add(new ProcessQueue.QueueItem(ProcessQueue.QueueItemType.SignatureIngestor, 60));
ProcessQueue.QueueItems.Add(new ProcessQueue.QueueItem(
    ProcessQueue.QueueItemType.TitleIngestor, 1,
    new List<ProcessQueue.QueueItemType>
    {
        ProcessQueue.QueueItemType.OrganiseLibrary,
        ProcessQueue.QueueItemType.LibraryScan
    })
    );
ProcessQueue.QueueItems.Add(new ProcessQueue.QueueItem(ProcessQueue.QueueItemType.MetadataRefresh, 360));
ProcessQueue.QueueItems.Add(new ProcessQueue.QueueItem(
    ProcessQueue.QueueItemType.OrganiseLibrary, 2040, new List<ProcessQueue.QueueItemType>
    {
        ProcessQueue.QueueItemType.LibraryScan,
        ProcessQueue.QueueItemType.TitleIngestor
    })
    );
ProcessQueue.QueueItems.Add(new ProcessQueue.QueueItem(
    ProcessQueue.QueueItemType.LibraryScan, 30, new List<ProcessQueue.QueueItemType>
    {
        ProcessQueue.QueueItemType.TitleIngestor,
        ProcessQueue.QueueItemType.OrganiseLibrary
    })
    );

// start the app
app.Run();

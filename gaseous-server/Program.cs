using System.Text.Json.Serialization;
using gaseous_server;
using gaseous_tools;

Logging.Log(Logging.LogType.Information, "Startup", "Starting Gaseous Server");

// set up db
Database db = new gaseous_tools.Database(Database.databaseType.MySql, Config.DatabaseConfiguration.ConnectionString);
db.InitDB();

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
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<TimedHostedService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// setup library directories
Config.LibraryConfiguration.InitLibrary();

// add background tasks
ProcessQueue.QueueItems.Add(new ProcessQueue.QueueItem(ProcessQueue.QueueItemType.SignatureIngestor, 60));
ProcessQueue.QueueItems.Add(new ProcessQueue.QueueItem(ProcessQueue.QueueItemType.TitleIngestor, 1));

// start the app
app.Run();

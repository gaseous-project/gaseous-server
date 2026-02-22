// start command line parser
using gaseous_server.Classes;
using gaseous_server.ProcessQueue;
using gaseous_server.ProcessQueue.Plugins;
using GaseousServerHost.Classes.CLI;

string[] cmdArgs = Environment.GetCommandLineArgs();

// Parse the command line arguments
if (cmdArgs.Length == 1 || cmdArgs.Contains("--help"))
{
    // No arguments provided, display usage
    Help.DisplayHelp();
    return;
}

// Check for version argument
if (cmdArgs.Contains("--version"))
{
    Console.WriteLine("Gaseous Server Host Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
    return;
}

// process other command line arguments
string serviceName = null;
string reportingServerUrl = null;
string correlationId = null;

for (int i = 0; i < cmdArgs.Length; i++)
{
    if (cmdArgs[i] == "--service" && i + 1 < cmdArgs.Length)
    {
        serviceName = cmdArgs[i + 1];
    }
    else if (cmdArgs[i] == "--reportingserver" && i + 1 < cmdArgs.Length)
    {
        reportingServerUrl = cmdArgs[i + 1];
    }
    else if (cmdArgs[i] == "--correlationid" && i + 1 < cmdArgs.Length)
    {
        correlationId = cmdArgs[i + 1];
    }
}

// If no service name is provided, display help
if (string.IsNullOrEmpty(serviceName))
{
    Console.WriteLine("Error: No service name provided.");
    Help.DisplayHelp();
    return;
}

// verify the service name can be parsed as Classes.ProcessQueue.QueueItemType, and is not "All" or "NotConfigured"
if (!Enum.TryParse(serviceName, out QueueItemType taskType) || taskType == QueueItemType.All || taskType == QueueItemType.NotConfigured)
{
    Console.WriteLine($"Error: Invalid service name '{serviceName}'.");
    Help.DisplayHelp();
    return;
}

// If no reporting server URL is provided, abort
if (string.IsNullOrEmpty(reportingServerUrl))
{
    Console.WriteLine("Error: No reporting server URL provided.");
    Help.DisplayHelp();
    return;
}

// If a correlation ID is provided, set it in the CallContext
if (string.IsNullOrEmpty(correlationId))
{
    // If no correlation ID is provided, generate a new one
    correlationId = Guid.NewGuid().ToString();
}
CallContext.SetData("CorrelationId", correlationId);
CallContext.SetData("CallingProcess", taskType.ToString());
CallContext.SetData("CallingUser", "System");
CallContext.SetData("ReportingServerUrl", reportingServerUrl);
CallContext.SetData("OutProcess", true);

// Initialize the service with the provided configuration
gaseous_server.ProcessQueue.QueueProcessor.QueueItem Task = new QueueProcessor.QueueItem(taskType, false, false)
{
    CorrelationId = correlationId
};
Task.ForceExecute();

// start the task
try
{
    // Settings
    Config.InitSettings();

    // API metadata source
    HasheousClient.WebApp.HttpHelper.BaseUri = Config.MetadataConfiguration.HasheousHost;

    Console.WriteLine($"Starting service '{serviceName}' with reporting server '{reportingServerUrl}' and correlation ID '{correlationId}'.");
    await Task.Execute();
}
catch (Exception ex)
{
    Console.WriteLine($"An error occurred while executing service: {ex.Message}");
    // terminate the application with a non-zero exit code
    Environment.Exit(1);
}

// Log the successful completion of the service
Console.WriteLine("Service completed successfully.");
Environment.Exit(0); // exit with success code
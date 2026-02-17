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
string processId = Guid.Empty.ToString();
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
    else if (cmdArgs[i] == "--processid" && i + 1 < cmdArgs.Length)
    {
        processId = cmdArgs[i + 1];
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
CallContext.SetData("ProcessId", processId);
CallContext.SetData("CorrelationId", correlationId);
CallContext.SetData("CallingProcess", taskType.ToString());
CallContext.SetData("CallingUser", "System");

// Initialize the service with the provided configuration
ITaskPlugin? Task;

// Find the task plugin class that matches the taskType
var taskPluginType = typeof(ITaskPlugin).Assembly.GetTypes()
    .Where(t => t.Namespace == "gaseous_server.ProcessQueue.Plugins" &&
                typeof(ITaskPlugin).IsAssignableFrom(t) &&
                !t.IsInterface &&
                !t.IsAbstract &&
                t.Name.Equals(taskType.ToString(), StringComparison.OrdinalIgnoreCase))
    .FirstOrDefault();

if (taskPluginType == null)
{
    Console.WriteLine($"Error: No plugin found for service type '{serviceName}'.");
    return;
}

Task = (ITaskPlugin?)Activator.CreateInstance(taskPluginType);

if (Task == null)
{
    Console.WriteLine($"Error: Failed to instantiate plugin for service type '{serviceName}'.");
    return;
}

// start the task
try
{
    Console.WriteLine($"Starting service '{serviceName}' with reporting server '{reportingServerUrl}' and process ID '{processId}'.");
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
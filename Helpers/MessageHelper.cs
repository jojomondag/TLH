using Serilog;
using Serilog.Formatting.Json;
using System.Collections.Concurrent;
using System.Text.Json;

public static class MessageHelper
{
    private static readonly ConcurrentQueue<string> Messages = new ConcurrentQueue<string>();
    public static bool ConsoleLoggingEnabled { get; set; } = true;
    public static bool JsonLoggingEnabled { get; set; } = true;
    private static readonly string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    private static readonly string JsonFilePath = Path.Combine(DesktopPath, "log.json");

    private static readonly ILogger Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .WriteTo.File(new JsonFormatter(), "log.json")
        .CreateLogger();

    public static async Task SaveMessageAsync(string message)
    {
        if (ConsoleLoggingEnabled)
        {
            Logger.Information(message);
        }

        var successMessage = new
        {
            Success = message,
            Timestamp = DateTime.UtcNow
        };

        Messages.Enqueue(JsonSerializer.Serialize(successMessage));

        if (JsonLoggingEnabled)
        {
            await SaveMessagesToJsonFileAsync();
        }
    }

    public static async Task SaveErrorAsync(string error)
    {
        if (ConsoleLoggingEnabled)
        {
            Logger.Error(error);
        }

        var errorMessage = new
        {
            Error = error,
            Timestamp = DateTime.UtcNow
        };

        Messages.Enqueue(JsonSerializer.Serialize(errorMessage));

        if (JsonLoggingEnabled)
        {
            await SaveMessagesToJsonFileAsync();
        }
    }

    public static async Task<string?> GetInputAsync()
    {
        return ConsoleLoggingEnabled ? await Task.Run(() => Console.ReadLine()) : string.Empty;
    }

    public static async Task SaveMessagesToJsonFileAsync()
    {
        if (JsonLoggingEnabled)
        {
            var logEvents = Messages.ToArray();

            var errorMessages = new List<string>();
            var successMessages = new List<string>();

            foreach (var message in logEvents)
            {
                if (message.StartsWith("{") && message.Contains("\"Error\""))
                {
                    errorMessages.Add(message);
                }
                else if (message.StartsWith("{") && message.Contains("\"Success\""))
                {
                    successMessages.Add(message);
                }
            }

            var json = JsonSerializer.Serialize(new { Errors = errorMessages, Successful = successMessages }, new JsonSerializerOptions { WriteIndented = true });

            // Use a FileStream with FileShare.ReadWrite to allow other processes to read/write the file concurrently
            using (var fileStream = new FileStream(JsonFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                await using (var writer = new StreamWriter(fileStream))
                {
                    await writer.WriteAsync(json);
                }
            }
        }
    }
}
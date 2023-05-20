using Serilog;
using Serilog.Formatting.Json;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System;
using System.Text;

public class SuccessMessage
{
    public string Success { get; set; }
    public DateTime Timestamp { get; set; }
}
public class ErrorMessage
{
    public string Error { get; set; }
    public DateTime Timestamp { get; set; }
}
public static class MessageHelper
{
    private static readonly ConcurrentQueue<object> Messages = new ConcurrentQueue<object>();
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

        var successMessage = new SuccessMessage
        {
            Success = message,
            Timestamp = DateTime.UtcNow
        };

        Messages.Enqueue(successMessage);

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

        var errorMessage = new ErrorMessage
        {
            Error = error,
            Timestamp = DateTime.UtcNow
        };

        Messages.Enqueue(errorMessage);

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
                if (message is ErrorMessage errorMessage)
                {
                    errorMessages.Add($"Timestamp: {errorMessage.Timestamp:T}, {errorMessage.Error}");
                }
                else if (message is SuccessMessage successMessage)
                {
                    successMessages.Add($"Timestamp: {successMessage.Timestamp:T}, {successMessage.Success}");
                }
            }

            var aggregatedMessages = new
            {
                Errors = errorMessages,
                Successful = successMessages
            };

            var json = JsonSerializer.Serialize(aggregatedMessages, new JsonSerializerOptions { WriteIndented = true });

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

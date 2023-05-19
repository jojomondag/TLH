using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

public class LogEntry
{
    public string Message { get; set; }
    public string Color { get; set; }
}

public static class MessageHelper
{
    private static readonly ConcurrentQueue<LogEntry> Messages = new ConcurrentQueue<LogEntry>();
    public static bool ConsoleLoggingEnabled { get; set; } = true;
    public static bool JsonLoggingEnabled { get; set; } = true;
    private static readonly string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

    private static string GetColorCode(ConsoleColor color)
    {
        return color.ToString();
    }

    public static async Task SaveMessageAsync(string message)
    {
        await Task.CompletedTask; // Replace this with actual asynchronous logic if needed.

        if (ConsoleLoggingEnabled)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }

        Messages.Enqueue(new LogEntry { Message = message, Color = GetColorCode(ConsoleColor.Green) });
    }

    public static async Task SaveErrorAsync(string error)
    {
        await Task.CompletedTask; // Replace this with actual asynchronous logic if needed.

        if (ConsoleLoggingEnabled)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(error);
            Console.ForegroundColor = originalColor;
        }

        Messages.Enqueue(new LogEntry { Message = $"Error: {error}", Color = GetColorCode(ConsoleColor.Red) });
    }

    public static async Task<string> GetInputAsync()
    {
        await Task.CompletedTask; // Replace this with actual asynchronous logic if needed.
        return ConsoleLoggingEnabled ? Console.ReadLine() : string.Empty;
    }

    public static async Task SaveMessagesToJsonFileAsync()
    {
        if (JsonLoggingEnabled)
        {
            var json = JsonSerializer.Serialize(Messages, new JsonSerializerOptions { WriteIndented = true });
            var filePath = Path.Combine(DesktopPath, "log.json");

            await File.WriteAllTextAsync(filePath, json).ConfigureAwait(false);
        }
    }

    public static void SaveMessage(string message)
    {
        if (ConsoleLoggingEnabled)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(message);
            Console.ForegroundColor = originalColor;
        }

        Messages.Enqueue(new LogEntry { Message = message, Color = GetColorCode(ConsoleColor.Green) });
    }

    public static void SaveError(string error)
    {
        if (ConsoleLoggingEnabled)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(error);
            Console.ForegroundColor = originalColor;
        }

        Messages.Enqueue(new LogEntry { Message = $"Error: {error}", Color = GetColorCode(ConsoleColor.Red) });
    }

    public static string GetInput()
    {
        return ConsoleLoggingEnabled ? Console.ReadLine() : string.Empty;
    }
    public static void SaveMessagesToJsonFile()
    {
        if (JsonLoggingEnabled)
        {
            var json = JsonSerializer.Serialize(Messages, new JsonSerializerOptions { WriteIndented = true });
            var filePath = Path.Combine(DesktopPath, "log.json");

            Console.WriteLine($"Saving JSON file to: {filePath}"); // Add this line

            File.WriteAllText(filePath, json);
        }
    }
}

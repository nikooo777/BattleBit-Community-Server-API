using System.Text;

public class ChatLogger
{
    private static readonly object FileLock = new();


    public static void Log(string message, string steamId)
    {
        Task.Run(() => WriteToFileAsync($"{DateTime.Now}: {steamId} - {message}"));
    }

    private static Task WriteToFileAsync(string message)
    {
        var fileName = $"chatlog_{DateTime.Today:yyyyMMdd}.log";
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), fileName);

        lock (FileLock)
        {
            try
            {
                using var stream = new FileStream(fullPath, FileMode.Append, FileAccess.Write, FileShare.Read);
                using var writer = new StreamWriter(stream, Encoding.UTF8);
                writer.WriteLine(message);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to write to log: {ex.Message}");
                throw;
            }
        }

        return Task.CompletedTask;
    }
}
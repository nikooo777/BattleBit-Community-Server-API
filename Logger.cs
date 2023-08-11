using System.Collections.Concurrent;
using System.Text;
using MySql.Data.MySqlClient;

public class Logger
{
    private const int MaxMessages = 50;
    private const int TimerInterval = 30000; // 30 seconds
    private static readonly object FileLock = new();
    private static readonly ConcurrentQueue<(string Message, string PlayerId)> MessageQueue = new();
    private static Timer _timer;

    private readonly string mConnectionString;

    public Logger(string connectionString)
    {
        mConnectionString = connectionString;
        _timer = new Timer(FlushToDatabase, null, TimerInterval, TimerInterval);
    }

    public void Log(string message, string playerId)
    {
        Task.Run(() => WriteToFileAsync($"{DateTime.Now}: {playerId} - {message}"));
        MessageQueue.Enqueue((message, playerId));

        if (MessageQueue.Count >= MaxMessages) FlushToDatabase(null);
    }

    private void FlushToDatabase(object state)
    {
        if (!MessageQueue.Any())
            return;

        using var connection = new MySqlConnection(mConnectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            var command =
                new MySqlCommand(
                    "INSERT INTO ChatLogs (Message, PlayerId, Timestamp) VALUES (@message, @playerId, @timestamp)",
                    connection, transaction);
            command.Parameters.Add("@message", MySqlDbType.Text);
            command.Parameters.Add("@playerId", MySqlDbType.VarChar);
            command.Parameters.Add("@timestamp", MySqlDbType.DateTime);

            while (MessageQueue.TryDequeue(out var log))
            {
                command.Parameters["@message"].Value = log.Message;
                command.Parameters["@playerId"].Value = log.PlayerId;
                command.Parameters["@timestamp"].Value = DateTime.Now;

                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static async Task WriteToFileAsync(string message)
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
    }
}
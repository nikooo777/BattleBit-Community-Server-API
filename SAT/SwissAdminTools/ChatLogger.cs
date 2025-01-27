using System.Collections.Concurrent;
using SAT.Db;
using SAT.Models;
using SwissAdminTools;

namespace SAT.SwissAdminTools;

public class ChatLogger
{
    private const int MaxMessages = 50;
    private const int TimerInterval = 10000; // 10 seconds
    private static readonly ConcurrentQueue<(string Message, int PlayerId)> MessageQueue = new();
    private static Timer _timer = new(FlushToDatabase, null, TimerInterval, TimerInterval);


    public static void StoreChatLog(MyPlayer player, string message)
    {
        MessageQueue.Enqueue((message, player.DbId));
        if (MessageQueue.Count >= MaxMessages)
            Task.Run(() => { FlushToDatabase(null); });
    }

    private static void FlushToDatabase(object? state)
    {
        if (!MessageQueue.Any())
            return;
        while (MessageQueue.TryDequeue(out var log))
        {
            var db = MyGameServer.Db;
            db.ChatLogs.Add(new ChatLog
            {
                Message = log.Message,
                PlayerId = log.PlayerId,
                Timestamp = DateTime.UtcNow
            });
            db.SaveChanges();
            DbContextPool.ReturnContext(db);
        }
    }
}
using System.Collections.Concurrent;
using SAT.Models;
using SAT.Storage;
using SwissAdminTools;

namespace SAT.SwissAdminTools;

public class ChatLogger
{
    private const int MaxMessages = 50;
    private const int TimerInterval = 10000; // 10 seconds
    private static readonly ConcurrentQueue<(string Message, int PlayerId)> MessageQueue = new();
    private static Timer _timer = new(FlushToDatabase, null, TimerInterval, TimerInterval);


    public static void StoreChatLog(ulong steamId, string message)
    {
        var p = PlayersManager.GetPlayer(steamId);
        if (p == null) return;
        MessageQueue.Enqueue((message, p.Id));
        if (MessageQueue.Count >= MaxMessages)
            Task.Run(() => { FlushToDatabase(null); });
    }

    private static void FlushToDatabase(object? state)
    {
        if (!MessageQueue.Any())
            return;
        while (MessageQueue.TryDequeue(out var log))
            MyGameServer.Db.ChatLogs.Add(new ChatLog
            {
                Message = log.Message,
                PlayerId = log.PlayerId,
                Timestamp = DateTime.UtcNow
            });
        MyGameServer.Db.SaveChanges();
    }
}
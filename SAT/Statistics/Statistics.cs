using SAT.Db;
using SAT.Models;
using SwissAdminTools;

namespace SAT.Statistics;

public static class Statistics
{
    public static void TrackPlayerCount(MyGameServer server)
    {
        var lastPlayerCount = -1;
        Thread.Sleep(1000 * 10); // wait 10 seconds before starting
        while (true)
            //persist the player count every 1 minute if the count has changed
            if (server.CurrentPlayerCount != lastPlayerCount)
                try
                {
                    var db = MyGameServer.Db;
                    lastPlayerCount = server.CurrentPlayerCount;
                    db.Stats.Add(new Stat
                    {
                        PlayerCount = server.CurrentPlayerCount,
                        CreatedAt = default,
                        UpdatedAt = default
                    });
                    db.SaveChanges();
                    DbContextPool.ReturnContext(db);
                    Thread.Sleep(1000 * 60); // 1 minute
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
    }
}
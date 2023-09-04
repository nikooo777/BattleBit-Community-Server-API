using SAT.Models;
using SwissAdminTools;

namespace SAT.Statistics;

public static class Statistics
{
    public static void TrackPlayerCount(MyGameServer server)
    {
        var lastPlayerCount = -1;
        while (true)
            //persist the player count every 1 minute if the count has changed
            if (server.CurrentPlayerCount != lastPlayerCount)
                try
                {
                    lastPlayerCount = server.CurrentPlayerCount;
                    MyGameServer.Db.Stats.Add(new Stat
                    {
                        PlayerCount = server.CurrentPlayerCount,
                        CreatedAt = default,
                        UpdatedAt = default
                    });
                    MyGameServer.Db.SaveChanges();
                    Thread.Sleep(1000 * 60); // 1 minute
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
    }
}
using BattleBitAPI.Common;
using BattleBitAPI.Common.Threading;
using Microsoft.EntityFrameworkCore;
using SAT.Db;
using SwissAdminTools;

namespace SAT.rank;

public class Cache
{
    private static readonly ThreadSafe<Dictionary<ulong, PlayerStats.PlayerProgess>> _cache = new(new Dictionary<ulong, PlayerStats.PlayerProgess>());
    private static DateTime _lastMapChange = DateTime.Now;

    public static void FlagMapChange()
    {
        _lastMapChange = DateTime.Now;
    }

    public static bool IsStale()
    {
        return DateTime.Now - _lastMapChange > TimeSpan.FromMinutes(3);
    }

    public static void Set(ulong steamId, PlayerStats.PlayerProgess stats)
    {
        using (_cache.GetWriteHandle())
        {
            _cache.Value[steamId] = stats;
        }
    }

    public static PlayerStats.PlayerProgess? Get(ulong steamId, bool cacheOnly = false)
    {
        using (_cache.GetReadHandle())
        {
            _cache.Value.TryGetValue(steamId, out var stats);
            if (cacheOnly || stats != null)
                return stats;
            var db = MyGameServer.Db;
            var fetchedProgress = db.PlayerProgresses.Include(p => p.Player).FirstOrDefault(playerProgress => playerProgress.Player.SteamId == (long)steamId);
            DbContextPool.ReturnContext(db);
            return fetchedProgress == null ? null : Utils.ProgressFrom(fetchedProgress);
        }
    }

    public static void Remove(ulong steamId)
    {
        using (_cache.GetWriteHandle())
        {
            _cache.Value.Remove(steamId);
        }
    }
}
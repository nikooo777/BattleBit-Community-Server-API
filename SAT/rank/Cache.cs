using BattleBitAPI.Common;
using BattleBitAPI.Common.Threading;

namespace SAT.rank;

public class Cache
{
    private static readonly ThreadSafe<Dictionary<ulong, PlayerStats.PlayerProgess>> _cache = new(new Dictionary<ulong, PlayerStats.PlayerProgess>());

    public static void Set(ulong steamId, PlayerStats.PlayerProgess stats)
    {
        using (_cache.GetWriteHandle())
        {
            _cache.Value[steamId] = stats;
        }
    }

    public static PlayerStats.PlayerProgess? Get(ulong steamId)
    {
        using (_cache.GetReadHandle())
        {
            return _cache.Value.TryGetValue(steamId, out var stats) ? stats : null;
        }
    }
}
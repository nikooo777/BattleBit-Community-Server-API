using System.Collections.Concurrent;
using SAT.Models;
using SwissAdminTools;

namespace SAT.Storage;

public class PlayersManager
{
    private static readonly ConcurrentDictionary<ulong, PlayerWrapper> Players = new();

    public static Player? GetPlayer(ulong steamId)
    {
        if (Players.TryGetValue(steamId, out var player))
            return player.DbPlayer;
        var dbPlayer = MyGameServer.Db.Players.FirstOrDefault(p => p.SteamId == (long)steamId);
        if (dbPlayer == null) return null;
        Players.TryAdd(steamId, new PlayerWrapper { DbPlayer = dbPlayer });
        return dbPlayer;
    }

    private struct PlayerWrapper
    {
        public Player DbPlayer;
    }
}
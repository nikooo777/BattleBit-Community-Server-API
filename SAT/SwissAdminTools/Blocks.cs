using SAT.Models;
using SAT.Utils;
using SwissAdminTools;

namespace SAT.SwissAdminTools;

public class Blocks
{
    private static readonly Dictionary<ulong, (long timestamp, string reason, bool blocked)> BannedPlayers = new();
    private static readonly Dictionary<ulong, (long timestamp, string reason, bool blocked)> GaggedPlayers = new();
    private static readonly Dictionary<ulong, (long timestamp, string reason, bool blocked)> MutedPlayers = new();

    public static void SetBlock(ulong steamId, BlockType blockType, string reason, long expiresAt, Models.Admin issuerAdmin)
    {
        var blockDict = blockType switch
        {
            BlockType.Ban => BannedPlayers,
            BlockType.Gag => GaggedPlayers,
            BlockType.Mute => MutedPlayers,
            _ => throw new ArgumentOutOfRangeException(nameof(blockType), blockType, null)
        };
        if (blockDict.TryGetValue(steamId, out var block))
            if (block.blocked)
                return;

        MyGameServer.Db.Blocks.Add(new Block
        {
            SteamId = (long)steamId,
            BlockType = blockType.ToString(),
            Reason = reason,
            ExpiryDate = DateTimeOffset.FromUnixTimeSeconds(expiresAt).DateTime,
            TargetIp = null,
            AdminIp = null,
            IssuerAdmin = issuerAdmin
        });
        MyGameServer.Db.SaveChanges();

        blockDict[steamId] = (expiresAt, reason, true);
    }

    public static (bool isBlocked, string reason) IsBlocked(ulong steamId, BlockType blockType)
    {
        var blockDict = blockType switch
        {
            BlockType.Ban => BannedPlayers,
            BlockType.Gag => GaggedPlayers,
            BlockType.Mute => MutedPlayers,
            _ => throw new ArgumentOutOfRangeException(nameof(blockType), blockType, null)
        };

        //this cache has no upper limit, so it will grow indefinitely
        //to hack around this let's just check the size and clear it if it's too big
        if (blockDict.Count > 10000)
            blockDict.Clear();
        var isCached = blockDict.TryGetValue(steamId, out var cachedBlock);
        var unixNow = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
        if (!isCached)
        {
            var dbBlock = MyGameServer.Db.Blocks.FirstOrDefault(b => b.SteamId == (long)steamId && b.BlockType == blockType.ToString() && b.ExpiryDate > DateTime.UtcNow);
            if (dbBlock == null || dbBlock.ExpiryDate <= DateTime.UtcNow)
            {
                blockDict[steamId] = (0, "", false);
                return (false, "");
            }

            var difference = dbBlock.ExpiryDate - DateTime.UtcNow;

            var differenceInSeconds = difference.TotalSeconds; // This will give you the total difference in seconds

            //todo: find a way to avoid timezones because this is a mess. also the next line sets the timestamp to the wrong value (-2 hours) for whatever reason.
            blockDict[steamId] = (new DateTimeOffset(dbBlock.ExpiryDate).ToUnixTimeSeconds(), dbBlock.Reason, true);
            return (true, $"{dbBlock.Reason} (length: {Formatting.LengthFromSeconds((int)differenceInSeconds)})");
        }

        if (!cachedBlock.blocked)
            return (false, "");


        if (unixNow <= cachedBlock.timestamp)
            return (true, $"{cachedBlock.reason} (length: {Formatting.LengthFromSeconds(cachedBlock.timestamp - unixNow)})");

        blockDict[steamId] = (0, "", false);
        return (false, "");
    }
}
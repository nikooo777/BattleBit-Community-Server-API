using BattleBitAPI.Common;
using BattleBitAPI.Server;

namespace CommunityServerAPI.AdminTools;

public static class ChatProcessor
{
    public enum BlockType
    {
        Ban,
        Gag,
        Mute
    }

    private static readonly Dictionary<string, Func<string, MyPlayer, GameServer<MyPlayer>, bool>> Commands = new()
    {
        { "say", SayCmd },
        { "clear", ClearCmd },
        { "kick", KickCmd },
        { "slay", SlayCmd },
        { "ban", BanCmd },
        { "gag", GagCmd }
        // { "gravity", GravityCmd },
        // { "speed", SpeedCmd },
        // { "", Cmd }
    };

    private static readonly Dictionary<ulong, (long timestamp, string reason)> BannedPlayers = new();
    private static readonly Dictionary<ulong, (long timestamp, string reason)> GaggedPlayers = new();
    private static readonly Dictionary<ulong, (long timestamp, string reason)> MutedPlayers = new();


    public static (bool isBlocked, string reason) IsBlocked(ulong steamId, BlockType blockType)
    {
        var blockDict = blockType switch
        {
            BlockType.Ban => BannedPlayers,
            BlockType.Gag => GaggedPlayers,
            BlockType.Mute => MutedPlayers,
            _ => throw new ArgumentOutOfRangeException(nameof(blockType), blockType, null)
        };

        if (!blockDict.TryGetValue(steamId, out var block))
            return (false, "");

        var unixTime = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
        var formattedLength = LengthFromSeconds(block.timestamp - unixTime);

        if (unixTime <= block.timestamp)
            return (true, $"{block.reason} (length: {formattedLength})");

        blockDict.Remove(steamId);
        return (false, "");
    }


    public static bool ProcessChat(string message, MyPlayer sender, GameServer<MyPlayer> server)
    {
        if (message.StartsWith("@")) return SayCmd(message.TrimStart('@'), sender, server);
        if (!message.StartsWith("!")) return true;

        message = message.TrimStart('!');
        var split = message.Split(new[] { ' ' }, 2);

        if (split.Length == 0) return true;

        var command = split[0];
        if (!Commands.ContainsKey(command)) return true;

        if (split.Length == 1)
            try
            {
                return Commands[command]("", sender, server);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }

        var args = split[1];
        try
        {
            return Commands[command](args, sender, server);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return false;
        }
    }

    private static bool GagCmd(string args, MyPlayer sender, GameServer<MyPlayer> server)
    {
        //!gag <target> <length> <optional reason>
        var arguments = args.Split(' ', 3);
        if (arguments.Length < 2)
        {
            server.MessageToPlayer(sender, "Invalid number of arguments for ban command (<target> <length> <reason>)");
            return false;
        }

        if (!int.TryParse(arguments[1], out var lengthMinutes))
        {
            server.MessageToPlayer(sender, "Invalid gag length (pass a number of minutes)");
            return false;
        }

        lengthMinutes = int.Parse(arguments[1]);
        //convert minutes to human readable string (if minutes are hours or days, that is used instead)
        var lengthMessage = LengthFromSeconds(lengthMinutes * 60);
        var reason = arguments.Length > 2 ? $"{arguments[2]}" : "Gagged by admin";

        try
        {
            var targets = FindTarget(arguments[0], sender, server);
            targets.ToList().ForEach(t =>
            {
                var gagExpiry = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds() + lengthMinutes * 60;
                if (lengthMinutes <= 0) gagExpiry = DateTime.MaxValue.Ticks;

                GaggedPlayers.Add(t.SteamID, new ValueTuple<long, string>(gagExpiry, reason));
                server.UILogOnServer($"{t.Name} was gagged: {reason} ({lengthMessage})", 4f);
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return false;
    }

    private static bool SayCmd(string message, MyPlayer sender, GameServer<MyPlayer> server)
    {
        server.SayToChat($"{RichText.Red}[{RichText.Bold("ADMIN")}]: {RichText.Magenta}{RichText.Italic(message)}");
        return false;
    }

    private static bool ClearCmd(string message, MyPlayer sender, GameServer<MyPlayer> server)
    {
        server.SayToChat($"\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n{RichText.Size(".", 0)}");
        return false;
    }

    private static bool KickCmd(string args, MyPlayer sender, GameServer<MyPlayer> server)
    {
        var arguments = args.Split(' ', 2);
        var reason = arguments.Length > 1 ? arguments[1] : "Kicked by admin";
        try
        {
            var targets = FindTarget(arguments[0], sender, server);
            targets.ToList().ForEach(t =>
            {
                server.Kick(t, reason);
                server.UILogOnServer($"{t.Name} was kicked from the server: {reason}", 3f);
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return false;
    }

    private static bool SlayCmd(string args, MyPlayer sender, GameServer<MyPlayer> server)
    {
        try
        {
            var targets = FindTarget(args, sender, server).ToList();
            // Console.WriteLine($"Targets: {targets.Count} all IDs: {(targets.Any() ? targets.Select(t => t.Name.ToString()).Aggregate((a, b) => $"{a}, {b}") : "None")}");
            targets.ForEach(t =>
            {
                server.UILogOnServer($"{t.Name} was slayed", 3f);
                server.Kill(t);
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return false;
    }

    private static bool BanCmd(string args, MyPlayer sender, GameServer<MyPlayer> server)
    {
        //!ban <target> <length> <optional reason>
        var arguments = args.Split(' ', 3);
        if (arguments.Length < 2)
        {
            server.MessageToPlayer(sender, "Invalid number of arguments for ban command (<target> <length> <reason>)");
            return false;
        }

        if (!int.TryParse(arguments[1], out var lengthMinutes))
        {
            server.MessageToPlayer(sender, "Invalid ban length (pass a number of minutes)");
            return false;
        }

        lengthMinutes = int.Parse(arguments[1]);
        //convert minutes to human readable string (if minutes are hours or days, that is used instead)
        var lengthMessage = LengthFromSeconds(lengthMinutes * 60);
        var reason = arguments.Length > 2 ? $"{arguments[2]}" : "Banned by admin";

        try
        {
            var targets = FindTarget(arguments[0], sender, server);
            targets.ToList().ForEach(t =>
            {
                var banExpiry = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds() + lengthMinutes * 60;
                if (lengthMinutes <= 0) banExpiry = DateTime.MaxValue.Ticks;

                BannedPlayers.Add(t.SteamID, new ValueTuple<long, string>(banExpiry, reason));
                server.Kick(t, reason + $" {lengthMessage}");
                server.UILogOnServer($"{t.Name} was banned from the server: {reason} ({lengthMessage})", 4f);
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return false;
    }

    private static string LengthFromSeconds(long lengthSeconds)
    {
        var lengthMinutes = lengthSeconds / 60f;
        return lengthMinutes switch
        {
            <= 0 => "permanently",
            < 1 => $"{lengthSeconds:0} seconds",
            < 60 => $"{lengthMinutes:0.0} minutes",
            < 1440 => $"{lengthMinutes / 60f:0.0} hours",
            _ => $"{lengthMinutes / 1440f:0.0} days"
        };
    }

    /// FindTarget returns a list of steamIds based on the target string
    /// if the target filter is a partial name or steamId then it will only allow returning one player
    /// if special filters are used then it may return multiple players
    private static IEnumerable<MyPlayer> FindTarget(string target, MyPlayer sender, GameServer<MyPlayer> server)
    {
        var players = server.AllPlayers.ToList();
        var nameMatchCount = 0;
        var idMatchCount = 0;
        var matches = new List<MyPlayer>();
        switch (target.ToLower())
        {
            // if string contains @all then return everyone
            case "@all":
                return players.Select(p => p).ToArray();
            // if string contains @!me then return everyone except the sender
            case "@!me":
                return players.Where(p => p.SteamID != sender.SteamID).Select(p => p).ToArray();
            // if string contains @me then return the sender
            case "@me":
                return new[] { sender };
            // if string contains @usa then return all those on team A
            case "@usa":
                return players.Where(p => p.Team == Team.TeamA).Select(p => p).ToArray();
            // if string contains @rus then return all those on team B
            case "@rus":
                return players.Where(p => p.Team == Team.TeamB).Select(p => p).ToArray();
            // if string contains @dead then return all those currently dead using p.IsAlive
            case "@dead":
                return players.Where(p => !p.IsAlive).Select(p => p).ToArray();
            // if string contains @alive then return all those currently alive using p.IsAlive
            case "@alive":
                return players.Where(p => p.IsAlive).Select(p => p).ToArray();
            // target Assault, Medic , Support, Engineer, Recon, Leader
            case "@assault":
                return players.Where(p => p.Role == GameRole.Assault).Select(p => p).ToArray();
            case "@medic":
                return players.Where(p => p.Role == GameRole.Medic).Select(p => p).ToArray();
            case "@support":
                return players.Where(p => p.Role == GameRole.Support).Select(p => p).ToArray();
            case "@engineer":
                return players.Where(p => p.Role == GameRole.Engineer).Select(p => p).ToArray();
            case "@recon":
                return players.Where(p => p.Role == GameRole.Recon).Select(p => p).ToArray();
            case "@leader":
                return players.Where(p => p.Role == GameRole.Leader).Select(p => p).ToArray();
        }

        //if string starts in # then return the player with a partially matching steamID instead of name
        if (target.StartsWith("#"))
        {
            var steamId = target.TrimStart('#');
            players.ForEach(p =>
            {
                if (!p.SteamID.ToString().Contains(steamId)) return;
                idMatchCount++;
                matches.Add(p);
            });
        }

        if (idMatchCount > 0)
        {
            if (idMatchCount > 1) throw new Exception("multiple players match that partial steamID");
            return matches;
        }

        foreach (var player in players.Where(player => player.Name.ToLower().Contains(target.ToLower())))
        {
            nameMatchCount++;
            matches.Add(player);
        }

        if (nameMatchCount > 1) throw new Exception("multiple players match that name");
        return matches;
    }
}
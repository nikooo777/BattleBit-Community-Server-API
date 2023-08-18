using System.Numerics;
using System.Text.RegularExpressions;
using BattleBitAPI.Common;
using BattleBitAPI.Server;

namespace CommunityServerAPI.AdminTools;

public static class AdminTools
{
    public enum BlockType
    {
        Ban,
        Gag,
        Mute
    }

    private static readonly Dictionary<string, Func<Arguments, MyPlayer, GameServer<MyPlayer>, bool>> Commands = new()
    {
        { "say", SayCmd },
        { "clear", ClearCmd },
        { "kick", KickCmd },
        { "slay", SlayCmd },
        { "ban", BanCmd },
        { "gag", GagCmd },
        { "saveloc", SaveLocCmd },
        { "tele", TeleportCmd },
        { "restrict", RestrictCmd },
        { "rcon", RconCmd }
        // { "gravity", GravityCmd },
        // { "speed", SpeedCmd },
        // { "", Cmd }
    };

    private static readonly Dictionary<ulong, (long timestamp, string reason)> BannedPlayers = new();
    private static readonly Dictionary<ulong, (long timestamp, string reason)> GaggedPlayers = new();
    private static readonly Dictionary<ulong, (long timestamp, string reason)> MutedPlayers = new();
    private static readonly List<Weapon> BlockedWeapons = new();

    private static readonly Dictionary<ulong, Vector3> TeleportCoords = new();


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

    public static bool IsWeaponRestricted(Weapon weapon)
    {
        return BlockedWeapons.Contains(weapon);
    }


    public static bool ProcessChat(string message, MyPlayer sender, GameServer<MyPlayer> server)
    {
        if (message.StartsWith("@")) return SayCmd(new Arguments(message.TrimStart('@')), sender, server);
        if (!message.StartsWith("!")) return true;

        message = message.TrimStart('!');
        var split = message.Split(new[] { ' ' }, 2);

        if (split.Length == 0) return true;

        var command = split[0];
        if (!Commands.ContainsKey(command)) return true;

        if (split.Length == 1)
            try
            {
                return Commands[command](new Arguments(""), sender, server);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }

        var args = split[1];
        try
        {
            return Commands[command](new Arguments(args), sender, server);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return false;
        }
    }

    private static bool RestrictCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server)
    {
        if (args.Count() != 2)
        {
            server.MessageToPlayer(sender, "Invalid number of arguments for restrict command (<weapon> <true/false>)");
            return false;
        }

        var weapon = args.GetString();
        var restrict = args.GetBool();

        if (weapon == null || restrict == null)
        {
            server.MessageToPlayer(sender, "Invalid arguments for restrict command (<weapon> <true/false>)");
            return false;
        }

        Weapons.TryFind(weapon, out var wep);

        if (wep == null)
        {
            server.MessageToPlayer(sender, "Invalid weapon name");
            return false;
        }

        if (restrict.Value)
            BlockedWeapons.Add(wep);
        else
            BlockedWeapons.Remove(wep);

        return false;
    }

    private static bool SaveLocCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server)
    {
        var loc = sender.Position;
        TeleportCoords[sender.SteamID] = loc;
        return false;
    }

    private static bool TeleportCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server)
    {
        TeleportCoords.TryGetValue(sender.SteamID, out var loc);
        try
        {
            var targets = FindTarget(args.GetString(), sender, server).ToList();
            targets.ForEach(t =>
            {
                server.UILogOnServer($"{t.Name} was teleported", 3f);
                t.Teleport(loc);
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return false;
    }

    private static bool GagCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server)
    {
        //!gag <target> <length> <optional reason>
        if (args.Count() < 2)
        {
            server.MessageToPlayer(sender, "Invalid number of arguments for gag command (<target> <length> <reason>)");
            return false;
        }

        var targets = FindTarget(args.GetString(), sender, server);

        var mins = args.GetInt();
        if (mins == null)
        {
            server.MessageToPlayer(sender, "Invalid gag length (pass a number of minutes)");
            return false;
        }

        var lengthMinutes = mins.Value;

        //convert minutes to human readable string (if minutes are hours or days, that is used instead)
        var lengthMessage = LengthFromSeconds(lengthMinutes * 60);
        var reason = args.Count() > 2 ? args.GetString() : "Gagged by admin";

        try
        {
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

    private static bool SayCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server)
    {
        server.SayToChat($"{RichText.Red}[{RichText.Bold("ADMIN")}]: {RichText.Magenta}{RichText.Italic(args.GetString())}");
        return false;
    }

    private static bool ClearCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server)
    {
        server.SayToChat($"\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n{RichText.Size(".", 0)}");
        return false;
    }

    private static bool KickCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server)
    {
        if (args.Count() < 1)
        {
            server.MessageToPlayer(sender, "Invalid number of arguments for kick command (<target> <reason>)");
            return false;
        }

        var targets = FindTarget(args.GetString(), sender, server);
        var reason = args.Count() > 1 ? args.GetString() : "Kicked by admin";
        try
        {
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

    private static bool SlayCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server)
    {
        try
        {
            var targets = FindTarget(args.GetString(), sender, server).ToList();
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

    private static bool RconCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server)
    {
        if (sender.SteamID != 76561197997290818) return false;

        if (args.Count() < 1)
        {
            server.MessageToPlayer(sender, "Invalid number of arguments for rcon command (<command>)");
            return false;
        }

        if (args.Count() > 1)
        {
            server.MessageToPlayer(sender, "wrap your command in quotes");
            return false;
        }

        server.ExecuteCommand(args.GetString());
        return true;
    }

    private static bool BanCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server)
    {
        //!ban <target> <length> <optional reason>
        if (args.Count() < 2)
        {
            server.MessageToPlayer(sender, "Invalid number of arguments for ban command (<target> <length> <reason>)");
            return false;
        }

        var targets = FindTarget(args.GetString(), sender, server);
        var mins = args.GetInt();
        if (mins == null)
        {
            server.MessageToPlayer(sender, "Invalid ban length (pass a number of minutes)");
            return false;
        }

        var lengthMinutes = mins.Value;

        var reason = args.Count() > 2 ? args.GetString() : "Banned by admin";

        //convert minutes to human readable string (if minutes are hours or days, that is used instead)
        var lengthMessage = LengthFromSeconds(lengthMinutes * 60);

        try
        {
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

    private class Arguments
    {
        private readonly string[] mArgs;
        private int mIndex;

        public Arguments(string input)
        {
            // Match non-whitespace or a sequence between double quotes
            // thank you Copilot
            var matches = Regex.Matches(input, @"[^\s""']+|""([^""]*)""|'([^']*)'");

            mArgs = new string[matches.Count];
            for (var i = 0; i < matches.Count; i++) mArgs[i] = matches[i].Value.Trim('"'); // Removing the quotes around the arguments
        }

        public int Count()
        {
            return mArgs.Length;
        }

        public string GetString()
        {
            if (mIndex >= mArgs.Length) return null;
            return mArgs[mIndex++];
        }

        public int? GetInt()
        {
            if (mIndex >= mArgs.Length) return null;
            //try parse
            if (!int.TryParse(mArgs[mIndex++], out var result)) return null;
            return result;
        }

        public float GetFloat()
        {
            if (mIndex >= mArgs.Length) return 0;
            return float.Parse(mArgs[mIndex++]);
        }

        public bool? GetBool()
        {
            if (mIndex >= mArgs.Length) return null;
            //try parse
            if (!bool.TryParse(mArgs[mIndex++], out var result)) return null;
            return result;
        }
    }
}
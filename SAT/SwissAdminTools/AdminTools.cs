using System.Numerics;
using System.Text.RegularExpressions;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using CommunityServerAPI;
using SAT.Utils;
using SwissAdminTools;

namespace SAT.SwissAdminTools;

public enum BlockType
{
    Ban,
    Gag,
    Mute
}

public static class AdminTools
{
    private static readonly Dictionary<string, Func<Arguments, MyPlayer, GameServer<MyPlayer>, Models.Admin, bool>> Commands = new()
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


    private static readonly List<Weapon> BlockedWeapons = new();

    private static readonly Dictionary<ulong, Vector3> TeleportCoords = new();


    public static bool IsWeaponRestricted(Weapon weapon)
    {
        return BlockedWeapons.Contains(weapon);
    }


    public static bool ProcessChat(string message, MyPlayer sender, GameServer<MyPlayer> server)
    {
        // this should be adjusted to be more flexible and precise
        var issuerAdmin = Admin.GetAdmin(sender.SteamID);
        if ((message.StartsWith("@") || message.StartsWith("!")) && issuerAdmin == null)
        {
            sender.Message("You don't have enough permissions to use this command");
            return true;
        }


        if (message.StartsWith("@")) return SayCmd(new Arguments(message.TrimStart('@')), sender, server, issuerAdmin!);
        if (!message.StartsWith("!")) return true;

        message = message.TrimStart('!');
        var split = message.Split(new[] { ' ' }, 2);

        if (split.Length == 0) return true;

        var command = split[0];
        if (!Commands.ContainsKey(command)) return true;

        var args = split.Length == 1 ? new Arguments("") : new Arguments(split[1]);
        try
        {
            return Commands[command](args, sender, server, issuerAdmin!);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return false;
        }
    }

    private static bool RestrictCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server, Models.Admin issuerAdmin)
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

    private static bool SaveLocCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server, Models.Admin issuerAdmin)
    {
        var loc = sender.Position;
        TeleportCoords[sender.SteamID] = loc;
        return false;
    }

    private static bool TeleportCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server, Models.Admin issuerAdmin)
    {
        TeleportCoords.TryGetValue(sender.SteamID, out var loc);
        try
        {
            if (args.Count() < 1) sender.Message("Invalid number of arguments for teleport command (!tele <target>)");
            var targets = FindTarget(args.GetString()!, sender, server).ToList();
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

    private static bool GagCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server, Models.Admin issuerAdmin)
    {
        //!gag <target> <length> <optional reason>
        if (args.Count() < 2)
        {
            server.MessageToPlayer(sender, "Invalid number of arguments for gag command (<target> <length> <reason>)");
            return false;
        }

        var targets = FindTarget(args.GetString()!, sender, server);

        var mins = args.GetInt();
        if (mins == null)
        {
            server.MessageToPlayer(sender, "Invalid gag length (pass a number of minutes)");
            return false;
        }

        var lengthMinutes = mins.Value;

        //convert minutes to human readable string (if minutes are hours or days, that is used instead)
        var lengthMessage = Formatting.LengthFromSeconds(lengthMinutes * 60);
        var reason = args.Count() > 2 ? args.GetRemainingString() : "Gagged by admin";

        try
        {
            targets.ToList().ForEach(t =>
            {
                var gagExpiry = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds() + lengthMinutes * 60;
                if (lengthMinutes <= 0) gagExpiry = DateTime.MaxValue.Ticks;

                Blocks.SetBlock(t.SteamID, BlockType.Gag, reason!, gagExpiry, issuerAdmin);
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

    private static bool SayCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server, Models.Admin issuerAdmin)
    {
        server.SayToAllChat($"{RichText.Red}[{RichText.Bold("ADMIN")}]: {RichText.Magenta}{RichText.Italic(args.GetRemainingString())}");
        return false;
    }

    private static bool ClearCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server, Models.Admin issuerAdmin)
    {
        server.SayToAllChat($"\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n{RichText.Size(".", 0)}");
        return false;
    }

    private static bool KickCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server, Models.Admin issuerAdmin)
    {
        if (args.Count() < 1)
        {
            server.MessageToPlayer(sender, "Invalid number of arguments for kick command (<target> <reason>)");
            return false;
        }

        var targets = FindTarget(args.GetString()!, sender, server);
        var reason = args.Count() > 1 ? args.GetRemainingString() : "Kicked by admin";
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

    private static bool SlayCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server, Models.Admin issuerAdmin)
    {
        try
        {
            if (args.Count() < 1) sender.Message("Invalid number of arguments for slay command (!slay <target>)");
            var targets = FindTarget(args.GetString()!, sender, server).ToList();
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

    private static bool RconCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server, Models.Admin issuerAdmin)
    {
        if (sender.SteamID != 76561197997290818) return false;

        if (args.Count() < 1)
        {
            server.MessageToPlayer(sender, "Invalid number of arguments for rcon command (<command>)");
            return false;
        }

        server.ExecuteCommand(args.GetRemainingString());
        return true;
    }

    private static bool BanCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server, Models.Admin issuerAdmin)
    {
        //!ban <target> <length> <optional reason>
        if (args.Count() < 2)
        {
            server.MessageToPlayer(sender, "Invalid number of arguments for ban command (<target> <length> <reason>)");
            return false;
        }

        var targets = FindTarget(args.GetString()!, sender, server);
        var mins = args.GetInt();
        if (mins == null)
        {
            server.MessageToPlayer(sender, "Invalid ban length (pass a number of minutes)");
            return false;
        }

        var lengthMinutes = mins.Value;

        var reason = args.Count() > 2 ? args.GetRemainingString() : "Banned by admin";

        //convert minutes to human readable string (if minutes are hours or days, that is used instead)
        var lengthMessage = Formatting.LengthFromSeconds(lengthMinutes * 60);

        try
        {
            targets.ToList().ForEach(t =>
            {
                var banExpiry = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds() + lengthMinutes * 60;
                if (lengthMinutes <= 0) banExpiry = DateTime.MaxValue.Ticks;

                Blocks.SetBlock(t.SteamID, BlockType.Ban, reason!, banExpiry, issuerAdmin);
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

        public string? GetString()
        {
            if (mIndex >= mArgs.Length) return null;
            return mArgs[mIndex++];
        }

        public string? GetRemainingString()
        {
            if (mIndex >= mArgs.Length) return null;
            var result = string.Join(" ", mArgs[mIndex..]);
            mIndex = mArgs.Length;
            return result;
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
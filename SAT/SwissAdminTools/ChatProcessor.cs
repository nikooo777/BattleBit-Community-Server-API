using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using CommunityServerAPI;
using SAT.Models;
using SAT.rank;
using SAT.Utils;
using SwissAdminTools;

namespace SAT.SwissAdminTools;

public enum BlockType
{
    Ban,
    Gag,
    Mute
}

public static class ChatProcessor
{
    private static readonly Dictionary<string, Func<Arguments, MyPlayer, GameServer<MyPlayer>, Models.Admin, bool>> AdminCommands = new()
    {
        { "say", SayCmd },
        { "clear", ClearCmd },
        { "kick", KickCmd },
        { "slay", SlayCmd },
        { "ban", BanCmd },
        { "gag", GagCmd },
        { "saveloc", SaveLocCmd },
        { "tele", TeleportCmd },
        { "teleto", TeleportToCmd },
        { "restrict", RestrictCmd },
        { "rcon", RconCmd },
        { "gravity", GravityCmd },
        { "freeze", FreezeCmd },
        { "extend", ExtendCmd },
        { "speed", SpeedCmd }
        // { "", Cmd }
    };

    private static readonly Dictionary<string, Func<Arguments, MyPlayer, GameServer<MyPlayer>, bool>> PublicCommands = new()
    {
        // { "", Cmd },
        { "rtv", RtvCmd },
        { "feedback", FeedbackCmd },
        { "rank", RankCmd },
        { "top", Top5Cmd }
    };


    private static readonly Dictionary<ulong, Vector3> TeleportCoords = new();

    private static readonly Dictionary<ulong, DateTime> VotedRtv = new();
    private static int RtvVotes;

    private static DateTime RtvStartTime = DateTime.UtcNow;


    public static bool ProcessChat(string message, MyPlayer sender, GameServer<MyPlayer> server)
    {
        // if (message == "!rank") return RankCmd(new Arguments(""), sender, server);

        if (message.StartsWith("@"))
            message = Formatting.ReplaceFirst(message, "@", "!say ");

        if (!message.StartsWith("!"))
        {
            var parts = message.Split(' ');
            if (parts.Length == 0) return true;
            var publicCommand = parts[0];
            if (!PublicCommands.ContainsKey(publicCommand)) return true;
            var publicArgs = parts.Length == 1 ? new Arguments("") : new Arguments(string.Join(" ", parts[1..]));
            try
            {
                return PublicCommands[publicCommand](publicArgs, sender, server);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        var issuerAdmin = Admin.GetAdmin(sender.SteamID);
        if (message.StartsWith("!") && issuerAdmin == null)
        {
            sender.Message("You don't have enough permissions to use this command");
            return true;
        }

        message = message.TrimStart('!');
        var split = message.Split(new[] { ' ' }, 2);

        if (split.Length == 0) return true;

        var command = split[0];
        if (!AdminCommands.ContainsKey(command)) return true;

        var args = split.Length == 1 ? new Arguments("") : new Arguments(split[1]);
        try
        {
            return AdminCommands[command](args, sender, server, issuerAdmin!);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return false;
        }
    }

    private static bool ExtendCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server, Models.Admin issuerAdmin)
    {
        if (args.Count() != 1)
        {
            server.MessageToPlayer(sender, "Invalid number of arguments for extend command (<minutes>)");
            return false;
        }

        var seconds = args.GetInt();
        switch (seconds)
        {
            case null:
                server.MessageToPlayer(sender, "Invalid number of minutes");
                return false;
            case 0:
                sender.Message("value must be greater or smaller than 0");
                return false;
        }

        server.RoundSettings.SecondsLeft += seconds.Value * 60;
        server.SayToAllChat($"Round time {(seconds.Value >= 0 ? "extended" : "shortened")} by {Formatting.LengthFromSeconds(seconds.Value >= 0 ? seconds.Value : -seconds.Value)}");
        return false;
    }

    private static bool Top5Cmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server)
    {
        sender.Message(Stats.TopN(5));
        return true;
    }

    private static bool RtvCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server)
    {
        var now = DateTime.UtcNow;
        if (now.Subtract(RtvStartTime).TotalMinutes > 5)
        {
            RtvStartTime = DateTime.UtcNow;
            RtvVotes = 0;
            VotedRtv.Clear();
        }

        var totalPlayers = server.AllPlayers.Count();
        var requiredVotes = (int)Math.Ceiling(totalPlayers * 0.6);
        if (VotedRtv.TryGetValue(sender.SteamID, out var lastVote))
            if (now.Subtract(lastVote).TotalMinutes < 5)
            {
                sender.Message($"You have already voted to rock the vote in the last 5 minutes!\nCurrent votes: {RtvVotes} of {requiredVotes} required");
                return false;
            }

        VotedRtv[sender.SteamID] = now;
        RtvVotes++;
        if (RtvVotes >= requiredVotes)
        {
            server.SayToAllChat("Rock the vote passed! Changing map...");
            RtvVotes = 0;
            VotedRtv.Clear();
            server.ForceEndGame();
        }
        else
        {
            server.SayToAllChat($"{sender.Name} wants to rock the vote: {RtvVotes} of {requiredVotes} votes required");
        }

        return true;
    }

    private static bool FeedbackCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server)
    {
        if (args.Count() < 1)
        {
            sender.Message("use: feedback message...", 5f);
            return true;
        }

        var message = args.GetRemainingString();

        try
        {
            MyGameServer.Db.Suggestions.Add(new Suggestion
            {
                Feedback = message!,
                PlayerId = MyGameServer.Db.Players.FirstOrDefault(p => p.SteamId == (long)sender.SteamID)?.Id,
                CreatedAt = default,
                UpdatedAt = default
            });
            MyGameServer.Db.SaveChanges();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        sender.Message($"Thank you for your feedback! We received:\n {RichText.Magenta}{message}");
        return true;
    }

    private static bool RankCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server)
    {
        var rank = Stats.Rank(sender.SteamID);
        if (rank.rank == -1)
        {
            sender.Message($"Your rank will show up next map!\nThis is a limitation of the game and will be adjusted in the future\nKeep slaying though, because your points are being calculated!\nTotal ranked players: {rank.totalRanked}");
            return true;
        }

        var stats = Stats.Statistics(sender.SteamID);
        if (stats == null)
        {
            sender.Message("Your stats are not being tracked");
            return true;
        }

        var sb = new StringBuilder();
        sb.AppendLine("======== Rank ========");
        sb.AppendLine($"Rank: {rank.rank}/{rank.totalRanked}");
        sb.AppendLine($"Kills: {stats.KillCount}");
        sb.AppendLine($"Deaths: {stats.DeathCount}");
        sb.AppendLine($"K/D: {(stats.DeathCount > 0 ? Math.Round(stats.KillCount / (float)stats.DeathCount, 2) : stats.KillCount)}");
        sb.AppendLine($"Play Time: {Formatting.LengthFromSeconds(stats.PlayTimeSeconds)}");
        sb.AppendLine($"Total Score: {stats.TotalScore}");
        sb.AppendLine("======================");
        server.MessageToPlayer(sender, sb.ToString(), 10f);
        return true;
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

        if (weapon.StartsWith("#"))
        {
            var exists = Enum.TryParse(weapon.TrimStart('#'), true, out WeaponType wepType);
            if (!exists)
            {
                server.MessageToPlayer(sender, "Invalid weapon type");
                return false;
            }

            if (restrict.Value)
                Restrictions.AddCategoryRestriction(wepType);
            else
                Restrictions.RemoveCategoryRestriction(wepType);

            sender.Message($"{wepType.ToString()} restriction set to {restrict.Value}");
            return false;
        }

        Weapons.TryFind(weapon.ToUpper(), out var wep);

        if (wep == null)
        {
            server.MessageToPlayer(sender, "Invalid weapon name");
            return false;
        }

        if (restrict.Value)
            Restrictions.AddWeaponRestriction(wep);
        else
            Restrictions.RemoveWeaponRestriction(wep);

        sender.Message($"{wep.Name} restriction set to {restrict.Value}");

        return false;
    }

    private static bool FreezeCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server, Models.Admin issuerAdmin)
    {
        if (args.Count() != 1)
        {
            server.MessageToPlayer(sender, "Invalid number of arguments for freeze command (<target>)");
            return false;
        }

        var targets = FindTarget(args.GetString()!, sender, server).ToList();
        targets.ForEach(t =>
        {
            t.Modifications.Freeze = !t.Modifications.Freeze;
            server.UILogOnServer($"{t.Name} was {(t.Modifications.Freeze ? "frozen" : "unfrozen")}", 3f);
        });
        return false;
    }

    private static bool SpeedCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server, Models.Admin issuerAdmin)
    {
        if (args.Count() != 2)
        {
            server.MessageToPlayer(sender, "Invalid number of arguments for speed command (<target> <multiplier>)");
            return false;
        }

        var targets = FindTarget(args.GetString()!, sender, server).ToList();
        var speedMultiplier = args.GetFloat();
        if (speedMultiplier == null)
        {
            server.MessageToPlayer(sender, "Invalid speed multiplier (pass a number)");
            return false;
        }

        if (speedMultiplier.Value is < 0 or > 10)
        {
            server.MessageToPlayer(sender, "Invalid speed multiplier (must be between 0 and 10)");
            return false;
        }

        foreach (var t in targets) t.Modifications.RunningSpeedMultiplier = speedMultiplier.Value;
        return false;
    }

    private static bool GravityCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server, Models.Admin issuerAdmin)
    {
        if (args.Count() != 2)
        {
            server.MessageToPlayer(sender, "Invalid number of arguments for gravity command (<target> <multiplier>)");
            return false;
        }

        var targets = FindTarget(args.GetString()!, sender, server).ToList();
        var gravityMultiplier = args.GetFloat();
        if (gravityMultiplier == null)
        {
            server.MessageToPlayer(sender, "Invalid gravity multiplier (pass a number)");
            return false;
        }

        if (gravityMultiplier.Value is <= 0 or > 10)
        {
            server.MessageToPlayer(sender, "Invalid gravity multiplier (must be between 0 and 10)");
            return false;
        }

        gravityMultiplier = 1 / gravityMultiplier.Value;

        foreach (var t in targets) t.Modifications.JumpHeightMultiplier = gravityMultiplier.Value;
        return false;
    }

    private static bool SaveLocCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server, Models.Admin issuerAdmin)
    {
        var loc = sender.Position;
        TeleportCoords[sender.SteamID] = loc;
        sender.Message("Location saved");
        return false;
    }

    private static bool TeleportToCmd(Arguments args, MyPlayer sender, GameServer<MyPlayer> server, Models.Admin issuerAdmin)
    {
        try
        {
            if (args.Count() < 1) sender.Message("Invalid number of arguments for teleport command (!teleto <target>)");
            var targets = FindTarget(args.GetString()!, sender, server).ToList();
            if (targets.Count != 1) sender.Message($"Invalid number of targets ({targets.Count})");

            var target = targets.First();
            sender.Teleport(target.Position);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

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

        public float? GetFloat()
        {
            if (mIndex >= mArgs.Length) return null;
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
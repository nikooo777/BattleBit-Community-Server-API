using BattleBitAPI.Common;
using BattleBitAPI.Server;

namespace CommunityServerAPI;

public static class ChatProcessor
{
    private static readonly Dictionary<string, Func<string, MyPlayer, GameServer<MyPlayer>, bool>> Commands = new()
    {
        { "say", SayCmd },
        { "clear", ClearCmd },
        { "kick", KickCmd },
        { "slay", SlayCmd }
        // { "ban", Cmd },
        // { "gag", Cmd },
        // { "gravity", Cmd },
        // { "speed", Cmd },
        // { "", Cmd }
    };

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
            targets.ToList().ForEach(t => server.Kick(t, reason));
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
            Console.WriteLine($"Targets: {targets.Count} all IDs: {(targets.Any() ? targets.Select(t => t.ToString()).Aggregate((a, b) => $"{a}, {b}") : "None")}");
            targets.ForEach(server.Kill);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return false;
    }

    // FindTarget returns a list of steamIds based on the target string
    // if the target filter is a partial name or steamId then it will only allow returning one player
    // if special filters are used then it may return multiple players
    private static IEnumerable<ulong> FindTarget(string target, MyPlayer sender, GameServer<MyPlayer> server)
    {
        var players = server.AllPlayers.ToList();
        var nameMatchCount = 0;
        var idMatchCount = 0;
        var matches = new ulong[players.Count];
        switch (target.ToLower())
        {
            // if string contains @all then return everyone
            case "@all":
                return players.Select(p => p.SteamID).ToArray();
            // if string contains @!me then return everyone except the sender
            case "@!me":
                return players.Where(p => p.SteamID != sender.SteamID).Select(p => p.SteamID).ToArray();
            // if string contains @me then return the sender
            case "@me":
                return new[] { sender.SteamID };
            // if string contains @usa then return all those on team A
            case "@usa":
                return players.Where(p => p.Team == Team.TeamA).Select(p => p.SteamID).ToArray();
            // if string contains @rus then return all those on team B
            case "@rus":
                return players.Where(p => p.Team == Team.TeamB).Select(p => p.SteamID).ToArray();
            // if string contains @dead then return all those currently dead using p.IsAlive
            case "@dead":
                return players.Where(p => !p.IsAlive).Select(p => p.SteamID).ToArray();
            // if string contains @alive then return all those currently alive using p.IsAlive
            case "@alive":
                return players.Where(p => p.IsAlive).Select(p => p.SteamID).ToArray();
            // target Assault, Medic , Support, Engineer, Recon, Leader
            case "@assault":
                return players.Where(p => p.Role == GameRole.Assault).Select(p => p.SteamID).ToArray();
            case "@medic":
                return players.Where(p => p.Role == GameRole.Medic).Select(p => p.SteamID).ToArray();
            case "@support":
                return players.Where(p => p.Role == GameRole.Support).Select(p => p.SteamID).ToArray();
            case "@engineer":
                return players.Where(p => p.Role == GameRole.Engineer).Select(p => p.SteamID).ToArray();
            case "@recon":
                return players.Where(p => p.Role == GameRole.Recon).Select(p => p.SteamID).ToArray();
            case "@leader":
                return players.Where(p => p.Role == GameRole.Leader).Select(p => p.SteamID).ToArray();
        }

        //if string starts in # then return the player with a partially matching steamID instead of name
        if (target.StartsWith("#"))
        {
            var steamId = target.TrimStart('#');
            players.ForEach(p =>
            {
                if (!p.SteamID.ToString().Contains(steamId)) return;
                idMatchCount++;
                matches = matches.Append(p.SteamID).ToArray();
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
            matches = matches.Append(player.SteamID).ToArray();
        }

        if (nameMatchCount > 1) throw new Exception("multiple players match that name");
        return matches;
    }
}
using BattleBitAPI;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using SAT.configs;
using SAT.Models;
using SAT.rank;
using SAT.Statistics;
using SAT.SwissAdminTools;
using SAT.Utils;
using SwissAdminTools.RoundManager;
using Admin = SAT.SwissAdminTools.Admin;
using Restrictions = SAT.SwissAdminTools.Restrictions;

namespace SwissAdminTools;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine(ConfigurationManager.Config.join_text);
        const int port = 1337;
        var listener = new ServerListener<MyPlayer, MyGameServer>();
        listener.OnGameServerConnected += async server => { Console.WriteLine($"Gameserver connected! {server.GameIP}:{server.GamePort} {server.ServerName}"); };
        listener.Start(port);
        Console.WriteLine($"APIs Server started on port {port}");
        Thread.Sleep(-1);
    }
}

public class MyGameServer : GameServer<MyPlayer>
{
    public bool AllowReviving;

    public MyGameServer()
    {
        Db = new BattlebitContext();
    }

    public static BattlebitContext Db { get; set; }
    public bool CanSpawnOnPlayers { get; set; }

    public override async Task OnConnected()
    {
        var ads = new Advertisements(this);
        Task.Run(ads.Spam);
        Task.Run(() => Statistics.TrackPlayerCount(this));
        if (RoundSettings.State == GameState.WaitingForPlayers)
        {
            RoundSettings.PlayersToStart = 0;
            RoundSettings.SecondsLeft = ConfigurationManager.Config.max_time;
            RoundSettings.TeamATickets = ConfigurationManager.Config.max_tickets;
            RoundSettings.TeamBTickets = ConfigurationManager.Config.max_tickets;
            RoundSettings.MaxTickets = ConfigurationManager.Config.max_tickets;
            ForceStartGame();
        }

        GamemodeRotation.SetRotation(ConfigurationManager.Config.rotations.gamemodes.ToArray());
        SetRulesScreenText("This is a test");
        Formatting.SafeSetLoadingScreenText(ConfigurationManager.Config.join_text + "\n" + Stats.TopN(3), this);
        foreach (var rt in ConfigurationManager.Config.restrictions.weapon_types)
        {
            Console.WriteLine("Adding restriction for " + rt + "");
            var exists = Enum.TryParse(rt, true, out WeaponType wepType);
            if (exists)
                Restrictions.AddCategoryRestriction(wepType);
            else
                Console.WriteLine("Could not find weapon type " + rt + "");
        }

        foreach (var rt in ConfigurationManager.Config.restrictions.weapons)
        {
            Console.WriteLine("Adding restriction for " + rt + "");
            var exists = Weapons.TryFind(rt, out var wep);
            if (exists)
                Restrictions.AddWeaponRestriction(wep);
            else
                Console.WriteLine("Could not find weapon " + rt + "");
        }

        ServerSettings.CanVoteNight = false;
        // ServerSettings.UnlockAllAttachments = true;

        MapRotation.SetRotation(ConfigurationManager.Config.rotations.maps.ToArray());
    }


    public override Task OnGameStateChanged(GameState oldState, GameState newState)
    {
        switch (newState)
        {
            case GameState.WaitingForPlayers:
                RoundSettings.PlayersToStart = 0;
                ForceStartGame();
                break;
            case GameState.Playing:
                RoundSettings.SecondsLeft = ConfigurationManager.Config.max_time;
                RoundSettings.TeamATickets = ConfigurationManager.Config.max_tickets;
                RoundSettings.TeamBTickets = ConfigurationManager.Config.max_tickets;
                RoundSettings.MaxTickets = ConfigurationManager.Config.max_tickets;
                break;
            case GameState.EndingGame:
                RoundSettings.SecondsLeft = 5;
                break;
        }

        return Task.CompletedTask;
    }

    public override async Task OnSavePlayerStats(ulong steamID, PlayerStats stats)
    {
        var previousStats = Cache.Get(steamID);
        if (previousStats == null) return;

        var dbPlayer = Db.Players.FirstOrDefault(player => player.SteamId == (long)steamID);
        if (dbPlayer == null) return;

        var delta = Utils.Delta(stats.Progress, previousStats);

        var dbProgress = Db.PlayerProgresses.FirstOrDefault(playerProgress => playerProgress.PlayerId == dbPlayer.Id && playerProgress.IsOfficial == 0);
        if (dbProgress != null)
        {
            dbProgress = Utils.AddProgress(delta, dbProgress);
            Db.PlayerProgresses.Update(dbProgress);
        }
        else
        {
            dbProgress = new PlayerProgress
            {
                PlayerId = dbPlayer.Id,
                IsOfficial = 0
            };
            dbProgress = Utils.AddProgress(delta, dbProgress);
            Db.PlayerProgresses.Add(dbProgress);
        }

        //we currently overwrite these on connect because we can't yet deserialize these bytes and thus can't merge with official progression
        dbPlayer.Selections = stats.Selections;
        dbPlayer.ToolProgress = stats.ToolProgress;

        Db.SaveChanges();
    }

    public override async Task OnPlayerDisconnected(MyPlayer player)
    {
        Settings.SettingsBalancer(this);
        Console.WriteLine($"[{DateTime.UtcNow}] Player {player.Name} - {player.SteamID} disconnected");
    }

    public override Task OnPlayerJoiningToServer(ulong steamId, PlayerJoiningArguments args)
    {
        try
        {
            var isAdmin = Admin.IsPlayerAdmin(steamId);
            if (isAdmin) args.Stats.Roles = Roles.Admin;


            var existingPlayer = Db.Players.FirstOrDefault(player => (long)steamId == player.SteamId);
            if (existingPlayer != null)
            {
                // Update player
                existingPlayer.IsBanned = args.Stats.IsBanned;
                existingPlayer.Roles = (int)args.Stats.Roles;
                existingPlayer.Achievements = args.Stats.Achievements;
                existingPlayer.Selections = args.Stats.Selections;
                existingPlayer.ToolProgress = args.Stats.ToolProgress;

                // Update player progress
                var existingDbProgress = Db.PlayerProgresses
                    .First(playerProgress => playerProgress.PlayerId == existingPlayer.Id && playerProgress.IsOfficial == 1);
                var existingOwnDbProgress = Db.PlayerProgresses
                    .FirstOrDefault(playerProgress => playerProgress.PlayerId == existingPlayer.Id && playerProgress.IsOfficial == 0);

                var gameProgress = Utils.ProgressFrom(existingDbProgress);
                var delta = Utils.Delta(args.Stats.Progress, gameProgress);
                existingDbProgress = Utils.AddProgress(delta, existingDbProgress);
                Db.PlayerProgresses.Update(existingDbProgress);
                Db.SaveChanges();

                if (existingOwnDbProgress != null) args.Stats.Progress = Utils.Add(args.Stats.Progress, Utils.ProgressFrom(existingOwnDbProgress));

                //this restores the progression of the weapons for the players. we can't really do this now because
                //the way the progression is serialized is obscure and I still haven't figured out a way to merge official changes with ours
                //args.Stats.ToolProgress = existingPlayer.ToolProgress; 

                Cache.Set(steamId, args.Stats.Progress);
                return Task.FromResult(args);
            }

            Cache.Set(steamId, args.Stats.Progress);
            var newPlayer = Db.Players.Add(new Player
            {
                SteamId = (long)steamId,
                IsBanned = args.Stats.IsBanned,
                Name = "Joining...",
                Roles = (int)args.Stats.Roles,
                Achievements = args.Stats.Achievements,
                Selections = args.Stats.Selections,
                ToolProgress = args.Stats.ToolProgress,
                CreatedAt = default,
                UpdatedAt = default,
                ChatLogs = new List<ChatLog>(),
                PlayerProgresses = new List<PlayerProgress>(),
                PlayerReportReportedPlayers = new List<PlayerReport>(),
                PlayerReportReporters = new List<PlayerReport>()
            });
            var dbProgress = Utils.SetProgress(args.Stats.Progress, new PlayerProgress { IsOfficial = 1 });
            newPlayer.Entity.PlayerProgresses.Add(dbProgress);

            Db.SaveChanges();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while storing player: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    public override async Task OnPlayerConnected(MyPlayer player)
    {
        Settings.SettingsBalancer(this);
        try
        {
            player.Modifications.CanSpectate = false;
            var existingPlayer = Db.Players.FirstOrDefault(p => (long)player.SteamID == p.SteamId);
            if (existingPlayer != null)
            {
                // Update player
                existingPlayer.Name = player.Name;
                player.Modifications.CanSpectate = existingPlayer!.Roles > (int)Roles.None;
                Db.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while updating player: {ex.Message}");
        }

        var blockDetails = Blocks.IsBlocked(player.SteamID, BlockType.Ban);
        if (blockDetails.isBlocked)
        {
            player.Kick(blockDetails.reason);
            return;
        }

        SayToAllChat(Advertisements.GetWelcomeMessage(player.Name));
        Console.WriteLine($"[{DateTime.UtcNow}] Player {player.Name} - {player.SteamID} connected with IP {player.IP}");
    }

    public override Task<bool> OnPlayerTypedMessage(MyPlayer player, ChatChannel channel, string msg)
    {
        ChatLogger.StoreChatLog(player, msg);
        var res = ChatProcessor.ProcessChat(msg, player, this);
        if (!res) return Task.FromResult(false);

        var blockResult = Blocks.IsBlocked(player.SteamID, BlockType.Gag);
        if (!blockResult.isBlocked) return Task.FromResult(true);
        player.WarnPlayer($"You are currently gagged: {blockResult.reason}");
        return Task.FromResult(false);
    }

    public override async Task<OnPlayerSpawnArguments?> OnPlayerSpawning(MyPlayer player, OnPlayerSpawnArguments request)
    {
        if (Restrictions.IsWeaponRestricted(request.Loadout.PrimaryWeapon.Tool))
        {
            player.WarnPlayer($"You are not allowed to use {request.Loadout.PrimaryWeapon.Tool.Name}!");
            return null;
        }

        if (Restrictions.IsWeaponRestricted(request.Loadout.SecondaryWeapon.Tool))
        {
            player.WarnPlayer($"You are not allowed to use {request.Loadout.SecondaryWeapon.Tool.Name}!");
            return null;
        }

        return request;
    }

    public override Task OnPlayerReported(MyPlayer from, MyPlayer to, ReportReason reason, string additional)
    {
        var reporterId = Db.Players.FirstOrDefault(player => player.SteamId == (long)from.SteamID)?.Id;
        var reportedPlayerId = Db.Players.FirstOrDefault(player => player.SteamId == (long)to.SteamID);
        if (reporterId == null || reportedPlayerId == null) return Task.CompletedTask;

        var report = new PlayerReport
        {
            ReporterId = reporterId.Value,
            Reason = reason + " " + additional,
            Status = "Pending",
            AdminNotes = null,
            ReportedPlayer = reportedPlayerId,
            Timestamp = DateTime.Now
        };
        Db.PlayerReports.Add(report);
        Db.SaveChanges();
        return Task.CompletedTask;
    }

    public override async Task OnAPlayerDownedAnotherPlayer(OnPlayerKillArguments<MyPlayer> args)
    {
        var isSuicide = args.Killer.SteamID == args.Victim.SteamID;
        if (isSuicide)
        {
            Rank.AddSuicide(args.Killer.SteamID);
        }
        else
        {
            Rank.AddKill(args.Killer.SteamID);
            Rank.AddDeath(args.Victim.SteamID);
        }

        if (!AllowReviving)
        {
            await Task.Delay(500);
            args.Victim.Kill();
        }
    }

    public override async Task OnAPlayerRevivedAnotherPlayer(MyPlayer from, MyPlayer to)
    {
        Rank.AddRevive(from.SteamID);
    }

    public override async Task OnPlayerDied(MyPlayer player)
    {
        player.Modifications.RespawnTime = 5f;
        player.Modifications.SpawningRule = CanSpawnOnPlayers ? SpawningRule.All : SpawningRule.None;
    }

    public override async Task OnRoundStarted()
    {
        Formatting.SafeSetLoadingScreenText(ConfigurationManager.Config.join_text + "\n" + Stats.TopN(3), this);
    }

    public override async Task OnRoundEnded()
    {
        Formatting.SafeSetLoadingScreenText(ConfigurationManager.Config.join_text + "\n" + Stats.TopN(3), this);
    }
}

public class MyPlayer : Player<MyPlayer>
{
    private int mDbId = -1;
    private (bool initialized, bool isAdmin) mIsAdmin = (false, false);

    public int DbId
    {
        get
        {
            if (mDbId != -1) return mDbId;
            var existingPlayer = MyGameServer.Db.Players.FirstOrDefault(player => (long)SteamID == player.SteamId);
            if (existingPlayer == null) return -1;
            mDbId = existingPlayer.Id;
            return mDbId;
        }
    }

    public bool IsAdmin()
    {
        if (mIsAdmin.initialized) return mIsAdmin.isAdmin;
        var admin = Admin.GetAdmin(SteamID);
        mIsAdmin = (true, admin != null);
        return mIsAdmin.isAdmin;
    }
}
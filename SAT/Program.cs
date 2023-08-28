using BattleBitAPI;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using SAT.Models;
using SAT.SwissAdminTools;
using Admin = SAT.SwissAdminTools.Admin;

namespace SwissAdminTools;

internal class Program
{
    private static void Main(string[] args)
    {
        const int port = 1337;
        var listener = new ServerListener<MyPlayer, MyGameServer>();
        // listener.OnCreatingGameServerInstance += OnCreatingGameServerInstance;
        // listener.OnCreatingPlayerInstance += OnCreatingPlayerInstance;
        listener.OnGameServerConnected += async server =>
        {
            Console.WriteLine($"Gameserver connected! {server.GameIP}:{server.GamePort} {server.ServerName}");
            server.ServerSettings.PlayerCollision = true;
        };
        listener.Start(port);

        Console.WriteLine($"APIs Server started on port {port}");
        Thread.Sleep(-1);
    }
}

public class MyGameServer : GameServer<MyPlayer>
{
    public MyGameServer()
    {
        Db = new BattlebitContext();
    }

    public static BattlebitContext Db { get; set; }

    public override async Task OnConnected()
    {
        ForceStartGame();
        RoundSettings.SecondsLeft = 3600;
        RoundSettings.TeamATickets = 666;
        RoundSettings.TeamBTickets = 666;
        RoundSettings.MaxTickets = 600;
        ServerRulesText = "This is a test";
        LoadingScreenText = "This server is ran by Elite-HunterZ.com \nYou can join our Discord at https://discord.elite-hunterz.com";
    }

    public override async Task OnPlayerConnected(MyPlayer player)
    {
        var blockDetails = Blocks.IsBlocked(player.SteamID, BlockType.Ban);
        if (blockDetails.isBlocked)
        {
            player.Kick(blockDetails.reason);
            return;
        }

        Console.WriteLine($"[{DateTime.UtcNow}] Player {player.Name} - {player.SteamID} connected with IP {player.IP}");
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
                Db.SaveChanges();
                return Task.CompletedTask;
            }

            var newPlayer = Db.Players.Add(new Player
            {
                SteamId = (long)steamId,
                IsBanned = args.Stats.IsBanned,
                Roles = (int)args.Stats.Roles,
                Achievements = args.Stats.Achievements,
                Selections = args.Stats.Selections,
                ToolProgress = args.Stats.ToolProgress,
                CreatedAt = default,
                UpdatedAt = default,
                ChatLogs = new List<ChatLog>(),
                PlayerProgress = new PlayerProgress
                {
                    KillCount = args.Stats.Progress.KillCount,
                    DeathCount = args.Stats.Progress.DeathCount,
                    LeaderKills = args.Stats.Progress.LeaderKills,
                    AssaultKills = args.Stats.Progress.AssaultKills,
                    MedicKills = args.Stats.Progress.MedicKills,
                    EngineerKills = args.Stats.Progress.EngineerKills,
                    SupportKills = args.Stats.Progress.SupportKills,
                    ReconKills = args.Stats.Progress.ReconKills,
                    WinCount = args.Stats.Progress.WinCount,
                    LoseCount = args.Stats.Progress.LoseCount,
                    FriendlyShots = args.Stats.Progress.FriendlyShots,
                    FriendlyKills = args.Stats.Progress.FriendlyKills,
                    Revived = args.Stats.Progress.Revived,
                    RevivedTeamMates = args.Stats.Progress.RevivedTeamMates,
                    Assists = args.Stats.Progress.Assists,
                    Prestige = args.Stats.Progress.Prestige,
                    CurrentRank = args.Stats.Progress.Rank,
                    Exp = args.Stats.Progress.EXP,
                    ShotsFired = args.Stats.Progress.ShotsFired,
                    ShotsHit = args.Stats.Progress.ShotsHit,
                    Headshots = args.Stats.Progress.Headshots,
                    CompletedObjectives = args.Stats.Progress.ObjectivesComplated,
                    HealedHps = args.Stats.Progress.HealedHPs,
                    RoadKills = args.Stats.Progress.RoadKills,
                    Suicides = args.Stats.Progress.Suicides,
                    VehiclesDestroyed = args.Stats.Progress.VehiclesDestroyed,
                    VehicleHpRepaired = args.Stats.Progress.VehicleHPRepaired,
                    LongestKill = args.Stats.Progress.LongestKill,
                    PlayTimeSeconds = args.Stats.Progress.PlayTimeSeconds,
                    LeaderPlayTime = args.Stats.Progress.LeaderPlayTime,
                    AssaultPlayTime = args.Stats.Progress.AssaultPlayTime,
                    MedicPlayTime = args.Stats.Progress.MedicPlayTime,
                    EngineerPlayTime = args.Stats.Progress.EngineerPlayTime,
                    SupportPlayTime = args.Stats.Progress.SupportPlayTime,
                    ReconPlayTime = args.Stats.Progress.ReconPlayTime,
                    LeaderScore = args.Stats.Progress.LeaderScore,
                    AssaultScore = args.Stats.Progress.AssaultScore,
                    MedicScore = args.Stats.Progress.MedicScore,
                    EngineerScore = args.Stats.Progress.EngineerScore,
                    SupportScore = args.Stats.Progress.SupportScore,
                    ReconScore = args.Stats.Progress.ReconScore,
                    TotalScore = args.Stats.Progress.TotalScore,
                    CreatedAt = default,
                    UpdatedAt = default
                },
                PlayerReportReportedPlayers = new List<PlayerReport>(),
                PlayerReportReporters = new List<PlayerReport>()
            });
            Db.SaveChanges();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while storing player: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    public override Task<bool> OnPlayerTypedMessage(MyPlayer player, ChatChannel channel, string msg)
    {
        ChatLogger.StoreChatLog(player.SteamID, msg);
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
            ReportedPlayer = reportedPlayerId
        };
        Db.PlayerReports.Add(report);
        Db.SaveChanges();
        return Task.CompletedTask;
    }

    public override async Task OnPlayerDisconnected(MyPlayer player)
    {
        Console.WriteLine($"[{DateTime.UtcNow}] Player {player.Name} - {player.SteamID} disconnected");
    }

    public override async Task OnRoundStarted()
    {
        RoundSettings.SecondsLeft *= 2;
        RoundSettings.TeamATickets *= 2;
        RoundSettings.TeamBTickets *= 2;
        RoundSettings.MaxTickets = RoundSettings.TeamATickets;
    }

    public override async Task OnPlayerDied(MyPlayer player)
    {
        player.Modifications.RespawnTime = 5;
    }
}

public class MyPlayer : Player<MyPlayer>
{
}
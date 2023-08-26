using BattleBitAPI;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using CommunityServerAPI.Storage;
using SAT.Models;
using SAT.Storage;
using SAT.SwissAdminTools;
using Admin = SAT.SwissAdminTools.Admin;
using BlockType = SAT.SwissAdminTools.BlockType;
using ChatLog = SAT.Models.ChatLog;

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
        const string connectionString = "server=localhost;user=battlebit;password=battlebit;database=battlebit";
        Db = new BattlebitContext();
        Sat = new SwissAdminToolsMysql(connectionString);
    }

    public static SwissAdminToolsStore Sat { get; set; }
    public static BattlebitContext Db { get; set; }

    public override async Task OnConnected()
    {
        ForceStartGame();
        RoundSettings.SecondsLeft = 3600;
        RoundSettings.TeamATickets = 100;
        RoundSettings.TeamBTickets = 100;
        ServerSettings.PlayerCollision = true;
    }

    public override async Task OnPlayerConnected(MyPlayer player)
    {
        var blockDetails = Blocks.IsBlocked(player.SteamID, BlockType.Ban);
        if (blockDetails.isBlocked)
        {
            player.Kick(blockDetails.reason);
            return;
        }

        player.Modifications.CanDeploy = true;
        Console.WriteLine($"Player {player.Name} - {player.SteamID} connected with IP {player.IP}");
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
        Sat.StoreChatLog(player.SteamID, msg);
        var res = AdminTools.ProcessChat(msg, player, this);
        if (!res) return Task.FromResult(false);

        var blockResult = Blocks.IsBlocked(player.SteamID, BlockType.Gag);
        if (!blockResult.isBlocked) return Task.FromResult(true);
        player.WarnPlayer($"You are currently gagged: {blockResult.reason}");
        return Task.FromResult(false);
    }

    public override async Task<OnPlayerSpawnArguments?> OnPlayerSpawning(MyPlayer player, OnPlayerSpawnArguments request)
    {
        if (AdminTools.IsWeaponRestricted(request.Loadout.PrimaryWeapon.Tool))
        {
            player.Modifications.CanDeploy = false;
            player.WarnPlayer($"You are not allowed to use {request.Loadout.PrimaryWeapon.Tool.Name}!");
        }

        if (AdminTools.IsWeaponRestricted(request.Loadout.SecondaryWeapon.Tool))
        {
            player.Modifications.CanDeploy = false;
            player.WarnPlayer($"You are not allowed to use {request.Loadout.SecondaryWeapon.Tool.Name}!");
        }

        //create a timer to reset the player's ability to deploy
        if (player.Modifications.CanDeploy)
            return request;

        //set the player's ability to deploy to true
        Timer t = null; // Declare the timer outside the callback for clarity

        t = new Timer(state =>
        {
            player.Modifications.CanDeploy = true;
            player.Kill();
            t?.Dispose();
        }, null, 2000, Timeout.Infinite);

        return request;
    }

    public override Task OnPlayerReported(MyPlayer from, MyPlayer to, ReportReason reason, string additional)
    {
        var reporterID = Db.Players.FirstOrDefault(player => player.SteamId == (long)from.SteamID)?.Id;
        var reportedPlayerID = Db.Players.FirstOrDefault(player => player.SteamId == (long)to.SteamID);
        if (reporterID == null || reportedPlayerID == null) return Task.CompletedTask;

        var report = new PlayerReport
        {
            ReporterId = reporterID.Value,
            Reason = reason + " " + additional,
            Status = "Pending",
            AdminNotes = null,
            ReportedPlayer = reportedPlayerID
        };
        Db.PlayerReports.Add(report);
        Db.SaveChanges();
        return Task.CompletedTask;
    }
}

public class MyPlayer : Player<MyPlayer>
{
}
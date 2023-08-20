using BattleBitAPI;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using CommunityServerAPI.AdminTools;
using CommunityServerAPI.Storage;

internal class Program
{
    private static void Main(string[] args)
    {
        const int port = 1337;
        var listener = new ServerListener<MyPlayer, MyGameServer>();
        listener.OnCreatingGameServerInstance += OnCreatingGameServerInstance;
        listener.OnCreatingPlayerInstance += OnCreatingPlayerInstance;
        listener.OnGameServerConnected += async server =>
        {
            Console.WriteLine($"Gameserver connected! {server.GameIP}:{server.GamePort} {server.ServerName}");
            server.ServerSettings.PlayerCollision = true;
        };
        listener.Start(port);

        Console.WriteLine($"APIs Server started on port {port}");
        Thread.Sleep(-1);
    }

    private static MyPlayer OnCreatingPlayerInstance()
    {
        return new MyPlayer();
    }

    private static MyGameServer OnCreatingGameServerInstance()
    {
        return new MyGameServer();
    }
}

public class MyPlayer : Player<MyPlayer>
{
}

internal class MyGameServer : GameServer<MyPlayer>
{
    public MyGameServer()
    {
        const string connectionString = "server=localhost;user=battlebit;password=battlebit;database=battlebit";
        Sat = new SwissAdminToolsMysql(connectionString);
    }

    public static SwissAdminToolsStore Sat { get; set; }

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
        var blockDetails = AdminTools.IsBlocked(player.SteamID, AdminTools.BlockType.Ban);
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
            Sat.StorePlayer(steamId, args);
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

        var blockResult = AdminTools.IsBlocked(player.SteamID, AdminTools.BlockType.Gag);
        if (!blockResult.isBlocked) return Task.FromResult(true);
        player.WarnPlayer($"You are currently gagged: {blockResult.reason}");
        return Task.FromResult(false);
    }

    public override async Task<OnPlayerSpawnArguments> OnPlayerSpawning(MyPlayer player, OnPlayerSpawnArguments request)
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
}
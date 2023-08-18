using BattleBitAPI;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using CommunityServerAPI.AdminTools;
using CommunityServerAPI.Storage;

internal class Program
{
    public static SwissAdminToolsStore Sat { get; set; }

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
        const string connectionString = "server=localhost;user=battlebit;password=battlebit;database=battlebit";
        Sat = new SwissAdminToolsMysql(connectionString);
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
    public override async Task OnConnected()
    {
        ForceStartGame();
        ServerSettings.PlayerCollision = true;
    }

    public override async Task OnPlayerConnected(MyPlayer player)
    {
        var blockDetails = ChatProcessor.IsBlocked(player.SteamID, ChatProcessor.BlockType.Ban);
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
            Program.Sat.StorePlayer(steamId, args);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while storing player: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    public override Task<bool> OnPlayerTypedMessage(MyPlayer player, ChatChannel channel, string msg)
    {
        Program.Sat.StoreChatLog(player.SteamID, msg);
        var res = ChatProcessor.ProcessChat(msg, player, this);
        if (!res) return Task.FromResult(false);

        var blockResult = ChatProcessor.IsBlocked(player.SteamID, ChatProcessor.BlockType.Gag);
        if (!blockResult.isBlocked) return Task.FromResult(true);
        player.WarnPlayer($"You are currently gagged: {blockResult.reason}");
        return Task.FromResult(false);
    }
}
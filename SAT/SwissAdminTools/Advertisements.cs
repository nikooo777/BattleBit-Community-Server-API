using BattleBitAPI.Server;
using CommunityServerAPI;
using SAT.configs;
using SwissAdminTools;

namespace SAT.SwissAdminTools;

public class Advertisements
{
    private static readonly string[] WelcomeMessages = ConfigurationManager.Config.welcome_messages.ToArray();
    private readonly string[] mAds;
    private readonly GameServer<MyPlayer> mServer;
    private int mIndex;

    public Advertisements(MyGameServer server)
    {
        mServer = server;
        mAds = ConfigurationManager.Config.advertisements.ToArray();
        mIndex = 0;
    }

    public void Spam()
    {
        while (true)
        {
            mServer.SayToAllChat(mAds[mIndex++ % mAds.Length]);
            Thread.Sleep(1000 * 40); // 40 seconds
        }
    }

    public static string GetWelcomeMessage(string playerName)
    {
        return WelcomeMessages[new Random().Next(0, WelcomeMessages.Length)].Replace("{player_name}", $"{RichText.LightBlue}{playerName}{RichText.EndColor}");
    }
}
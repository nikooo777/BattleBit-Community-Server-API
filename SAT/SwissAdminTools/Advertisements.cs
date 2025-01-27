using BattleBitAPI.Server;
using CommunityServerAPI;
using SAT.configs;
using SwissAdminTools;

namespace SAT.SwissAdminTools;

public class Advertisements
{
    private static readonly string[] WelcomeMessages = ConfigurationManager.Config.welcome_messages.ToArray();
    private static Advertisements? _instance;
    private readonly string[] mAds;
    private readonly GameServer<MyPlayer> mServer;
    private bool mAlreadyRunning;
    private int mIndex;
    private bool mShouldTerminate;

    public Advertisements(MyGameServer server)
    {
        mServer = server;
        mAds = ConfigurationManager.Config.advertisements.ToArray();
        mIndex = 0;
        _instance?.Terminate();
        _instance = this;
    }

    public void Spam()
    {
        if (mAlreadyRunning) return;
        mAlreadyRunning = true;
        while (!mShouldTerminate)
        {
            mServer.SayToAllChat(mAds[mIndex++ % mAds.Length]);
            Thread.Sleep(1000 * 40); // 40 seconds
        }
    }

    private void Terminate()
    {
        Console.WriteLine("Terminating advertisements thread...");
        mShouldTerminate = true;
    }

    public static string GetWelcomeMessage(string playerName)
    {
        return WelcomeMessages[new Random().Next(0, WelcomeMessages.Length)].Replace("{player_name}", $"{RichText.LightBlue}{playerName}{RichText.EndColor}");
    }
}
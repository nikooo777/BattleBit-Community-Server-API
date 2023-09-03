namespace SwissAdminTools.RoundManager;

public class Settings
{
    public static void SettingsBalancer(MyGameServer server)
    {
        switch (server.CurrentPlayerCount)
        {
            case <= 32:
                server.CanSpawnOnPlayers = false;
                server.AllowBleeding = false;
                break;
            case > 32:
                server.CanSpawnOnPlayers = true;
                server.AllowBleeding = true;
                break;
        }
    }
}
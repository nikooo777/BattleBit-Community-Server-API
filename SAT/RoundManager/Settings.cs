using SwissAdminTools;

namespace SAT.RoundManager;

public static class Settings
{
    public static void SettingsBalancer(MyGameServer server)
    {
        if (server.CurrentPlayerCount <= 4)
        {
            foreach (var p in server.AllPlayers)
            {
                p.Modifications.IsExposedOnMap = true;
            }
        } else
        {
            foreach (var p in server.AllPlayers)
            {
                p.Modifications.IsExposedOnMap = false;
            }

            if (server.CurrentPlayerCount <= 32)
            {
                server.CanSpawnOnPlayers = false;
                server.AllowReviving = false;
            } else
            {
                server.CanSpawnOnPlayers = true;
                server.AllowReviving = true;
            }
        }
    }
}
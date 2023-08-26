using SwissAdminTools;

namespace SAT.SwissAdminTools;

public class Admin
{
    public static Dictionary<ulong, Models.Admin?> Admins = new();

    public static bool IsPlayerAdmin(ulong steamId)
    {
        var admin = GetAdmin(steamId);
        return admin != null;
    }

    public static Models.Admin? GetAdmin(ulong steamId)
    {
        if (Admins.TryGetValue(steamId, out var cachedAdmin)) return cachedAdmin;
        var admin = MyGameServer.Db.Admins.FirstOrDefault(admin => admin.SteamId == (long)steamId);
        if (admin == null)
        {
            Admins[steamId] = null;
            return null;
        }

        Admins[steamId] = admin;
        return admin;
    }
}
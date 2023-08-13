using BattleBitAPI.Server;

namespace CommunityServerAPI;

public static class ChatProcessor
{
    private static readonly Dictionary<string, Func<string, GameServer<MyPlayer>, bool>> Commands = new()
    {
        { "say", SayCmd },
        { "clear", ClearCmd }
    };

    public static bool ProcessChat(string message, GameServer<MyPlayer> server)
    {
        if (message.StartsWith("@")) return SayCmd(message.TrimStart('@'), server);
        if (!message.StartsWith("!")) return true;

        message = message.TrimStart('!');
        var split = message.Split(new[] { ' ' }, 2);

        if (split.Length == 0) return true;

        var command = split[0];
        if (!Commands.ContainsKey(command)) return true;

        if (split.Length == 1)
        {
            try
            {
                return Commands[command]("", server);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }

            return true;
        }

        var args = split[1];
        try
        {
            return Commands[command](args, server);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return false;
        }
    }

    private static bool SayCmd(string message, GameServer<MyPlayer> server)
    {
        server.SayToChat($"{RichText.Red}[{RichText.Bold("ADMIN")}]: {RichText.Magenta}{RichText.Italic(message)}");
        return false;
    }

    private static bool ClearCmd(string message, GameServer<MyPlayer> server)
    {
        server.SayToChat($"\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n{RichText.Size(".", 0)}");
        return false;
    }
}
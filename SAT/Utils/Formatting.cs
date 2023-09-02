using SwissAdminTools;

namespace SAT.Utils;

public class Formatting
{
    public static string LengthFromSeconds(long lengthSeconds)
    {
        var lengthMinutes = lengthSeconds / 60f;
        return lengthMinutes switch
        {
            <= 0 => "permanently",
            < 1 => $"{lengthSeconds:0} seconds",
            < 60 => $"{lengthMinutes:0.0} minutes",
            < 1440 => $"{lengthMinutes / 60f:0.0} hours",
            _ => $"{lengthMinutes / 1440f:0.0} days"
        };
    }

    public static void SafeSetLoadingScreenText(string message, MyGameServer server)
    {
        if (message.Length > 1800)
        {
            message = message.Substring(0, 1800);
            Console.Error.WriteLine("Loading screen message too long, truncated to 1800 characters!");
        }

        server.SetLoadingScreenText(message);
    }

    public static string ReplaceFirst(string text, string search, string replace)
    {
        var pos = text.IndexOf(search);
        return pos < 0 ? text : string.Concat(text.AsSpan(0, pos), replace, text.AsSpan(pos + search.Length));
    }
}
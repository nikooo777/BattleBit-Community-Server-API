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
}
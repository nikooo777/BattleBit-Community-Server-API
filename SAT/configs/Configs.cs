using Newtonsoft.Json;

namespace SAT.configs;

public class Configuration
{
    public Restrictions restrictions { get; set; }
    public int max_tickets { get; set; }
    public int max_time { get; set; }
    public string join_text { get; set; }
    public BattlebitDb battlebit_db { get; set; }
}

public class Restrictions
{
    public List<string> weapons { get; set; }
    public List<string> weapon_types { get; set; }
    public List<string> classes { get; set; }
}

public class BattlebitDb
{
    public string user { get; set; }
    public string password { get; set; }
    public string database { get; set; }
    public string host { get; set; }

    public override string ToString()
    {
        return $"Server={host};Database={database};Uid={user};Pwd={password};";
    }
}

public static class ConfigurationManager
{
    static ConfigurationManager()
    {
        LoadConfiguration();
    }

    public static Configuration Config { get; private set; }

    private static void LoadConfiguration()
    {
        var path = "defaults.json";
        if (File.Exists(path))
        {
            var jsonText = File.ReadAllText(path);
            Config = JsonConvert.DeserializeObject<Configuration>(jsonText);
        }
        else
        {
            throw new FileNotFoundException($"Configuration file not found at {path}");
        }
    }
}
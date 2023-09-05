namespace SAT.rank;

public class ToolProgress
{
    public static Models.ToolProgress Merge(Models.ToolProgress a, Models.ToolProgress b)
    {
        if (a.ToolId != b.ToolId) throw new Exception("ToolIds do not match");
        var kills = a.Kills > b.Kills ? a.Kills - b.Kills : b.Kills - a.Kills;
        var distance = a.MaxDistance > b.MaxDistance ? a.MaxDistance : b.MaxDistance;
        return new Models.ToolProgress
        {
            UserId = a.UserId,
            Kills = kills,
            MaxDistance = distance,
            ToolId = a.ToolId
        };
    }

    public static List<Models.ToolProgress> Merge(List<Models.ToolProgress> a, List<Models.ToolProgress> b)
    {
        var mergedList = new List<Models.ToolProgress>();

        // Convert list b into a dictionary for faster lookups
        var dictB = b.ToDictionary(tool => tool.ToolId);

        foreach (var toolA in a)
        {
            if (dictB.TryGetValue(toolA.ToolId, out var toolB))
            {
                mergedList.Add(Merge(toolA, toolB));
                dictB.Remove(toolA.ToolId); // remove the tool from the dictionary once it's merged
            } else
            {
                mergedList.Add(toolA); // If there's no matching tool in b, add the tool from a
            }
        }

        // Add remaining tools from b that weren't matched/merged
        mergedList.AddRange(dictB.Values);

        return mergedList;
    }

    public static List<Models.ToolProgress> ToolsFrom(byte[] data)
    {
        using (var ms = new MemoryStream(data))
        using (var ser = new BinaryReader(ms))
        {
            ser.ReadUInt32();
            var size = data.Length - 4;
            var tools = new List<Models.ToolProgress>();
            while (size > 0)
            {
                size -= 10; // 10 bytes: 2 for id, 4 for value01, and 4 for value02

                var toolId = ser.ReadUInt16();
                var kills = ser.ReadUInt32();
                var maxDistance = ser.ReadUInt32();
                tools.Add(new Models.ToolProgress
                {
                    ToolId = toolId,
                    Kills = (int)kills,
                    MaxDistance = (int)maxDistance
                });
            }

            return tools;
        }
    }
}
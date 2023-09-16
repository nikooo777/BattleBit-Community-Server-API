using System.Text;
using CommunityServerAPI;
using Microsoft.EntityFrameworkCore;
using SAT.Db;
using SAT.Models;
using SAT.Utils;
using SwissAdminTools;

namespace SAT.rank;

public static class Stats
{
    public static string TopN(int limit)
    {
        var db = MyGameServer.Db;
        var topPlayers = db.PlayerProgresses
            .Where(progress => progress.IsOfficial.CompareTo(0) == 0)
            .OrderByDescending(progress => progress.TotalScore)
            .Include(progress => progress.Player)
            .Take(limit);
        DbContextPool.ReturnContext(db);
        var sb = new StringBuilder();
        sb.AppendLine($"Top {limit} Players");
        sb.AppendLine("==============");

        var position = 1;
        foreach (var player in topPlayers)
        {
            sb.AppendLine($"{RichText.Bold($"{position}. {player.Player.Name}")}");
            sb.AppendLine($"\t<mark=#000000aa>{RichText.LightBlue}Kills: {player.KillCount}{RichText.EndColor} - {RichText.Red}Deaths: {player.DeathCount}{RichText.EndColor} - {RichText.Yellow}Points: {player.TotalScore}{RichText.EndColor} - {RichText.Fuchsia}K/D: {(player.DeathCount > 0 ? Math.Round(player.KillCount / (float)player.DeathCount, 2) : player.KillCount)}{RichText.EndColor} - {RichText.Orange}Play Time: {Formatting.LengthFromSeconds(player.PlayTimeSeconds)}{RichText.EndColor}</mark>");
            position++;
        }

        return sb.ToString();
    }

    public static PlayerProgress? Statistics(ulong steamId)
    {
        var db = MyGameServer.Db;
        var playerId = db.Players.FirstOrDefault(p => p.SteamId == (long)steamId)?.Id;
        var playerProgress = db.PlayerProgresses.FirstOrDefault(progress => progress.PlayerId == playerId && progress.IsOfficial.CompareTo(0) == 0);
        DbContextPool.ReturnContext(db);
        return playerProgress;
    }

    public static (int rank, int totalRanked) Rank(ulong steamId)
    {
        var db = MyGameServer.Db;
        var totalRankedPlayers = db.PlayerProgresses.Count(progress => progress.IsOfficial.CompareTo(0) == 0 && progress.TotalScore > 0);
        var playerId = db.Players.FirstOrDefault(p => p.SteamId == (long)steamId)?.Id;
        var rawSql = $@"SELECT player_id, `rank`
FROM (
    SELECT player_id, total_score,
           ROW_NUMBER() OVER (ORDER BY total_score DESC) AS `rank`
    FROM player_progress
    WHERE is_official = 0
) AS ranked
WHERE player_id = {playerId};";

        var rank = db.RankResponses.FromSqlRaw(rawSql).ToList();
        DbContextPool.ReturnContext(db);
        var currentRank = -1;
        if (rank.Count != 0) currentRank = rank[0].rank;

        return (currentRank, totalRankedPlayers);
    }
}
using BattleBitAPI.Common;
using SAT.Models;

namespace SAT.rank;

public class Utils
{
    public static PlayerProgress SetProgress(PlayerStats.PlayerProgess deltaProgress, PlayerProgress dbProgress)
    {
        dbProgress.KillCount = deltaProgress.KillCount;
        dbProgress.LeaderKills = deltaProgress.LeaderKills;
        dbProgress.AssaultKills = deltaProgress.AssaultKills;
        dbProgress.MedicKills = deltaProgress.MedicKills;
        dbProgress.EngineerKills = deltaProgress.EngineerKills;
        dbProgress.SupportKills = deltaProgress.SupportKills;
        dbProgress.ReconKills = deltaProgress.ReconKills;
        dbProgress.DeathCount = deltaProgress.DeathCount;
        dbProgress.WinCount = deltaProgress.WinCount;
        dbProgress.LoseCount = deltaProgress.LoseCount;
        dbProgress.FriendlyShots = deltaProgress.FriendlyShots;
        dbProgress.FriendlyKills = deltaProgress.FriendlyKills;
        dbProgress.Revived = deltaProgress.Revived;
        dbProgress.RevivedTeamMates = deltaProgress.RevivedTeamMates;
        dbProgress.Assists = deltaProgress.Assists;
        dbProgress.Prestige = deltaProgress.Prestige;
        dbProgress.CurrentRank = deltaProgress.Rank;
        dbProgress.Exp = deltaProgress.EXP;
        dbProgress.ShotsFired = deltaProgress.ShotsFired;
        dbProgress.ShotsHit = deltaProgress.ShotsHit;
        dbProgress.Headshots = deltaProgress.Headshots;
        dbProgress.CompletedObjectives = deltaProgress.ObjectivesComplated;
        dbProgress.HealedHps = deltaProgress.HealedHPs;
        dbProgress.RoadKills = deltaProgress.RoadKills;
        dbProgress.Suicides = deltaProgress.Suicides;
        dbProgress.VehiclesDestroyed = deltaProgress.VehiclesDestroyed;
        dbProgress.VehicleHpRepaired = deltaProgress.VehicleHPRepaired;
        dbProgress.LongestKill = deltaProgress.LongestKill;
        dbProgress.PlayTimeSeconds = deltaProgress.PlayTimeSeconds;
        dbProgress.LeaderPlayTime = deltaProgress.LeaderPlayTime;
        dbProgress.AssaultPlayTime = deltaProgress.AssaultPlayTime;
        dbProgress.MedicPlayTime = deltaProgress.MedicPlayTime;
        dbProgress.EngineerPlayTime = deltaProgress.EngineerPlayTime;
        dbProgress.SupportPlayTime = deltaProgress.SupportPlayTime;
        dbProgress.ReconPlayTime = deltaProgress.ReconPlayTime;
        dbProgress.LeaderScore = deltaProgress.LeaderScore;
        dbProgress.AssaultScore = deltaProgress.AssaultScore;
        dbProgress.MedicScore = deltaProgress.MedicScore;
        dbProgress.EngineerScore = deltaProgress.EngineerScore;
        dbProgress.SupportScore = deltaProgress.SupportScore;
        dbProgress.ReconScore = deltaProgress.ReconScore;
        dbProgress.TotalScore = deltaProgress.TotalScore;
        return dbProgress;
    }

    public static PlayerProgress AddProgress(PlayerStats.PlayerProgess deltaProgress, PlayerProgress dbProgress)
    {
        dbProgress.KillCount += deltaProgress.KillCount;
        dbProgress.LeaderKills += deltaProgress.LeaderKills;
        dbProgress.AssaultKills += deltaProgress.AssaultKills;
        dbProgress.MedicKills += deltaProgress.MedicKills;
        dbProgress.EngineerKills += deltaProgress.EngineerKills;
        dbProgress.SupportKills += deltaProgress.SupportKills;
        dbProgress.ReconKills += deltaProgress.ReconKills;
        dbProgress.DeathCount += deltaProgress.DeathCount;
        dbProgress.WinCount += deltaProgress.WinCount;
        dbProgress.LoseCount += deltaProgress.LoseCount;
        dbProgress.FriendlyShots += deltaProgress.FriendlyShots;
        dbProgress.FriendlyKills += deltaProgress.FriendlyKills;
        dbProgress.Revived += deltaProgress.Revived;
        dbProgress.RevivedTeamMates += deltaProgress.RevivedTeamMates;
        dbProgress.Assists += deltaProgress.Assists;
        dbProgress.Prestige += deltaProgress.Prestige;
        dbProgress.CurrentRank += deltaProgress.Rank;
        dbProgress.Exp += deltaProgress.EXP;
        dbProgress.ShotsFired += deltaProgress.ShotsFired;
        dbProgress.ShotsHit += deltaProgress.ShotsHit;
        dbProgress.Headshots += deltaProgress.Headshots;
        dbProgress.CompletedObjectives += deltaProgress.ObjectivesComplated;
        dbProgress.HealedHps += deltaProgress.HealedHPs;
        dbProgress.RoadKills += deltaProgress.RoadKills;
        dbProgress.Suicides += deltaProgress.Suicides;
        dbProgress.VehiclesDestroyed += deltaProgress.VehiclesDestroyed;
        dbProgress.VehicleHpRepaired += deltaProgress.VehicleHPRepaired;
        dbProgress.LongestKill += deltaProgress.LongestKill;
        dbProgress.PlayTimeSeconds += deltaProgress.PlayTimeSeconds;
        dbProgress.LeaderPlayTime += deltaProgress.LeaderPlayTime;
        dbProgress.AssaultPlayTime += deltaProgress.AssaultPlayTime;
        dbProgress.MedicPlayTime += deltaProgress.MedicPlayTime;
        dbProgress.EngineerPlayTime += deltaProgress.EngineerPlayTime;
        dbProgress.SupportPlayTime += deltaProgress.SupportPlayTime;
        dbProgress.ReconPlayTime += deltaProgress.ReconPlayTime;
        dbProgress.LeaderScore += deltaProgress.LeaderScore;
        dbProgress.AssaultScore += deltaProgress.AssaultScore;
        dbProgress.MedicScore += deltaProgress.MedicScore;
        dbProgress.EngineerScore += deltaProgress.EngineerScore;
        dbProgress.SupportScore += deltaProgress.SupportScore;
        dbProgress.ReconScore += deltaProgress.ReconScore;
        dbProgress.TotalScore += deltaProgress.TotalScore;
        return dbProgress;
    }

    public static PlayerStats.PlayerProgess ProgressFrom(PlayerProgress dbProgress)
    {
        return new PlayerStats.PlayerProgess
        {
            KillCount = dbProgress.KillCount,
            LeaderKills = dbProgress.LeaderKills,
            AssaultKills = dbProgress.AssaultKills,
            MedicKills = dbProgress.MedicKills,
            EngineerKills = dbProgress.EngineerKills,
            SupportKills = dbProgress.SupportKills,
            ReconKills = dbProgress.ReconKills,
            DeathCount = dbProgress.DeathCount,
            WinCount = dbProgress.WinCount,
            LoseCount = dbProgress.LoseCount,
            FriendlyShots = dbProgress.FriendlyShots,
            FriendlyKills = dbProgress.FriendlyKills,
            Revived = dbProgress.Revived,
            RevivedTeamMates = dbProgress.RevivedTeamMates,
            Assists = dbProgress.Assists,
            Prestige = dbProgress.Prestige,
            Rank = dbProgress.CurrentRank,
            EXP = dbProgress.Exp,
            ShotsFired = dbProgress.ShotsFired,
            ShotsHit = dbProgress.ShotsHit,
            Headshots = dbProgress.Headshots,
            ObjectivesComplated = dbProgress.CompletedObjectives,
            HealedHPs = dbProgress.HealedHps,
            RoadKills = dbProgress.RoadKills,
            Suicides = dbProgress.Suicides,
            VehiclesDestroyed = dbProgress.VehiclesDestroyed,
            VehicleHPRepaired = dbProgress.VehicleHpRepaired,
            LongestKill = dbProgress.LongestKill,
            PlayTimeSeconds = dbProgress.PlayTimeSeconds,
            LeaderPlayTime = dbProgress.LeaderPlayTime,
            AssaultPlayTime = dbProgress.AssaultPlayTime,
            MedicPlayTime = dbProgress.MedicPlayTime,
            EngineerPlayTime = dbProgress.EngineerPlayTime,
            SupportPlayTime = dbProgress.SupportPlayTime,
            ReconPlayTime = dbProgress.ReconPlayTime,
            LeaderScore = dbProgress.LeaderScore,
            AssaultScore = dbProgress.AssaultScore,
            MedicScore = dbProgress.MedicScore,
            EngineerScore = dbProgress.EngineerScore,
            SupportScore = dbProgress.SupportScore,
            ReconScore = dbProgress.ReconScore,
            TotalScore = dbProgress.TotalScore
        };
    }

    public static PlayerStats.PlayerProgess Delta(PlayerStats.PlayerProgess newer, PlayerStats.PlayerProgess older)
    {
        return new PlayerStats.PlayerProgess
        {
            KillCount = ComputeSignedDelta(newer.KillCount, older.KillCount),
            LeaderKills = ComputeSignedDelta(newer.LeaderKills, older.LeaderKills),
            AssaultKills = ComputeSignedDelta(newer.AssaultKills, older.AssaultKills),
            MedicKills = ComputeSignedDelta(newer.MedicKills, older.MedicKills),
            EngineerKills = ComputeSignedDelta(newer.EngineerKills, older.EngineerKills),
            SupportKills = ComputeSignedDelta(newer.SupportKills, older.SupportKills),
            ReconKills = ComputeSignedDelta(newer.ReconKills, older.ReconKills),
            DeathCount = ComputeSignedDelta(newer.DeathCount, older.DeathCount),
            WinCount = ComputeSignedDelta(newer.WinCount, older.WinCount),
            LoseCount = ComputeSignedDelta(newer.LoseCount, older.LoseCount),
            FriendlyShots = ComputeSignedDelta(newer.FriendlyShots, older.FriendlyShots),
            FriendlyKills = ComputeSignedDelta(newer.FriendlyKills, older.FriendlyKills),
            Revived = ComputeSignedDelta(newer.Revived, older.Revived),
            RevivedTeamMates = ComputeSignedDelta(newer.RevivedTeamMates, older.RevivedTeamMates),
            Assists = ComputeSignedDelta(newer.Assists, older.Assists),
            Prestige = ComputeSignedDelta(newer.Prestige, older.Prestige),
            Rank = ComputeSignedDelta(newer.Rank, older.Rank),
            EXP = ComputeSignedDelta(newer.EXP, older.EXP),
            ShotsFired = ComputeSignedDelta(newer.ShotsFired, older.ShotsFired),
            ShotsHit = ComputeSignedDelta(newer.ShotsHit, older.ShotsHit),
            Headshots = ComputeSignedDelta(newer.Headshots, older.Headshots),
            ObjectivesComplated = ComputeSignedDelta(newer.ObjectivesComplated, older.ObjectivesComplated),
            HealedHPs = ComputeSignedDelta(newer.HealedHPs, older.HealedHPs),
            RoadKills = ComputeSignedDelta(newer.RoadKills, older.RoadKills),
            Suicides = ComputeSignedDelta(newer.Suicides, older.Suicides),
            VehiclesDestroyed = ComputeSignedDelta(newer.VehiclesDestroyed, older.VehiclesDestroyed),
            VehicleHPRepaired = ComputeSignedDelta(newer.VehicleHPRepaired, older.VehicleHPRepaired),
            LongestKill = ComputeSignedDelta(newer.LongestKill, older.LongestKill),
            PlayTimeSeconds = ComputeSignedDelta(newer.PlayTimeSeconds, older.PlayTimeSeconds),
            LeaderPlayTime = ComputeSignedDelta(newer.LeaderPlayTime, older.LeaderPlayTime),
            AssaultPlayTime = ComputeSignedDelta(newer.AssaultPlayTime, older.AssaultPlayTime),
            MedicPlayTime = ComputeSignedDelta(newer.MedicPlayTime, older.MedicPlayTime),
            EngineerPlayTime = ComputeSignedDelta(newer.EngineerPlayTime, older.EngineerPlayTime),
            SupportPlayTime = ComputeSignedDelta(newer.SupportPlayTime, older.SupportPlayTime),
            ReconPlayTime = ComputeSignedDelta(newer.ReconPlayTime, older.ReconPlayTime),
            LeaderScore = ComputeSignedDelta(newer.LeaderScore, older.LeaderScore),
            AssaultScore = ComputeSignedDelta(newer.AssaultScore, older.AssaultScore),
            MedicScore = ComputeSignedDelta(newer.MedicScore, older.MedicScore),
            EngineerScore = ComputeSignedDelta(newer.EngineerScore, older.EngineerScore),
            SupportScore = ComputeSignedDelta(newer.SupportScore, older.SupportScore),
            ReconScore = ComputeSignedDelta(newer.ReconScore, older.ReconScore),
            TotalScore = ComputeSignedDelta(newer.TotalScore, older.TotalScore)
        };
    }

    private static uint ComputeSignedDelta(uint newer, uint older)
    {
        return newer >= older ? newer - older : older - newer;
    }

    public static PlayerStats.PlayerProgess Add(PlayerStats.PlayerProgess a, PlayerStats.PlayerProgess b)
    {
        a.KillCount += b.KillCount;
        a.LeaderKills += b.LeaderKills;
        a.AssaultKills += b.AssaultKills;
        a.MedicKills += b.MedicKills;
        a.EngineerKills += b.EngineerKills;
        a.SupportKills += b.SupportKills;
        a.ReconKills += b.ReconKills;
        a.DeathCount += b.DeathCount;
        a.WinCount += b.WinCount;
        a.LoseCount += b.LoseCount;
        a.FriendlyShots += b.FriendlyShots;
        a.FriendlyKills += b.FriendlyKills;
        a.Revived += b.Revived;
        a.RevivedTeamMates += b.RevivedTeamMates;
        a.Assists += b.Assists;
        a.Prestige += b.Prestige;
        a.Rank += b.Rank;
        a.EXP += b.EXP;
        a.ShotsFired += b.ShotsFired;
        a.ShotsHit += b.ShotsHit;
        a.Headshots += b.Headshots;
        a.ObjectivesComplated += b.ObjectivesComplated;
        a.HealedHPs += b.HealedHPs;
        a.RoadKills += b.RoadKills;
        a.Suicides += b.Suicides;
        a.VehiclesDestroyed += b.VehiclesDestroyed;
        a.VehicleHPRepaired += b.VehicleHPRepaired;
        a.LongestKill += b.LongestKill;
        a.PlayTimeSeconds += b.PlayTimeSeconds;
        a.LeaderPlayTime += b.LeaderPlayTime;
        a.AssaultPlayTime += b.AssaultPlayTime;
        a.MedicPlayTime += b.MedicPlayTime;
        a.EngineerPlayTime += b.EngineerPlayTime;
        a.SupportPlayTime += b.SupportPlayTime;
        a.ReconPlayTime += b.ReconPlayTime;
        a.LeaderScore += b.LeaderScore;
        a.AssaultScore += b.AssaultScore;
        a.MedicScore += b.MedicScore;
        a.EngineerScore += b.EngineerScore;
        a.SupportScore += b.SupportScore;
        a.ReconScore += b.ReconScore;
        a.TotalScore += b.TotalScore;
        return a;
    }
}
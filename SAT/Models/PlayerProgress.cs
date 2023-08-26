using System;
using System.Collections.Generic;

namespace SAT.Models;

public partial class PlayerProgress
{
    public int Id { get; set; }

    public int? PlayerId { get; set; }

    public uint? KillCount { get; set; }

    public uint? DeathCount { get; set; }

    public uint? LeaderKills { get; set; }

    public uint? AssaultKills { get; set; }

    public uint? MedicKills { get; set; }

    public uint? EngineerKills { get; set; }

    public uint? SupportKills { get; set; }

    public uint? ReconKills { get; set; }

    public uint? WinCount { get; set; }

    public uint? LoseCount { get; set; }

    public uint? FriendlyShots { get; set; }

    public uint? FriendlyKills { get; set; }

    public uint? Revived { get; set; }

    public uint? RevivedTeamMates { get; set; }

    public uint? Assists { get; set; }

    public uint? Prestige { get; set; }

    public uint? CurrentRank { get; set; }

    public uint? Exp { get; set; }

    public uint? ShotsFired { get; set; }

    public uint? ShotsHit { get; set; }

    public uint? Headshots { get; set; }

    public uint? CompletedObjectives { get; set; }

    public uint? HealedHps { get; set; }

    public uint? RoadKills { get; set; }

    public uint? Suicides { get; set; }

    public uint? VehiclesDestroyed { get; set; }

    public uint? VehicleHpRepaired { get; set; }

    public uint? LongestKill { get; set; }

    public uint? PlayTimeSeconds { get; set; }

    public uint? LeaderPlayTime { get; set; }

    public uint? AssaultPlayTime { get; set; }

    public uint? MedicPlayTime { get; set; }

    public uint? EngineerPlayTime { get; set; }

    public uint? SupportPlayTime { get; set; }

    public uint? ReconPlayTime { get; set; }

    public uint? LeaderScore { get; set; }

    public uint? AssaultScore { get; set; }

    public uint? MedicScore { get; set; }

    public uint? EngineerScore { get; set; }

    public uint? SupportScore { get; set; }

    public uint? ReconScore { get; set; }

    public uint? TotalScore { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Player? Player { get; set; }
}

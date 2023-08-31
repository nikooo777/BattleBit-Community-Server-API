using System;
using System.Collections.Generic;

namespace SAT.Models;

public partial class Player
{
    public int Id { get; set; }

    public long SteamId { get; set; }

    public string Name { get; set; } = null!;

    public bool IsBanned { get; set; }

    public int? Roles { get; set; }

    public byte[]? Achievements { get; set; }

    public byte[]? Selections { get; set; }

    public byte[]? ToolProgress { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<ChatLog> ChatLogs { get; set; } = new List<ChatLog>();

    public virtual PlayerProgress? PlayerProgress { get; set; }

    public virtual ICollection<PlayerReport> PlayerReportReportedPlayers { get; set; } = new List<PlayerReport>();

    public virtual ICollection<PlayerReport> PlayerReportReporters { get; set; } = new List<PlayerReport>();
}

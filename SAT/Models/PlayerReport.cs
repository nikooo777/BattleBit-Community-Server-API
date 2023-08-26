using System;
using System.Collections.Generic;

namespace SAT.Models;

public partial class PlayerReport
{
    public int Id { get; set; }

    public int? ReporterId { get; set; }

    public int ReportedPlayerId { get; set; }

    public string Reason { get; set; } = null!;

    public DateTime Timestamp { get; set; }

    public string Status { get; set; } = null!;

    public string? AdminNotes { get; set; }

    public virtual Player ReportedPlayer { get; set; } = null!;

    public virtual Player? Reporter { get; set; }
}

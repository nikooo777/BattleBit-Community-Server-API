using System;
using System.Collections.Generic;

namespace SAT.Models;

public partial class Block
{
    public int Id { get; set; }

    public long SteamId { get; set; }

    public string BlockType { get; set; } = null!;

    public string Reason { get; set; } = null!;

    public DateTime ExpiryDate { get; set; }

    public int IssuerAdminId { get; set; }

    public string? TargetIp { get; set; }

    public string? AdminIp { get; set; }

    public virtual Admin IssuerAdmin { get; set; } = null!;
}

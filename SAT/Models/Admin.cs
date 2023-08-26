using System;
using System.Collections.Generic;

namespace SAT.Models;

public partial class Admin
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public long SteamId { get; set; }

    public int Immunity { get; set; }

    public string Flags { get; set; } = null!;

    public virtual ICollection<Block> Blocks { get; set; } = new List<Block>();
}

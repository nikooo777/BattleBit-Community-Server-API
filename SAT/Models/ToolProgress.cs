using System;
using System.Collections.Generic;

namespace SAT.Models;

public partial class ToolProgress
{
    public int Id { get; set; }

    public int ToolId { get; set; }

    public int UserId { get; set; }

    public int Kills { get; set; }

    public int MaxDistance { get; set; }

    public bool IsOfficial { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

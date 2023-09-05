using System;
using System.Collections.Generic;

namespace SAT.Models;

public partial class Tool
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int IngameId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

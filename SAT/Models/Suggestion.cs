using System;
using System.Collections.Generic;

namespace SAT.Models;

public partial class Suggestion
{
    public int Id { get; set; }

    public int? PlayerId { get; set; }

    public string Feedback { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Player? Player { get; set; }
}

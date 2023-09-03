using System;
using System.Collections.Generic;

namespace SAT.Models;

public partial class Stat
{
    public int Id { get; set; }

    public int PlayerCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

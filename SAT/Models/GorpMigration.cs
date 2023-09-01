using System;
using System.Collections.Generic;

namespace SAT.Models;

public partial class GorpMigration
{
    public string Id { get; set; } = null!;

    public DateTime? AppliedAt { get; set; }
}

using System;
using System.Collections.Generic;

namespace SAT.Models;

public partial class ChatLog
{
    public int Id { get; set; }

    public string Message { get; set; } = null!;

    public int PlayerId { get; set; }

    public DateTime Timestamp { get; set; }

    public virtual Player Player { get; set; } = null!;
}

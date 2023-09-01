using Microsoft.EntityFrameworkCore;

namespace SAT.Models;

[Keyless]
public class RankResponse
{
    public int player_id { get; set; }
    public int rank { get; set; }
}
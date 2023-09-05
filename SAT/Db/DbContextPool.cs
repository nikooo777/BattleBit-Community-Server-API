using System.Collections.Concurrent;
using SAT.Models;

namespace SAT.Db;

public class DbContextPool
{
    private readonly ConcurrentStack<BattlebitContext> contexts = new();
    private readonly int maxPoolSize;

    public DbContextPool(int maxPoolSize)
    {
        this.maxPoolSize = maxPoolSize;
    }

    public BattlebitContext GetContext()
    {
        if (contexts.TryPop(out var dbContext)) return dbContext;

        // If we reach here, no available dbContext in the pool, so create a new one.
        return new BattlebitContext();
    }

    public void ReturnContext(BattlebitContext dbContext)
    {
        if (dbContext != null && contexts.Count < maxPoolSize)
            contexts.Push(dbContext);
        else
            dbContext?.Dispose();
    }
}
using System.Collections.Concurrent;
using SAT.Models;

namespace SAT.Db;

public class DbContextPool
{
    private static readonly ConcurrentStack<BattlebitContext> Contexts = new();
    private static int _maxPoolSize;

    public static void Initialize(int maxPoolSize)
    {
        _maxPoolSize = maxPoolSize;
    }

    public static BattlebitContext GetContext()
    {
        if (Contexts.TryPop(out var dbContext)) return dbContext;

        // If we reach here, no available dbContext in the pool, so create a new one.
        return new BattlebitContext();
    }

    public static void ReturnContext(BattlebitContext dbContext)
    {
        if (Contexts.Count < _maxPoolSize)
            Contexts.Push(dbContext);
        else
            dbContext.Dispose();
    }
}
using System.Collections.Concurrent;

namespace SAT.rank;

public class Rank
{
    private static readonly ConcurrentDictionary<ulong, Statistics> Stats = new();

    public static void AddKill(ulong steamId)
    {
        UpdateStats(steamId, s => s.IncrementKills());
    }

    public static void AddDeath(ulong steamId)
    {
        UpdateStats(steamId, s => s.IncrementDeaths());
    }

    public static void AddRevive(ulong steamId)
    {
        UpdateStats(steamId, s => s.IncrementRevives());
    }

    public static void AddSuicide(ulong steamId)
    {
        UpdateStats(steamId, s => s.IncrementSuicides());
    }

    private static void UpdateStats(ulong steamId, Action<Statistics> action)
    {
        Stats.AddOrUpdate(
            steamId,
            _ =>
            {
                var stat = new Statistics();
                action(stat);
                return stat;
            },
            (key, existingValue) =>
            {
                action(existingValue);
                return existingValue;
            }
        );
    }

    private class Statistics
    {
        private readonly object mLock = new();
        private int mDeaths;
        private int mKills;
        private int mRevives;
        private int mSuicides;


        public void IncrementKills()
        {
            lock (mLock)
            {
                mKills++;
            }
        }

        public void IncrementDeaths()
        {
            lock (mLock)
            {
                mDeaths++;
            }
        }

        public void IncrementRevives()
        {
            lock (mLock)
            {
                mRevives++;
            }
        }

        public void IncrementSuicides()
        {
            lock (mLock)
            {
                mSuicides++;
            }
        }

        public int GetKills()
        {
            lock (mLock)
            {
                return mKills;
            }
        }

        public int GetDeaths()
        {
            lock (mLock)
            {
                return mDeaths;
            }
        }

        public int GetRevives()
        {
            lock (mLock)
            {
                return mRevives;
            }
        }

        public int GetSuicides()
        {
            lock (mLock)
            {
                return mSuicides;
            }
        }

        /// <summary>
        ///     Reset the statistics and return the old values.
        /// </summary>
        /// <returns>
        ///     A tuple containing the old values in the following order:
        ///     kills, deaths, revives, suicides
        /// </returns>
        public Tuple<int, int, int, int> Reset()
        {
            lock (mLock)
            {
                var kills = mKills;
                var deaths = mDeaths;
                var revives = mRevives;
                var suicides = mSuicides;
                mKills = 0;
                mDeaths = 0;
                mRevives = 0;
                mSuicides = 0;
                return new Tuple<int, int, int, int>(kills, deaths, revives, suicides);
            }
        }
    }
}
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using SwissAdminTools;

namespace SAT.TeamBalancer;

public static class Balancer
{
    private static bool _isBalancing;
    private static DateTime _balancingStartedAt;

    private static (bool, Team?) shouldBalance(MyGameServer server)
    {
        var teamACount = 0;
        var teamBCount = 0;
        foreach (var p in server.AllPlayers)
        {
            if (p.Team == Team.TeamA)
            {
                teamACount++;
            } else if (p.Team == Team.TeamB)
            {
                teamBCount++;
            }
        }

        var difference = Math.Abs(teamACount - teamBCount);
        var disadvantagedTeam = teamACount > teamBCount ? Team.TeamB : Team.TeamA;
        var maxDifference = server.CurrentPlayerCount switch
        {
            <= 20 => 1,
            <= 28 => 2,
            <= 35 => 3,
            <= 48 => 4,
            _ => 5
        };

        if (difference <= maxDifference) return (false, null);

        return (true, disadvantagedTeam);
    }

    public static void TeamBalancerCheck(MyGameServer server)
    {
        if (_isBalancing)
        {
            if (_balancingStartedAt.AddSeconds(30) < DateTime.Now)
            {
                _isBalancing = false;
                _balancingStartedAt = DateTime.Now;
            } else
            {
                return;
            }
        }

        var teamACount = 0;
        var teamBCount = 0;
        MyPlayer? mostRecentPlayerA = null;
        MyPlayer? mostRecentPlayerB = null;

        foreach (var p in server.AllPlayers)
        {
            p.IsFlaggedForTeamSwitch = false;
            if (p.Team == Team.TeamA)
            {
                teamACount++;
            } else if (p.Team == Team.TeamB)
            {
                teamBCount++;
            }

            if (p.IsDead || !p.IsAlive)
            {
                continue;
            }

            switch (p.Team)
            {
                case Team.TeamA:
                    if (mostRecentPlayerA == null)
                    {
                        mostRecentPlayerA = p;
                    } else if (mostRecentPlayerA.ConnectTime < p.ConnectTime)
                    {
                        mostRecentPlayerA = p;
                    }

                    break;
                case Team.TeamB:
                    if (mostRecentPlayerB == null)
                    {
                        mostRecentPlayerB = p;
                    } else if (mostRecentPlayerB.ConnectTime < p.ConnectTime)
                    {
                        mostRecentPlayerB = p;
                    }

                    break;
            }
        }

        var difference = Math.Abs(teamACount - teamBCount);
        var disadvantagedTeam = teamACount > teamBCount ? Team.TeamB : Team.TeamA;
        var maxDifference = server.CurrentPlayerCount switch
        {
            <= 20 => 1,
            <= 28 => 2,
            <= 35 => 3,
            <= 48 => 4,
            _ => 5
        };

        if (difference <= maxDifference) return;

        switch (disadvantagedTeam)
        {
            case Team.TeamA when mostRecentPlayerB != null:
                _isBalancing = true;
                mostRecentPlayerB.IsFlaggedForTeamSwitch = true;
                break;
            case Team.TeamB when mostRecentPlayerA != null:
                _isBalancing = true;
                mostRecentPlayerA.IsFlaggedForTeamSwitch = true;
                break;
        }
    }

    public static void FastBalance(MyPlayer p, MyGameServer server)
    {
        var (sb, disadvantagedTeam) = shouldBalance(server);
        if (!sb) return;
        if (p.Team == disadvantagedTeam) return;
        SwapPlayer(p, server);
    }

    private static void SwapPlayer(MyPlayer p, MyGameServer server)
    {
        var newTeamSquads = p.Team == Team.TeamA ? server.TeamBSquads : server.TeamASquads;

        p.ChangeTeam();
        Squad<MyPlayer>? bestSquad = null;
        foreach (var s in newTeamSquads)
        {
            //full squad
            if (s.NumberOfMembers == 8)
            {
                continue;
            }

            if (bestSquad == null)
            {
                bestSquad = s;
                continue;
            }

            if (s.NumberOfMembers > bestSquad.NumberOfMembers)
            {
                bestSquad = s;
            }
        }

        if (bestSquad == null)
        {
            return;
        }

        p.JoinSquad(bestSquad.Name);
        p.Message("You have been moved to the other team to balance the game", 3f);
    }

    public static void BalancePlayer(MyPlayer p, MyGameServer server)
    {
        if (!p.IsFlaggedForTeamSwitch) return;
        var newTeamSquads = p.Team == Team.TeamA ? server.TeamBSquads : server.TeamASquads;

        p.ChangeTeam();
        Squad<MyPlayer>? bestSquad = null;
        foreach (var s in newTeamSquads)
        {
            //full squad
            if (s.NumberOfMembers == 8)
            {
                continue;
            }

            if (bestSquad == null)
            {
                bestSquad = s;
                continue;
            }

            if (s.NumberOfMembers > bestSquad.NumberOfMembers)
            {
                bestSquad = s;
            }
        }

        if (bestSquad == null)
        {
            return;
        }

        p.JoinSquad(bestSquad.Name);
        p.Message("You have been moved to the other team to balance the game", 3f);
    }
}
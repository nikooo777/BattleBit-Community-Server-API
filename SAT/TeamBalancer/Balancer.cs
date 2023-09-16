using BattleBitAPI.Common;
using BattleBitAPI.Server;
using SwissAdminTools;

namespace SAT.TeamBalancer;

public static class Balancer
{
    private static bool _isBalancing;
    private static DateTime _balancingStartedAt;

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
        var maxDifference = 1;
        switch (server.CurrentPlayerCount)
        {
            case <= 6:
                maxDifference = 1;
                break;
            case <= 12:
                maxDifference = 2;
                break;
            case <= 24:
                maxDifference = 3;
                break;
            case <= 32:
                maxDifference = 4;
                break;
            case <= 48:
                maxDifference = 5;
                break;
            default:
                maxDifference = 6;
                break;
        }

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
        p.Message("You have been moved to the other team to balance the game", 2f);
    }
}